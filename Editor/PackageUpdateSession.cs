using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Wagenheimer.PackageHub.Editor
{
    public enum UpdateItemStatus
    {
        Pending,
        Updating,
        Verifying,
        Success,
        Failed,
        Skipped
    }

    [Serializable]
    public class PackageUpdateItemState
    {
        public string PackageId;
        public string DisplayName;
        public string OldVersion;
        public string TargetVersion;
        public string GitUrl;
        public UpdateItemStatus Status = UpdateItemStatus.Pending;
        public string StatusDetail = "Aguardando na fila...";
        public string ErrorMessage = "";
        public string VerifiedVersion = "";
    }

    [Serializable]
    public class PackageUpdateSessionData
    {
        public string SessionId = Guid.NewGuid().ToString("N");
        public string Title = "Package Updates";
        public bool IsActive = false;
        public bool IsFinished = false;
        public int TotalCount = 0;
        public int CompletedCount = 0;
        public int FailedCount = 0;
        public List<PackageUpdateItemState> Items = new List<PackageUpdateItemState>();
        public List<string> Logs = new List<string>();
        public string StartTime = "";
        public string EndTime = "";
    }

    /// <summary>
    /// Manages persistent state for package updates across Unity assembly compilations
    /// and domain reloads, ensuring zero dropped packages, strict disk-level verification,
    /// and lockfile cache invalidation.
    /// </summary>
    [InitializeOnLoad]
    public static class PackageUpdateSession
    {
        public const string SessionStateKey = "Wagenheimer_Hub_UpdateSession_Data";
        public const string ManifestPath = "Packages/manifest.json";
        public const string LockfilePath = "Packages/packages-lock.json";
        private const string BackupSessionFile = "Temp/WagenheimerUpdateSession.json";

        public static event Action OnSessionUpdated;

        private static PackageUpdateSessionData _currentSession;
        private static double _resolveStartTime;

        static PackageUpdateSession()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            var session = LoadSession();
            if (session != null && session.IsActive && !session.IsFinished)
            {
                AddLog("Domínio C# recarregado pelo Unity. Verificando integridade das versões instaladas em disco...");
                VerifyAllItemsInSession(session);
            }
        }

        public static PackageUpdateSessionData GetCurrentSession()
        {
            if (_currentSession == null)
            {
                _currentSession = LoadSession() ?? new PackageUpdateSessionData();
            }
            return _currentSession;
        }

        public static bool HasActiveSession()
        {
            var session = GetCurrentSession();
            return session != null && session.IsActive && !session.IsFinished;
        }

        public static PackageUpdateSessionData StartSession(List<PackageItem> items, Dictionary<string, string> targetVersions = null)
        {
            if (items == null || items.Count == 0) return null;

            var session = new PackageUpdateSessionData
            {
                SessionId = Guid.NewGuid().ToString("N"),
                Title = items.Count == 1 ? $"Atualizando {items[0].DisplayName}" : $"Atualizando {items.Count} pacotes",
                IsActive = true,
                IsFinished = false,
                TotalCount = items.Count,
                CompletedCount = 0,
                FailedCount = 0,
                StartTime = DateTime.Now.ToString("HH:mm:ss"),
                Items = new List<PackageUpdateItemState>(),
                Logs = new List<string>()
            };

            foreach (var item in items)
            {
                string targetVer = null;
                if (targetVersions != null && targetVersions.TryGetValue(item.PackageId, out var tv))
                {
                    targetVer = tv;
                }
                else if (!string.IsNullOrEmpty(item.LatestRemoteVersion))
                {
                    targetVer = item.LatestRemoteVersion;
                }
                else
                {
                    targetVer = item.InstalledVersion;
                }

                session.Items.Add(new PackageUpdateItemState
                {
                    PackageId = item.PackageId,
                    DisplayName = item.DisplayName ?? item.PackageId,
                    OldVersion = item.InstalledVersion ?? "desconhecido",
                    TargetVersion = targetVer,
                    GitUrl = item.GitUrl,
                    Status = UpdateItemStatus.Pending,
                    StatusDetail = "Aguardando na fila..."
                });
            }

            _currentSession = session;
            SaveSession(session);
            AddLog($"Iniciando sessão de atualização: {session.TotalCount} pacote(s) selecionado(s).");
            OnSessionUpdated?.Invoke();

            return session;
        }

        public static void AddLog(string message)
        {
            var time = DateTime.Now.ToString("HH:mm:ss");
            var line = $"[{time}] {message}";

            var session = GetCurrentSession();
            if (session != null)
            {
                session.Logs.Add(line);
                if (session.Logs.Count > 120) session.Logs.RemoveAt(0);
                SaveSession(session);
            }

            Debug.Log($"<color=#38BDF8><b>[Package Hub]</b></color> {message}");
            OnSessionUpdated?.Invoke();
        }

        public static void SaveSession(PackageUpdateSessionData session)
        {
            _currentSession = session;
            if (session == null)
            {
                SessionState.EraseString(SessionStateKey);
                try { if (File.Exists(BackupSessionFile)) File.Delete(BackupSessionFile); } catch { }
                return;
            }

            try
            {
                var json = JsonUtility.ToJson(session, false);
                SessionState.SetString(SessionStateKey, json);

                // Secondary backup in Temp
                try
                {
                    var dir = Path.GetDirectoryName(BackupSessionFile);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(BackupSessionFile, json);
                }
                catch { }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PackageUpdateSession] Falha ao salvar sessão: {ex.Message}");
            }
        }

        public static PackageUpdateSessionData LoadSession()
        {
            try
            {
                var json = SessionState.GetString(SessionStateKey, string.Empty);
                if (string.IsNullOrEmpty(json) && File.Exists(BackupSessionFile))
                {
                    json = File.ReadAllText(BackupSessionFile);
                }

                if (!string.IsNullOrEmpty(json))
                {
                    var session = JsonUtility.FromJson<PackageUpdateSessionData>(json);
                    _currentSession = session;
                    return session;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PackageUpdateSession] Falha ao carregar sessão: {ex.Message}");
            }

            return null;
        }

        public static void ClearSession()
        {
            _currentSession = null;
            SessionState.EraseString(SessionStateKey);
            try { if (File.Exists(BackupSessionFile)) File.Delete(BackupSessionFile); } catch { }
            OnSessionUpdated?.Invoke();
        }

        /// <summary>
        /// Reads disk and Unity Package Manager registry to find the real installed version of a package.
        /// </summary>
        public static string GetInstalledVersion(string packageId)
        {
            if (string.IsNullOrEmpty(packageId)) return null;

            try
            {
                // 1. Check registered packages in Unity UPM
                var registered = PackageInfo.GetAllRegisteredPackages();
                foreach (var pkg in registered)
                {
                    if (string.Equals(pkg.name, packageId, StringComparison.OrdinalIgnoreCase))
                    {
                        // Check package.json on disk if resolvedPath is available
                        if (!string.IsNullOrEmpty(pkg.resolvedPath) && Directory.Exists(pkg.resolvedPath))
                        {
                            var diskVer = ReadVersionFromPackageJson(Path.Combine(pkg.resolvedPath, "package.json"));
                            if (!string.IsNullOrEmpty(diskVer)) return diskVer;
                        }

                        if (!string.IsNullOrEmpty(pkg.version))
                            return PackageDiscovery.CleanVersion(pkg.version);
                    }
                }

                // 2. Fallback: Search Packages folder directly
                var embeddedPkgJson = Path.Combine("Packages", packageId, "package.json");
                if (File.Exists(embeddedPkgJson))
                {
                    var v = ReadVersionFromPackageJson(embeddedPkgJson);
                    if (!string.IsNullOrEmpty(v)) return v;
                }

                // 3. Fallback: Search Library/PackageCache
                var packageCacheDir = Path.Combine("Library", "PackageCache");
                if (Directory.Exists(packageCacheDir))
                {
                    var matchingDirs = Directory.GetDirectories(packageCacheDir, $"{packageId}*");
                    foreach (var dir in matchingDirs)
                    {
                        var jsonPath = Path.Combine(dir, "package.json");
                        if (File.Exists(jsonPath))
                        {
                            var v = ReadVersionFromPackageJson(jsonPath);
                            if (!string.IsNullOrEmpty(v)) return v;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PackageUpdateSession] Erro ao inspecionar versão de {packageId}: {ex.Message}");
            }

            return null;
        }

        private static string ReadVersionFromPackageJson(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var content = File.ReadAllText(path);
                    var match = Regex.Match(content, "\"version\"\\s*:\\s*\"([^\"]+)\"");
                    if (match.Success)
                    {
                        return PackageDiscovery.CleanVersion(match.Groups[1].Value);
                    }
                }
            }
            catch { }
            return null;
        }

        public static void VerifyAllItemsInSession(PackageUpdateSessionData session)
        {
            if (session == null || session.Items == null) return;

            int completed = 0;
            int failed = 0;

            foreach (var item in session.Items)
            {
                item.Status = UpdateItemStatus.Verifying;
                item.StatusDetail = "Verificando integridade no disco...";

                var installedVer = GetInstalledVersion(item.PackageId);
                var cleanInstalled = PackageDiscovery.CleanVersion(installedVer);
                var cleanTarget = PackageDiscovery.CleanVersion(item.TargetVersion);

                if (!string.IsNullOrEmpty(cleanInstalled) &&
                    (string.Equals(cleanInstalled, cleanTarget, StringComparison.OrdinalIgnoreCase) ||
                     !PackageUpdateService.IsNewerVersion(cleanTarget, cleanInstalled)))
                {
                    item.Status = UpdateItemStatus.Success;
                    item.VerifiedVersion = cleanInstalled;
                    item.StatusDetail = $"Instalado e verificado: v{cleanInstalled}";
                    item.ErrorMessage = "";
                    completed++;
                    AddLog($"✅ {item.DisplayName}: atualizado e confirmado no disco (v{cleanInstalled}).");
                }
                else
                {
                    item.Status = UpdateItemStatus.Failed;
                    item.VerifiedVersion = cleanInstalled;
                    var err = string.IsNullOrEmpty(installedVer)
                        ? "Pacote não foi detectado no disco após a resolução do UPM."
                        : $"Versão em disco continua sendo v{installedVer} (esperada: v{item.TargetVersion}). Verifique tags no Git.";
                    item.ErrorMessage = err;
                    item.StatusDetail = "Falha na verificação de versão";
                    failed++;
                    AddLog($"❌ {item.DisplayName}: {err}");
                }
            }

            session.CompletedCount = completed;
            session.FailedCount = failed;
            session.IsFinished = true;
            session.IsActive = false;
            session.EndTime = DateTime.Now.ToString("HH:mm:ss");

            SaveSession(session);
            OnSessionUpdated?.Invoke();
            PackageInstaller.NotifyInstallStateChanged();

            // Re-show popup window if closed to display verified results
            PackageUpdateProgressWindow.ShowWindow(false);
        }

        /// <summary>
        /// Safely removes a package entry from Packages/packages-lock.json so Unity doesn't pin an old git commit hash.
        /// </summary>
        public static string RemoveLockfileEntry(string json, string packageId)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(packageId)) return json;

            var keyMatch = Regex.Match(json, $"\"{Regex.Escape(packageId)}\"\\s*:\\s*\\{{");
            if (!keyMatch.Success) return json;

            var braceStart = keyMatch.Index + keyMatch.Length - 1;
            var depth = 0;
            var braceEnd = -1;
            for (var i = braceStart; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        braceEnd = i;
                        break;
                    }
                }
            }

            if (braceEnd < 0) return json;

            var entryEnd = braceEnd + 1;
            var afterEntry = entryEnd;
            while (afterEntry < json.Length && char.IsWhiteSpace(json[afterEntry])) afterEntry++;
            if (afterEntry < json.Length && json[afterEntry] == ',') afterEntry++;

            return json.Substring(0, keyMatch.Index) + json.Substring(afterEntry);
        }

        /// <summary>
        /// Injects or updates a package dependency in Packages/manifest.json.
        /// </summary>
        public static string SetManifestDependency(string json, string packageId, string packageSpec)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(packageId) || string.IsNullOrEmpty(packageSpec))
                return json;

            var depsMatch = Regex.Match(json, @"""dependencies""\s*:\s*\{(.*?)\}", RegexOptions.Singleline);
            if (!depsMatch.Success) return json;

            var depsBody = depsMatch.Groups[1].Value;
            var pkgPattern = $"\"{Regex.Escape(packageId)}\"\\s*:\\s*\"[^\"]*\"";
            var cleanEntry = $"\"{packageId}\": \"{packageSpec}\"";

            if (Regex.IsMatch(depsBody, pkgPattern))
            {
                depsBody = Regex.Replace(depsBody, pkgPattern, cleanEntry);
            }
            else
            {
                // Prepend to dependencies
                depsBody = $"\n    {cleanEntry}," + depsBody;
            }

            var replacement = $"\"dependencies\": {{{depsBody}}}";
            return json.Substring(0, depsMatch.Index) + replacement + json.Substring(depsMatch.Index + depsMatch.Length);
        }

        public static bool IsBalancedJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;

            var objectDepth = 0;
            var arrayDepth = 0;
            var inString = false;
            var escaped = false;

            foreach (var c in json)
            {
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') inString = false;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inString = true;
                        break;
                    case '{':
                        objectDepth++;
                        break;
                    case '}':
                        if (--objectDepth < 0) return false;
                        break;
                    case '[':
                        arrayDepth++;
                        break;
                    case ']':
                        if (--arrayDepth < 0) return false;
                        break;
                }
            }

            return !inString && objectDepth == 0 && arrayDepth == 0;
        }

        /// <summary>
        /// Schedules the batch update with a brief visual deferral so the UI window is fully
        /// centered, laid out, and painted on screen before file I/O and Unity compilation freeze the main thread.
        /// </summary>
        public static void ScheduleBatchUpdate(PackageUpdateSessionData session, Action<bool, string> onComplete = null)
        {
            if (session == null || session.Items == null || session.Items.Count == 0)
            {
                onComplete?.Invoke(false, "Nenhum pacote para atualizar.");
                return;
            }

            session.IsActive = true;
            session.IsFinished = false;
            SaveSession(session);
            OnSessionUpdated?.Invoke();

            double startTime = EditorApplication.timeSinceStartup;
            int step = 0;

            void StepExecution()
            {
                double elapsed = EditorApplication.timeSinceStartup - startTime;

                if (step == 0 && elapsed >= 0.18)
                {
                    step = 1;
                    AddLog("Preparando arquivos de manifesto e limpando caches...");
                    foreach (var it in session.Items)
                    {
                        it.Status = UpdateItemStatus.Updating;
                        it.StatusDetail = $"Configurando especificação v{it.TargetVersion}...";
                    }
                    SaveSession(session);
                    OnSessionUpdated?.Invoke();
                }
                else if (step == 1 && elapsed >= 0.40)
                {
                    step = 2;
                    EditorApplication.update -= StepExecution;
                    ExecuteBatchUpdate(session, onComplete);
                }
            }

            EditorApplication.update += StepExecution;
        }

        /// <summary>
        /// Executes batch update by updating manifest.json, clearing lockfile hashes, and calling Client.Resolve().
        /// </summary>
        public static bool ExecuteBatchUpdate(PackageUpdateSessionData session, Action<bool, string> onComplete = null)
        {
            if (session == null || session.Items == null || session.Items.Count == 0)
            {
                onComplete?.Invoke(false, "Nenhum pacote para atualizar.");
                return false;
            }

            try
            {
                if (!File.Exists(ManifestPath))
                {
                    var msg = $"{ManifestPath} não encontrado.";
                    AddLog($"❌ {msg}");
                    onComplete?.Invoke(false, msg);
                    return false;
                }

                AddLog("Preparando arquivos Packages/manifest.json e Packages/packages-lock.json...");

                var manifestContent = File.ReadAllText(ManifestPath);
                string lockContent = File.Exists(LockfilePath) ? File.ReadAllText(LockfilePath) : null;

                foreach (var item in session.Items)
                {
                    item.Status = UpdateItemStatus.Updating;
                    item.StatusDetail = $"Configurando especificação v{item.TargetVersion}...";

                    var cleanVer = PackageDiscovery.CleanVersion(item.TargetVersion);
                    string packageSpec;
                    if (!string.IsNullOrEmpty(cleanVer))
                    {
                        packageSpec = $"{item.GitUrl}#v{cleanVer}";
                    }
                    else
                    {
                        packageSpec = item.GitUrl;
                    }

                    manifestContent = SetManifestDependency(manifestContent, item.PackageId, packageSpec);

                    if (!string.IsNullOrEmpty(lockContent))
                    {
                        lockContent = RemoveLockfileEntry(lockContent, item.PackageId);
                    }

                    AddLog($"Especificação atualizada: {item.DisplayName} ➔ {packageSpec}");
                }

                if (!IsBalancedJson(manifestContent))
                {
                    var msg = "Operação abortada: o JSON resultante do manifest.json seria inválido.";
                    AddLog($"❌ {msg}");
                    foreach (var it in session.Items)
                    {
                        it.Status = UpdateItemStatus.Failed;
                        it.ErrorMessage = msg;
                    }
                    SaveSession(session);
                    onComplete?.Invoke(false, msg);
                    return false;
                }

                File.WriteAllText(ManifestPath, manifestContent);
                AddLog("Packages/manifest.json atualizado com sucesso.");

                if (!string.IsNullOrEmpty(lockContent) && File.Exists(LockfilePath))
                {
                    File.WriteAllText(LockfilePath, lockContent);
                    AddLog("Hashes antigos de git removidos de Packages/packages-lock.json.");
                }

                SaveSession(session);
                OnSessionUpdated?.Invoke();

                AddLog("Disparando UPM Client.Resolve(). Unity iniciará o download e compilação...");
                AssetDatabase.Refresh();

                _resolveStartTime = EditorApplication.timeSinceStartup;
                Client.Resolve();
                EditorApplication.update += MonitorResolveProgress;

                return true;
            }
            catch (Exception ex)
            {
                var msg = $"Erro inesperado durante a atualização: {ex.Message}";
                AddLog($"❌ {msg}");
                foreach (var it in session.Items)
                {
                    it.Status = UpdateItemStatus.Failed;
                    it.ErrorMessage = msg;
                }
                SaveSession(session);
                onComplete?.Invoke(false, msg);
                return false;
            }
        }

        private static void MonitorResolveProgress()
        {
            var session = GetCurrentSession();
            if (session == null || !session.IsActive || session.IsFinished)
            {
                EditorApplication.update -= MonitorResolveProgress;
                return;
            }

            // If still compiling or updating assets, wait
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            // Give UPM at least 2.5 seconds to start and settle if no compile was triggered
            if (EditorApplication.timeSinceStartup - _resolveStartTime < 2.5)
                return;

            EditorApplication.update -= MonitorResolveProgress;
            AddLog("Resolução do UPM concluída. Verificando integridade das versões instaladas...");
            VerifyAllItemsInSession(session);
        }
    }
}
