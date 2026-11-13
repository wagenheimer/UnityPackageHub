using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Wagenheimer.PackageHub.Editor
{
    public static class PackageInstaller
    {
        public static event Action OnInstallStateChanged;

        public static bool IsBusy => PackageUpdateSession.HasActiveSession();

        public static int BatchTotalCount
        {
            get
            {
                var s = PackageUpdateSession.GetCurrentSession();
                return s != null ? s.TotalCount : 0;
            }
        }

        public static int BatchCompletedCount
        {
            get
            {
                var s = PackageUpdateSession.GetCurrentSession();
                return s != null ? s.CompletedCount : 0;
            }
        }

        public static string CurrentOperationTitle
        {
            get
            {
                var s = PackageUpdateSession.GetCurrentSession();
                if (s == null || !s.IsActive || s.IsFinished) return null;
                return s.Title;
            }
        }

        public static float BatchProgress
        {
            get
            {
                var total = BatchTotalCount;
                var completed = BatchCompletedCount;
                return total > 0 ? (float)completed / total : (IsBusy ? 0.5f : 0f);
            }
        }

        public static PackageItem CurrentUpdatingItem
        {
            get
            {
                var s = PackageUpdateSession.GetCurrentSession();
                if (s == null || s.Items == null) return null;
                var active = s.Items.FirstOrDefault(i => i.Status == UpdateItemStatus.Updating || i.Status == UpdateItemStatus.Verifying);
                if (active != null)
                {
                    return new PackageItem
                    {
                        PackageId = active.PackageId,
                        DisplayName = active.DisplayName,
                        LatestRemoteVersion = active.TargetVersion,
                        InstalledVersion = active.OldVersion,
                        GitUrl = active.GitUrl
                    };
                }
                return null;
            }
        }

        public static void NotifyInstallStateChanged()
        {
            OnInstallStateChanged?.Invoke();
        }

        /// <summary>
        /// Installs or updates a single package, opening the real-time visual progress popup.
        /// </summary>
        public static void InstallOrUpdate(PackageItem item, string targetVersion = null, Action<bool, string> callback = null)
        {
            if (item == null)
            {
                callback?.Invoke(false, "O pacote especificado é nulo.");
                return;
            }

            // Check legacy I2 migration if applicable
            var migration = I2LegacyMigration.Prepare(item);
            if (migration is I2MigrationOutcome.Cancelled or I2MigrationOutcome.Failed)
            {
                var reason = migration == I2MigrationOutcome.Cancelled
                    ? "Cancelado: o código legado de I2 precisa ser migrado antes de instalar o pacote."
                    : "Falha na migração legada de I2; veja o Console do Unity.";
                callback?.Invoke(false, reason);
                return;
            }

            if (IsBusy)
            {
                PackageUpdateProgressWindow.ShowWindow(true);
                callback?.Invoke(false, "Outra atualização de pacotes já está em andamento. Acompanhe pela janela visual de progresso.");
                return;
            }

            var dict = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(targetVersion))
            {
                dict[item.PackageId] = targetVersion;
            }

            var list = new List<PackageItem> { item };
            var session = PackageUpdateSession.StartSession(list, dict);
            PackageUpdateProgressWindow.ShowWindow(true);
            NotifyInstallStateChanged();

            PackageUpdateSession.ScheduleBatchUpdate(session, (success, err) =>
            {
                NotifyInstallStateChanged();
                callback?.Invoke(success, err);
            });
        }

        /// <summary>
        /// Updates all specified packages at once via a single UPM resolve, surviving assembly reloads and showing real-time progress.
        /// </summary>
        public static void UpdateAll(List<PackageItem> itemsToUpdate, Action onAllCompleted = null)
        {
            if (itemsToUpdate == null || itemsToUpdate.Count == 0)
            {
                onAllCompleted?.Invoke();
                return;
            }

            if (IsBusy)
            {
                PackageUpdateProgressWindow.ShowWindow(true);
                return;
            }

            // Preflight migrations
            foreach (var item in itemsToUpdate)
            {
                var migration = I2LegacyMigration.Prepare(item);
                if (migration is I2MigrationOutcome.Cancelled or I2MigrationOutcome.Failed)
                {
                    PackageUpdateSession.AddLog($"Cancelado: migração necessária pendente para {item.DisplayName}.");
                    return;
                }
            }

            var session = PackageUpdateSession.StartSession(itemsToUpdate);
            PackageUpdateProgressWindow.ShowWindow(true);
            NotifyInstallStateChanged();

            PackageUpdateSession.ScheduleBatchUpdate(session, (success, err) =>
            {
                NotifyInstallStateChanged();
                onAllCompleted?.Invoke();
            });
        }

        /// <summary>
        /// Retries updating any packages that failed in the last session.
        /// </summary>
        public static void RetryFailed(Action onComplete = null)
        {
            var session = PackageUpdateSession.GetCurrentSession();
            if (session == null || session.Items == null) return;

            var failedItems = session.Items.Where(i => i.Status == UpdateItemStatus.Failed).ToList();
            if (failedItems.Count == 0) return;

            PackageUpdateSession.AddLog($"Reiniciando tentativa de atualização para {failedItems.Count} pacote(s)...");

            foreach (var item in failedItems)
            {
                item.Status = UpdateItemStatus.Pending;
                item.ErrorMessage = "";
                item.StatusDetail = "Aguardando nova tentativa...";
            }

            session.IsFinished = false;
            session.IsActive = true;
            session.FailedCount = 0;
            PackageUpdateSession.SaveSession(session);
            PackageUpdateProgressWindow.ShowWindow(true);
            NotifyInstallStateChanged();

            PackageUpdateSession.ScheduleBatchUpdate(session, (success, err) =>
            {
                NotifyInstallStateChanged();
                onComplete?.Invoke();
            });
        }
    }
}
