using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Wagenheimer.PackageHub.Editor
{
    public class PackageHubWindow : EditorWindow
    {
        private enum Tab
        {
            Installed,
            ExploreCatalog,
            Settings
        }

        private Tab _currentTab = Tab.Installed;
        private string _searchFilter = "";
        private Vector2 _scrollPos;
        private List<PackageItem> _allPackages = new List<PackageItem>();
        private bool _isCheckingAll = false;
        private string _targetPackageFocus = null;

        // Visual styles
        private GUIStyle _headerBoxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _cardHeaderStyle;
        private GUIStyle _tagStyle;
        private GUIStyle _badgeUpToDate;
        private GUIStyle _badgeUpdateAvailable;
        private GUIStyle _badgeInstalled;
        private GUIStyle _richNotesStyle;

        private Texture2D _headerTex;
        private Texture2D _cardTex;
        private Texture2D _dividerTex;

        [MenuItem("Tools/Wagenheimer/Package Hub...", priority = 0)]
        public static void ShowWindow()
        {
            var win = GetWindow<PackageHubWindow>("Wagenheimer Hub");
            win.minSize = new Vector2(720, 540);
            win.Show();
        }

        [MenuItem("Tools/Wagenheimer/Check for Updates...", priority = 1)]
        public static void CheckForUpdatesMenu()
        {
            var win = GetWindow<PackageHubWindow>("Wagenheimer Hub");
            win.minSize = new Vector2(720, 540);
            win.Show();
            win.CheckAllUpdates();
        }

        [MenuItem("Window/Wagenheimer/Package Hub", priority = 200)]
        public static void ShowWindowAlt() => ShowWindow();

        public static void OpenToPackage(string packageId)
        {
            var win = GetWindow<PackageHubWindow>("Wagenheimer Hub");
            win.minSize = new Vector2(720, 540);
            win._targetPackageFocus = packageId;
            win.Show();
            win.RefreshPackages(true);
        }

        private void OnEnable()
        {
            RefreshPackages(false);
        }

        private void OnDisable()
        {
            DestroyTextures();
        }

        private void RefreshPackages(bool triggerRemoteCheck)
        {
            _allPackages = PackageDiscovery.GetAllPackages();
            if (triggerRemoteCheck)
            {
                CheckAllUpdates();
            }
        }

        public void CheckAllUpdates()
        {
            _isCheckingAll = true;
            var toCheck = _allPackages.Where(p => p.IsInstalled).ToList();
            if (toCheck.Count == 0) toCheck = _allPackages;

            PackageUpdateService.CheckUpdates(toCheck, () =>
            {
                _isCheckingAll = false;
                Repaint();
            });
        }

        private void UpdateAllOutdated()
        {
            var outdated = _allPackages.Where(p => p.IsInstalled && p.HasUpdate).ToList();
            if (outdated.Count == 0) return;

            PackageInstaller.UpdateAll(outdated, () =>
            {
                RefreshPackages(false);
                Repaint();
            });
        }

        private void OnGUI()
        {
            InitStyles();

            DrawHeader();
            DrawTabBar();

            GUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            switch (_currentTab)
            {
                case Tab.Installed:
                    DrawInstalledTab();
                    break;
                case Tab.ExploreCatalog:
                    DrawCatalogTab();
                    break;
                case Tab.Settings:
                    DrawSettingsTab();
                    break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            DrawFooter();
        }

        private void DrawHeader()
        {
            // Top Accent Bar (Cyan/Blue)
            var topBar = GUILayoutUtility.GetRect(position.width, 3);
            EditorGUI.DrawRect(topBar, new Color(0.22f, 0.62f, 0.98f));

            // Header Container
            GUILayout.BeginVertical(_headerBoxStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);

            // Left: Title, Subtitle, and Website Link
            GUILayout.BeginVertical();
            GUILayout.Space(2);
            GUILayout.Label("WAGENHEIMER PACKAGE HUB", _headerStyle);
            GUILayout.Space(3);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Ecosystem Package Manager & Updater •", _subHeaderStyle);
            if (GUILayout.Button("wagenheimer.com ↗", EditorStyles.linkLabel))
            {
                Application.OpenURL("https://wagenheimer.com");
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Right: Status Badges and Global Actions
            var installedCount = _allPackages.Count(p => p.IsInstalled);
            var updateCount = _allPackages.Count(p => p.IsInstalled && p.HasUpdate);

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Installed count badge
            GUILayout.Label($"<b>{installedCount}</b> Installed", _tagStyle);
            GUILayout.Space(6);

            // Update status badge
            if (updateCount > 0)
            {
                GUI.backgroundColor = new Color(0.98f, 0.58f, 0.16f);
                GUILayout.Label($"⚡ <b>{updateCount}</b> Update{(updateCount > 1 ? "s" : "")} Available", _tagStyle);
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUI.backgroundColor = new Color(0.22f, 0.72f, 0.38f);
                GUILayout.Label("✓ All Up to Date", _tagStyle);
                GUI.backgroundColor = Color.white;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            // Action buttons
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.enabled = !_isCheckingAll && !PackageInstaller.IsBusy;
            if (GUILayout.Button(_isCheckingAll ? "Checking..." : "🔄 Check All Updates", EditorStyles.miniButtonLeft, GUILayout.Height(22), GUILayout.Width(130)))
            {
                CheckAllUpdates();
            }

            GUI.enabled = updateCount > 0 && !PackageInstaller.IsBusy;
            GUI.backgroundColor = updateCount > 0 ? new Color(0.2f, 0.75f, 0.35f) : Color.white;
            if (GUILayout.Button($"Update All ({updateCount})", EditorStyles.miniButtonRight, GUILayout.Height(22), GUILayout.Width(105)))
            {
                UpdateAllOutdated();
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            DrawDivider();
        }

        private void DrawTabBar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Toggle(_currentTab == Tab.Installed, $"Installed Packages ({_allPackages.Count(p => p.IsInstalled)})", EditorStyles.toolbarButton))
                _currentTab = Tab.Installed;

            if (GUILayout.Toggle(_currentTab == Tab.ExploreCatalog, $"Explore Catalog ({_allPackages.Count})", EditorStyles.toolbarButton))
                _currentTab = Tab.ExploreCatalog;

            if (GUILayout.Toggle(_currentTab == Tab.Settings, "Settings", EditorStyles.toolbarButton))
                _currentTab = Tab.Settings;

            GUILayout.FlexibleSpace();

            // Search bar
            GUILayout.Label("Search:", EditorStyles.miniLabel);
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(180));
            if (!string.IsNullOrEmpty(_searchFilter) && GUILayout.Button("", GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? EditorStyles.miniButton))
            {
                _searchFilter = "";
                GUI.FocusControl(null);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawInstalledTab()
        {
            GUILayout.Space(8);

            var installed = _allPackages.Where(p => p.IsInstalled).ToList();

            if (!string.IsNullOrEmpty(_searchFilter))
            {
                installed = installed.Where(p =>
                    p.DisplayName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.PackageId.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Description.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0
                ).ToList();
            }

            if (installed.Count == 0)
            {
                GUILayout.Space(30);
                EditorGUILayout.HelpBox("No Wagenheimer packages found in this project matching the filter.", MessageType.Info);
                return;
            }

            // Put packages with updates first
            var ordered = installed.OrderByDescending(p => p.HasUpdate).ThenBy(p => p.DisplayName);

            foreach (var item in ordered)
            {
                DrawPackageCard(item, true);
                GUILayout.Space(8);
            }
        }

        private void DrawCatalogTab()
        {
            GUILayout.Space(8);

            var list = _allPackages.AsEnumerable();

            if (!string.IsNullOrEmpty(_searchFilter))
            {
                list = list.Where(p =>
                    p.DisplayName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.PackageId.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Description.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Category.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0
                );
            }

            var ordered = list.OrderBy(p => p.IsInstalled).ThenBy(p => p.Category).ThenBy(p => p.DisplayName);

            foreach (var item in ordered)
            {
                DrawPackageCard(item, false);
                GUILayout.Space(8);
            }
        }

        private void DrawPackageCard(PackageItem item, bool isInstalledTab)
        {
            var isFocused = !string.IsNullOrEmpty(_targetPackageFocus) && string.Equals(item.PackageId, _targetPackageFocus, StringComparison.OrdinalIgnoreCase);

            GUI.backgroundColor = isFocused ? new Color(0.85f, 0.93f, 1f) : Color.white;
            GUILayout.BeginVertical(_cardStyle);
            GUI.backgroundColor = Color.white;

            // Header line
            GUILayout.BeginHorizontal();

            // Name and category
            GUILayout.Label(item.DisplayName, _cardHeaderStyle);
            GUILayout.Space(6);
            GUILayout.Label(item.Category, _tagStyle);

            GUILayout.FlexibleSpace();

            // Status badges
            if (item.IsUpdating)
            {
                GUI.backgroundColor = new Color(0.2f, 0.6f, 0.9f);
                GUILayout.Label("INSTALLING...", _tagStyle);
                GUI.backgroundColor = Color.white;
            }
            else if (item.IsChecking)
            {
                GUILayout.Label("Checking...", EditorStyles.miniLabel);
            }
            else if (item.IsInstalled)
            {
                if (item.HasUpdate)
                {
                    GUILayout.Label($"UPDATE: v{item.InstalledVersion} ➔ v{item.LatestRemoteVersion}", _badgeUpdateAvailable);
                }
                else
                {
                    GUILayout.Label($"v{item.InstalledVersion} (Latest)", _badgeUpToDate);
                }
            }
            else
            {
                GUILayout.Label("Available", _tagStyle);
            }

            // Action buttons
            GUILayout.Space(8);

            if (item.IsInstalled)
            {
                if (item.HasUpdate)
                {
                    GUI.backgroundColor = new Color(0.2f, 0.75f, 0.35f);
                    GUI.enabled = !PackageInstaller.IsBusy;
                    if (GUILayout.Button($"Update to v{item.LatestRemoteVersion}", GUILayout.Height(22), GUILayout.Width(130)))
                    {
                        PackageInstaller.InstallOrUpdate(item, item.LatestRemoteVersion, (success, err) =>
                        {
                            RefreshPackages(false);
                            Repaint();
                        });
                    }
                    GUI.enabled = true;
                    GUI.backgroundColor = Color.white;
                }
            }
            else
            {
                GUI.backgroundColor = new Color(0.2f, 0.6f, 0.9f);
                GUI.enabled = !PackageInstaller.IsBusy;
                if (GUILayout.Button("Install to Project", GUILayout.Height(22), GUILayout.Width(120)))
                {
                    PackageInstaller.InstallOrUpdate(item, null, (success, err) =>
                    {
                        RefreshPackages(false);
                        Repaint();
                    });
                }
                GUI.enabled = true;
                GUI.backgroundColor = Color.white;
            }

            // Changelog toggle
            if (!string.IsNullOrEmpty(item.ReleaseNotes))
            {
                var icon = item.ExpandedNotes ? "▼ Notes" : "▶ Notes";
                if (GUILayout.Button(icon, EditorStyles.miniButton, GUILayout.Width(64), GUILayout.Height(22)))
                {
                    item.ExpandedNotes = !item.ExpandedNotes;
                }
            }

            // GitHub link
            if (GUILayout.Button("GitHub", EditorStyles.miniButton, GUILayout.Width(54), GUILayout.Height(22)))
            {
                Application.OpenURL(item.RepoUrl);
            }

            GUILayout.EndHorizontal();

            // Package ID and description
            GUILayout.Space(2);
            GUILayout.Label(item.PackageId, EditorStyles.miniBoldLabel);
            GUILayout.Label(item.Description, EditorStyles.wordWrappedLabel);

            // Error display
            if (!string.IsNullOrEmpty(item.UpdateError))
            {
                GUILayout.Space(4);
                EditorGUILayout.HelpBox($"Update error: {item.UpdateError}", MessageType.Error);
            }

            // Release Notes accordion
            if (item.ExpandedNotes && !string.IsNullOrEmpty(item.ReleaseNotes))
            {
                GUILayout.Space(6);
                DrawReleaseNotes(item.ReleaseNotes);
            }

            GUILayout.EndVertical();
        }

        private void DrawReleaseNotes(string notes)
        {
            var formatted = FormatReleaseNotes(notes);

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Release Notes", EditorStyles.miniBoldLabel);
            GUILayout.Space(2);
            GUILayout.Label(formatted, _richNotesStyle);
            GUILayout.EndVertical();
        }

        private string FormatReleaseNotes(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return "";

            var lines = markdown.Split('\n');
            var result = new System.Text.StringBuilder();

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');
                var trimmed = line.Trim();

                if (trimmed.StartsWith("### Added") || trimmed.StartsWith("#### Added"))
                    result.AppendLine("<color=#4ADE80><b>✦ Added</b></color>");
                else if (trimmed.StartsWith("### Fixed") || trimmed.StartsWith("#### Fixed"))
                    result.AppendLine("<color=#60A5FA><b>✔ Fixed</b></color>");
                else if (trimmed.StartsWith("### Changed") || trimmed.StartsWith("#### Changed"))
                    result.AppendLine("<color=#F59E0B><b>⚡ Changed</b></color>");
                else if (trimmed.StartsWith("###") || trimmed.StartsWith("##"))
                    result.AppendLine($"<b>{trimmed.TrimStart('#').Trim()}</b>");
                else if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                {
                    var content = trimmed.Substring(2);
                    content = Regex.Replace(content, @"\*\*(.*?)\*\*", "<b>$1</b>");
                    result.AppendLine($"  • {content}");
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    var content = Regex.Replace(line, @"\*\*(.*?)\*\*", "<b>$1</b>");
                    result.AppendLine(content);
                }
                else
                {
                    result.AppendLine();
                }
            }

            return result.ToString().Trim();
        }

        private void DrawSettingsTab()
        {
            GUILayout.Space(12);

            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                GUILayout.Label("Auto-Check Settings", _cardHeaderStyle);
                GUILayout.Space(8);

                var autoCheck = EditorPrefs.GetBool(PackageHubAutoChecker.PrefAutoCheck, true);
                GUILayout.BeginHorizontal();
                var newAutoCheck = EditorGUILayout.Toggle(autoCheck, GUILayout.Width(20));
                GUILayout.Label("Check for Updates on Startup (runs once daily in background)", EditorStyles.label);
                GUILayout.EndHorizontal();

                if (newAutoCheck != autoCheck)
                {
                    EditorPrefs.SetBool(PackageHubAutoChecker.PrefAutoCheck, newAutoCheck);
                }

                GUILayout.Space(6);

                var autoOpen = EditorPrefs.GetBool(PackageHubAutoChecker.PrefAutoOpenWindow, false);
                GUILayout.BeginHorizontal();
                var newAutoOpen = EditorGUILayout.Toggle(autoOpen, GUILayout.Width(20));
                GUILayout.Label("Auto-open Hub window when new updates are found", EditorStyles.label);
                GUILayout.EndHorizontal();

                if (newAutoOpen != autoOpen)
                {
                    EditorPrefs.SetBool(PackageHubAutoChecker.PrefAutoOpenWindow, newAutoOpen);
                }
            }

            GUILayout.Space(10);

            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                GUILayout.Label("Cache & Maintenance", _cardHeaderStyle);
                GUILayout.Space(8);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear Check Timestamp", GUILayout.Width(190), GUILayout.Height(24)))
                {
                    EditorPrefs.DeleteKey(PackageHubAutoChecker.PrefLastCheck);
                    Debug.Log("[Wagenheimer Package Hub] Reset check schedule. Next startup will check automatically.");
                }
                GUILayout.Space(8);
                GUILayout.Label("Forces auto-checker to run again on next editor launch.", EditorStyles.miniLabel);
                GUILayout.EndHorizontal();

                GUILayout.Space(6);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Force Re-scan Packages", GUILayout.Width(190), GUILayout.Height(24)))
                {
                    RefreshPackages(true);
                }
                GUILayout.Space(8);
                GUILayout.Label("Re-reads all installed UPM packages and queries GitHub.", EditorStyles.miniLabel);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                GUILayout.Label("About & Links", _cardHeaderStyle);
                GUILayout.Space(8);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Developer Website:", EditorStyles.label, GUILayout.Width(130));
                if (GUILayout.Button("https://wagenheimer.com ↗", EditorStyles.linkLabel))
                {
                    Application.OpenURL("https://wagenheimer.com");
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(4);

                GUILayout.BeginHorizontal();
                GUILayout.Label("GitHub Profile:", EditorStyles.label, GUILayout.Width(130));
                if (GUILayout.Button("https://github.com/wagenheimer ↗", EditorStyles.linkLabel))
                {
                    Application.OpenURL("https://github.com/wagenheimer");
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawFooter()
        {
            DrawDivider();
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Space(8);

            GUILayout.Label("Wagenheimer Package Hub v1.0.2", EditorStyles.miniLabel);

            GUILayout.Space(12);
            if (GUILayout.Button("🌐 wagenheimer.com", EditorStyles.toolbarButton))
            {
                Application.OpenURL("https://wagenheimer.com");
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("GitHub", EditorStyles.toolbarButton))
            {
                Application.OpenURL("https://github.com/wagenheimer");
            }

            if (GUILayout.Button("Package Manager", EditorStyles.toolbarButton))
            {
                UnityEditor.PackageManager.UI.Window.Open("");
            }

            GUILayout.Space(8);
            GUILayout.EndHorizontal();
        }

        private void DrawDivider()
        {
            var rect = GUILayoutUtility.GetRect(position.width, 1);
            if (_dividerTex != null)
                GUI.DrawTexture(rect, _dividerTex);
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerTex = MakeTex(1, 1, new Color(0.07f, 0.10f, 0.18f)); // #121A2E slate-900
            _cardTex = MakeTex(1, 1, EditorGUIUtility.isProSkin ? new Color(0.18f, 0.20f, 0.24f) : new Color(0.92f, 0.92f, 0.92f));
            _dividerTex = MakeTex(1, 1, EditorGUIUtility.isProSkin ? new Color(0.24f, 0.27f, 0.32f) : new Color(0.75f, 0.75f, 0.75f));

            _headerBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _headerTex },
                padding = new RectOffset(12, 12, 8, 8),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.96f, 0.98f) }
            };

            _subHeaderStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.60f, 0.72f, 0.84f) }
            };

            _cardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 4, 4)
            };

            _cardHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.95f, 0.95f, 0.98f) : new Color(0.1f, 0.1f, 0.1f) }
            };

            _tagStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 9,
                fixedHeight = 19,
                padding = new RectOffset(7, 7, 2, 2),
                fontStyle = FontStyle.Bold
            };

            _badgeUpToDate = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 10,
                fixedHeight = 20,
                padding = new RectOffset(8, 8, 2, 2),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 0.8f, 0.35f) }
            };

            _badgeUpdateAvailable = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 10,
                fixedHeight = 20,
                padding = new RectOffset(8, 8, 2, 2),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.6f, 0.15f) }
            };

            _richNotesStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                richText = true,
                fontSize = 11,
                padding = new RectOffset(6, 6, 4, 4)
            };
        }

        private void DestroyTextures()
        {
            if (_headerTex != null) DestroyImmediate(_headerTex);
            if (_cardTex != null) DestroyImmediate(_cardTex);
            if (_dividerTex != null) DestroyImmediate(_dividerTex);
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            var pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
