using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wagenheimer.PackageHub.Editor
{
    public class PackageHubWindow : EditorWindow
    {
        public enum Tab
        {
            Installed = 0,
            ExploreCatalog = 1,
            About = 2,
            Settings = 3
        }

        private const string PackageJsonPath = "Packages/com.wagenheimer.packagehub/package.json";
        private const float MinWindowWidth = 760f;
        private const float MinWindowHeight = 560f;
        private const float DefaultWindowWidth = 920f;
        private const float DefaultWindowHeight = 660f;

        private Tab _currentTab = Tab.Installed;
        private string _searchFilter = "";
        private string _selectedCategory = "All";
        private List<PackageItem> _allPackages = new List<PackageItem>();
        private bool _isCheckingAll = false;
        private string _targetPackageFocus = null;
        private string _hubVersion = "1.1.0";

        private VisualElement _contentContainer;
        private Button[] _tabButtons;
        private VisualElement _headerContainer;
        private VisualElement _metricsContainer;

        [MenuItem("Tools/Wagenheimer/Package Hub...", priority = 0)]
        public static void ShowWindow() => OpenHub();

        [MenuItem("Window/Wagenheimer/Package Hub", priority = 200)]
        public static void ShowWindowAlt() => ShowWindow();

        [MenuItem("Tools/Wagenheimer/Check for Updates...", priority = 1)]
        public static void CheckForUpdatesMenu()
        {
            var win = OpenHub();
            win.CheckAllUpdates();
        }

        [MenuItem("Tools/Wagenheimer/Visit wagenheimer.com ↗", priority = 300)]
        public static void OpenWebsiteMenu()
        {
            Application.OpenURL("https://wagenheimer.com");
        }

        [MenuItem("Tools/Wagenheimer/GitHub Repositories ↗", priority = 301)]
        public static void OpenGitHubMenu()
        {
            Application.OpenURL("https://github.com/wagenheimer");
        }

        public static void OpenToPackage(string packageId)
        {
            var win = OpenHub();
            win._targetPackageFocus = packageId;
            win._currentTab = Tab.Installed;
            win.RefreshPackages(true);
        }

        private static PackageHubWindow OpenHub()
        {
            var win = GetWindow<PackageHubWindow>("Wagenheimer Hub");
            win.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
            win.Show();
            win.Focus();
            EnsureWindowOnScreen(win);
            return win;
        }

        private static void EnsureWindowOnScreen(EditorWindow window)
        {
            Rect host;
            try
            {
                host = EditorGUIUtility.GetMainWindowPosition();
            }
            catch
            {
                return;
            }

            if (host.width < 1f || host.height < 1f)
                return;

            var rect = window.position;

            var degenerate = float.IsNaN(rect.x) || float.IsNaN(rect.y) ||
                             float.IsInfinity(rect.x) || float.IsInfinity(rect.y) ||
                             rect.width < 50f || rect.height < 50f;

            const float margin = 40f;
            var overlaps = rect.xMax > host.x + margin &&
                           rect.yMax > host.y + margin &&
                           rect.x < host.xMax - margin &&
                           rect.y < host.yMax - margin;

            if (!degenerate && overlaps)
                return;

            var width = degenerate ? DefaultWindowWidth : rect.width;
            var height = degenerate ? DefaultWindowHeight : rect.height;

            width = Mathf.Clamp(width, MinWindowWidth, Mathf.Max(MinWindowWidth, host.width - 40f));
            height = Mathf.Clamp(height, MinWindowHeight, Mathf.Max(MinWindowHeight, host.height - 40f));

            window.position = new Rect(
                Mathf.Round(host.x + (host.width - width) * 0.5f),
                Mathf.Round(host.y + (host.height - height) * 0.5f),
                Mathf.Round(width),
                Mathf.Round(height));
        }

        private void OnEnable()
        {
            LoadPackageVersion();
            try
            {
                RefreshPackages(false);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Wagenheimer Package Hub] Package discovery failed: {e.Message}");
                _allPackages = new List<PackageItem>();
            }
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColBgDark);
            PackageHubUIStyle.Apply(rootVisualElement);

            var root = new VisualElement();
            root.AddToClassList("hub-root");
            root.style.flexGrow = 1;
            root.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColBgDark);
            root.style.paddingTop = 12;
            root.style.paddingBottom = 12;
            root.style.paddingLeft = 16;
            root.style.paddingRight = 16;

            // 1. Header Banner
            _headerContainer = new VisualElement();
            root.Add(_headerContainer);
            RebuildHeader();

            // 2. Metrics Counter Bar
            _metricsContainer = new VisualElement();
            root.Add(_metricsContainer);
            RebuildMetrics();

            // 3. Tab Bar
            var tabToolbar = new VisualElement();
            tabToolbar.AddToClassList("hub-tab-bar");
            tabToolbar.style.flexDirection = FlexDirection.Row;
            tabToolbar.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColCardBg);
            tabToolbar.style.borderTopWidth = 1;
            tabToolbar.style.borderBottomWidth = 1;
            tabToolbar.style.borderLeftWidth = 1;
            tabToolbar.style.borderRightWidth = 1;
            tabToolbar.style.borderTopColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            tabToolbar.style.borderBottomColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            tabToolbar.style.borderLeftColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            tabToolbar.style.borderRightColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            tabToolbar.style.SetRadius(7);
            tabToolbar.style.paddingTop = 3;
            tabToolbar.style.paddingBottom = 3;
            tabToolbar.style.paddingLeft = 3;
            tabToolbar.style.paddingRight = 3;
            tabToolbar.style.marginBottom = 12;
            tabToolbar.style.flexShrink = 0;

            var installedCount = _allPackages.Count(p => p.IsInstalled);
            var catalogCount = _allPackages.Count;

            var tabNames = new[]
            {
                $"Installed Packages ({installedCount})",
                $"Explore Catalog ({catalogCount})",
                "Sobre Cezar Wagenheimer & Ecosystem",
                "Settings & Maintenance"
            };

            _tabButtons = new Button[tabNames.Length];
            for (var i = 0; i < tabNames.Length; i++)
            {
                var tabIndex = (Tab)i;
                var btn = new Button(() => SwitchTab(tabIndex))
                {
                    text = tabNames[i]
                };
                btn.AddToClassList("hub-tab-btn");
                btn.style.flexGrow = 1;
                btn.style.height = 30;
                btn.style.borderTopWidth = 0;
                btn.style.borderBottomWidth = 0;
                btn.style.borderLeftWidth = 0;
                btn.style.borderRightWidth = 0;
                btn.style.SetRadius(5);
                btn.style.backgroundColor = new StyleColor(Color.clear);
                btn.style.color = new StyleColor(PackageHubUIStyle.ColTextMuted);
                btn.style.fontSize = 11.5f;
                btn.style.unityFontStyleAndWeight = FontStyle.Bold;
                btn.style.marginLeft = 2;
                btn.style.marginRight = 2;
                _tabButtons[i] = btn;
                tabToolbar.Add(btn);
            }
            root.Add(tabToolbar);

            // 4. Dynamic Content Area
            _contentContainer = new VisualElement();
            _contentContainer.AddToClassList("hub-content-container");
            _contentContainer.style.flexGrow = 1;
            root.Add(_contentContainer);

            // 5. Footer
            root.Add(BuildFooter());

            rootVisualElement.Add(root);

            RenderActiveTab();
        }

        public void SwitchTab(Tab tab)
        {
            _currentTab = tab;
            RenderActiveTab();
        }

        private void RebuildHeader()
        {
            if (_headerContainer == null) return;
            _headerContainer.Clear();

            var updateCount = _allPackages.Count(p => p.IsInstalled && p.HasUpdate);
            var header = PackageHubUIStyle.CreateHeader(
                "WAGENHEIMER PACKAGE HUB",
                "Central Ecosystem Package Manager, Diagnostics & Dashboard Center",
                _hubVersion,
                CheckAllUpdates,
                UpdateAllOutdated,
                updateCount,
                _isCheckingAll
            );
            _headerContainer.Add(header);
        }

        private void RebuildMetrics()
        {
            if (_metricsContainer == null) return;
            _metricsContainer.Clear();

            var installedCount = _allPackages.Count(p => p.IsInstalled);
            var totalCount = _allPackages.Count;
            var updateCount = _allPackages.Count(p => p.IsInstalled && p.HasUpdate);
            var upToDateCount = installedCount - updateCount;
            var dashboards = PackageDashboardLauncher.GetInstalledDashboards(_allPackages);

            var row = new VisualElement();
            row.AddToClassList("hub-metrics-row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 12;

            // Installed card
            row.Add(PackageHubUIStyle.CreateMetricCard("Installed in Project", $"{installedCount} / {totalCount}", out var instVal));
            instVal.style.color = new StyleColor(PackageHubUIStyle.ColAccent);

            // Up to Date card
            row.Add(PackageHubUIStyle.CreateMetricCard("Up to Date", upToDateCount.ToString(), out var upVal));
            upVal.style.color = new StyleColor(PackageHubUIStyle.ColGreen);

            // Updates Available card
            row.Add(PackageHubUIStyle.CreateMetricCard("Updates Available", updateCount.ToString(), out var upAvailVal));
            if (updateCount > 0)
                upAvailVal.style.color = new StyleColor(PackageHubUIStyle.ColAmber);
            else
                upAvailVal.style.color = new StyleColor(PackageHubUIStyle.ColTextMuted);

            // Dashboards Ready card
            var dashCard = PackageHubUIStyle.CreateMetricCard("Dashboards Ready", $"{dashboards.Count} Active", out var dashVal);
            dashCard.style.marginRight = 0;
            dashVal.style.color = new StyleColor(new Color(0.65f, 0.55f, 0.98f));
            row.Add(dashCard);

            _metricsContainer.Add(row);
        }

        private void RenderActiveTab()
        {
            if (_contentContainer == null) return;
            _contentContainer.Clear();

            // Update tab button highlights
            if (_tabButtons != null)
            {
                var installedCount = _allPackages.Count(p => p.IsInstalled);
                var catalogCount = _allPackages.Count;

                _tabButtons[0].text = $"Installed Packages ({installedCount})";
                _tabButtons[1].text = $"Explore Catalog ({catalogCount})";

                for (var i = 0; i < _tabButtons.Length; i++)
                {
                    if (i == (int)_currentTab)
                    {
                        _tabButtons[i].AddToClassList("hub-tab-btn--active");
                        _tabButtons[i].style.backgroundColor = new StyleColor(new Color(0.15f, 0.39f, 0.92f));
                        _tabButtons[i].style.color = new StyleColor(Color.white);
                    }
                    else
                    {
                        _tabButtons[i].RemoveFromClassList("hub-tab-btn--active");
                        _tabButtons[i].style.backgroundColor = new StyleColor(Color.clear);
                        _tabButtons[i].style.color = new StyleColor(PackageHubUIStyle.ColTextMuted);
                    }
                }
            }

            switch (_currentTab)
            {
                case Tab.Installed:
                    _contentContainer.Add(BuildInstalledView());
                    break;
                case Tab.ExploreCatalog:
                    _contentContainer.Add(BuildCatalogView());
                    break;
                case Tab.About:
                    _contentContainer.Add(BuildAboutView());
                    break;
                case Tab.Settings:
                    _contentContainer.Add(BuildSettingsView());
                    break;
            }
        }

        #region Tab 0: Installed Packages View

        private VisualElement BuildInstalledView()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;

            // 1. Quick Launch Dashboards Bar
            var dashboards = PackageDashboardLauncher.GetInstalledDashboards(_allPackages);
            if (dashboards.Count > 0)
            {
                var quickBar = new VisualElement();
                quickBar.AddToClassList("hub-quickbar");
                quickBar.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColCardBg);
                quickBar.style.borderTopWidth = 1;
                quickBar.style.borderBottomWidth = 1;
                quickBar.style.borderLeftWidth = 1;
                quickBar.style.borderRightWidth = 1;
                quickBar.style.borderTopColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
                quickBar.style.borderBottomColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
                quickBar.style.borderLeftColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
                quickBar.style.borderRightColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
                quickBar.style.SetRadius(8);
                quickBar.style.paddingTop = 10;
                quickBar.style.paddingBottom = 10;
                quickBar.style.paddingLeft = 14;
                quickBar.style.paddingRight = 14;
                quickBar.style.marginBottom = 12;

                var qHeader = new VisualElement();
                qHeader.AddToClassList("hub-quickbar-header");
                qHeader.style.flexDirection = FlexDirection.Row;
                qHeader.style.alignItems = Align.Center;
                qHeader.style.justifyContent = Justify.SpaceBetween;
                qHeader.style.marginBottom = 8;

                var qTitle = new Label("⚡ QUICK LAUNCH DASHBOARDS — INSTANT 1-CLICK ACCESS");
                qTitle.AddToClassList("hub-quickbar-title");
                qTitle.style.fontSize = 11;
                qTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                qTitle.style.color = new StyleColor(PackageHubUIStyle.ColAccent);
                qHeader.Add(qTitle);

                var qCountBadge = PackageHubUIStyle.CreateBadge($"{dashboards.Count} Ready", "hub-badge-info");
                qHeader.Add(qCountBadge);
                quickBar.Add(qHeader);

                var chipRow = new VisualElement();
                chipRow.AddToClassList("hub-quickbar-chips");
                chipRow.style.flexDirection = FlexDirection.Row;
                chipRow.style.flexWrap = Wrap.Wrap;
                chipRow.style.alignItems = Align.Center;

                foreach (var dash in dashboards)
                {
                    var chip = new Button(() =>
                    {
                        PackageDashboardLauncher.Launch(dash.PrimaryDashboard);
                    })
                    {
                        text = $"{dash.Icon}  {dash.DisplayName}"
                    };
                    chip.AddToClassList("hub-chip-btn");
                    chip.style.backgroundColor = new StyleColor(new Color(0.14f, 0.16f, 0.22f));
                    chip.style.borderTopWidth = 1;
                    chip.style.borderBottomWidth = 1;
                    chip.style.borderLeftWidth = 1;
                    chip.style.borderRightWidth = 1;
                    chip.style.borderTopColor = new StyleColor(new Color(0.22f, 0.26f, 0.34f));
                    chip.style.borderBottomColor = new StyleColor(new Color(0.22f, 0.26f, 0.34f));
                    chip.style.borderLeftColor = new StyleColor(new Color(0.22f, 0.26f, 0.34f));
                    chip.style.borderRightColor = new StyleColor(new Color(0.22f, 0.26f, 0.34f));
                    chip.style.SetRadius(6);
                    chip.style.paddingTop = 6;
                    chip.style.paddingBottom = 6;
                    chip.style.paddingLeft = 12;
                    chip.style.paddingRight = 12;
                    chip.style.marginRight = 8;
                    chip.style.marginBottom = 6;
                    chip.style.fontSize = 11;
                    chip.style.unityFontStyleAndWeight = FontStyle.Bold;
                    chip.style.color = new StyleColor(PackageHubUIStyle.ColTextWhite);
                    chip.tooltip = $"Open {dash.PrimaryTitle} ({dash.PrimaryDashboard.MenuItemPath})";
                    chipRow.Add(chip);
                }

                quickBar.Add(chipRow);
                container.Add(quickBar);
            }

            // 2. Search & Filter Bar
            container.Add(BuildSearchFilterBar(isInstalledTab: true));

            // 3. Scrollable List of Packages
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;

            var installed = _allPackages.Where(p => p.IsInstalled).ToList();

            if (!string.IsNullOrEmpty(_searchFilter))
            {
                installed = installed.Where(p =>
                    p.DisplayName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.PackageId.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Description.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0
                ).ToList();
            }

            if (_selectedCategory != "All")
            {
                installed = installed.Where(p => string.Equals(p.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (installed.Count == 0)
            {
                var emptyCard = new VisualElement();
                emptyCard.AddToClassList("hub-card");
                emptyCard.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColCardBg);
                emptyCard.style.SetRadius(8);
                emptyCard.style.paddingTop = 20;
                emptyCard.style.paddingBottom = 20;
                emptyCard.style.alignItems = Align.Center;

                var emptyLbl = new Label("No installed Wagenheimer packages match your search filter.");
                emptyLbl.style.color = new StyleColor(PackageHubUIStyle.ColTextMuted);
                emptyCard.Add(emptyLbl);
                scroll.Add(emptyCard);
            }
            else
            {
                var ordered = installed.OrderByDescending(p => p.HasUpdate).ThenBy(p => p.DisplayName);
                foreach (var item in ordered)
                {
                    scroll.Add(BuildPackageCard(item, isInstalledTab: true));
                }
            }

            container.Add(scroll);
            return container;
        }

        #endregion

        #region Tab 1: Explore Catalog View

        private VisualElement BuildCatalogView()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;

            // Search & Filter Bar
            container.Add(BuildSearchFilterBar(isInstalledTab: false));

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;

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

            if (_selectedCategory != "All")
            {
                list = list.Where(p => string.Equals(p.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase));
            }

            var ordered = list.OrderBy(p => p.IsInstalled).ThenBy(p => p.Category).ThenBy(p => p.DisplayName);

            foreach (var item in ordered)
            {
                scroll.Add(BuildPackageCard(item, isInstalledTab: false));
            }

            container.Add(scroll);
            return container;
        }

        #endregion

        #region Search & Category Filter Component

        private VisualElement BuildSearchFilterBar(bool isInstalledTab)
        {
            var bar = new VisualElement();
            bar.AddToClassList("hub-search-toolbar");
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColCardBg);
            bar.style.borderTopWidth = 1;
            bar.style.borderBottomWidth = 1;
            bar.style.borderLeftWidth = 1;
            bar.style.borderRightWidth = 1;
            bar.style.borderTopColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            bar.style.borderBottomColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            bar.style.borderLeftColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            bar.style.borderRightColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            bar.style.SetRadius(6);
            bar.style.paddingTop = 6;
            bar.style.paddingBottom = 6;
            bar.style.paddingLeft = 10;
            bar.style.paddingRight = 10;
            bar.style.marginBottom = 12;

            var searchLabel = new Label("Search:");
            searchLabel.style.fontSize = 11;
            searchLabel.style.color = new StyleColor(PackageHubUIStyle.ColTextMuted);
            searchLabel.style.marginRight = 6;
            bar.Add(searchLabel);

            var searchField = new TextField { value = _searchFilter };
            searchField.AddToClassList("hub-search-field");
            searchField.style.flexGrow = 1;
            searchField.style.marginRight = 10;
            searchField.RegisterValueChangedCallback(evt =>
            {
                _searchFilter = evt.newValue;
                RenderActiveTab();
            });
            bar.Add(searchField);

            var chipRow = new VisualElement();
            chipRow.AddToClassList("hub-filter-chips");
            chipRow.style.flexDirection = FlexDirection.Row;
            chipRow.style.alignItems = Align.Center;

            var categories = new[] { "All", "Core Tools", "Monetization", "Build & CI", "Storage & Cloud", "Engagement", "Input" };
            foreach (var cat in categories)
            {
                var isSelected = string.Equals(_selectedCategory, cat, StringComparison.OrdinalIgnoreCase);
                var chip = new Button(() =>
                {
                    _selectedCategory = cat;
                    RenderActiveTab();
                })
                {
                    text = cat
                };
                chip.AddToClassList("hub-filter-chip");
                chip.style.paddingTop = 4;
                chip.style.paddingBottom = 4;
                chip.style.paddingLeft = 8;
                chip.style.paddingRight = 8;
                chip.style.SetRadius(4);
                chip.style.borderTopWidth = 1;
                chip.style.borderBottomWidth = 1;
                chip.style.borderLeftWidth = 1;
                chip.style.borderRightWidth = 1;
                chip.style.fontSize = 10.5f;
                chip.style.marginLeft = 4;

                if (isSelected)
                {
                    chip.AddToClassList("hub-filter-chip--active");
                    chip.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColAccent);
                    chip.style.borderTopColor = new StyleColor(PackageHubUIStyle.ColAccent);
                    chip.style.borderBottomColor = new StyleColor(PackageHubUIStyle.ColAccent);
                    chip.style.borderLeftColor = new StyleColor(PackageHubUIStyle.ColAccent);
                    chip.style.borderRightColor = new StyleColor(PackageHubUIStyle.ColAccent);
                    chip.style.color = new StyleColor(new Color(0.06f, 0.09f, 0.16f));
                    chip.style.unityFontStyleAndWeight = FontStyle.Bold;
                }
                else
                {
                    chip.style.backgroundColor = new StyleColor(new Color(0.14f, 0.16f, 0.22f));
                    chip.style.borderTopColor = new StyleColor(new Color(0.22f, 0.26f, 0.34f));
                    chip.style.borderBottomColor = new StyleColor(new Color(0.22f, 0.26f, 0.34f));
                    chip.style.borderLeftColor = new StyleColor(new Color(0.22f, 0.26f, 0.34f));
                    chip.style.borderRightColor = new StyleColor(new Color(0.22f, 0.26f, 0.34f));
                    chip.style.color = new StyleColor(PackageHubUIStyle.ColTextMuted);
                }
                chipRow.Add(chip);
            }

            bar.Add(chipRow);
            return bar;
        }

        #endregion

        #region Package Card Builder (with Dashboard shortcuts & Actions)

        private VisualElement BuildPackageCard(PackageItem item, bool isInstalledTab)
        {
            var isFocused = !string.IsNullOrEmpty(_targetPackageFocus) &&
                            string.Equals(item.PackageId, _targetPackageFocus, StringComparison.OrdinalIgnoreCase);

            var card = new VisualElement();
            card.AddToClassList("hub-card");
            card.style.backgroundColor = new StyleColor(isFocused ? new Color(0.13f, 0.16f, 0.24f) : PackageHubUIStyle.ColCardBg);
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = isFocused ? 3 : 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopColor = new StyleColor(isFocused ? PackageHubUIStyle.ColAccent : PackageHubUIStyle.ColCardBorder);
            card.style.borderBottomColor = new StyleColor(isFocused ? PackageHubUIStyle.ColAccent : PackageHubUIStyle.ColCardBorder);
            card.style.borderLeftColor = new StyleColor(isFocused ? PackageHubUIStyle.ColAccent : PackageHubUIStyle.ColCardBorder);
            card.style.borderRightColor = new StyleColor(isFocused ? PackageHubUIStyle.ColAccent : PackageHubUIStyle.ColCardBorder);
            card.style.SetRadius(8);
            card.style.paddingTop = 12;
            card.style.paddingBottom = 12;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;
            card.style.marginBottom = 10;

            // Top Header: Name, Category, Badges
            var header = new VisualElement();
            header.AddToClassList("hub-card-header");
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginBottom = 6;

            var identity = new VisualElement();
            identity.AddToClassList("hub-card-identity");
            identity.style.flexDirection = FlexDirection.Row;
            identity.style.alignItems = Align.Center;

            var titleLbl = new Label(item.DisplayName);
            titleLbl.AddToClassList("hub-card-title");
            titleLbl.style.fontSize = 14;
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLbl.style.color = new StyleColor(PackageHubUIStyle.ColTextWhite);
            identity.Add(titleLbl);

            var catBadge = new Label(item.Category);
            catBadge.AddToClassList("hub-card-category");
            catBadge.style.backgroundColor = new StyleColor(new Color(0.14f, 0.16f, 0.22f));
            catBadge.style.borderTopWidth = 1;
            catBadge.style.borderBottomWidth = 1;
            catBadge.style.borderLeftWidth = 1;
            catBadge.style.borderRightWidth = 1;
            catBadge.style.borderTopColor = new StyleColor(new Color(0.22f, 0.26f, 0.34f));
            catBadge.style.borderBottomColor = new StyleColor(new Color(0.22f, 0.26f, 0.34f));
            catBadge.style.borderLeftColor = new StyleColor(new Color(0.22f, 0.26f, 0.34f));
            catBadge.style.borderRightColor = new StyleColor(new Color(0.22f, 0.26f, 0.34f));
            catBadge.style.SetRadius(4);
            catBadge.style.paddingTop = 2;
            catBadge.style.paddingBottom = 2;
            catBadge.style.paddingLeft = 7;
            catBadge.style.paddingRight = 7;
            catBadge.style.fontSize = 10;
            catBadge.style.color = new StyleColor(PackageHubUIStyle.ColTextMuted);
            catBadge.style.marginLeft = 8;
            identity.Add(catBadge);

            header.Add(identity);

            // Badges row
            var badgesRow = new VisualElement();
            badgesRow.AddToClassList("hub-card-badges");
            badgesRow.style.flexDirection = FlexDirection.Row;
            badgesRow.style.alignItems = Align.Center;

            if (item.IsUpdating)
            {
                badgesRow.Add(PackageHubUIStyle.CreateBadge("INSTALLING...", "hub-badge-info"));
            }
            else if (item.IsChecking)
            {
                badgesRow.Add(PackageHubUIStyle.CreateBadge("Checking...", "hub-badge-neutral"));
            }
            else if (item.IsInstalled)
            {
                if (item.HasUpdate)
                {
                    badgesRow.Add(PackageHubUIStyle.CreateBadge($"UPDATE: v{item.InstalledVersion} ➔ v{item.LatestRemoteVersion}", "hub-badge-update"));
                }
                else
                {
                    badgesRow.Add(PackageHubUIStyle.CreateBadge($"v{item.InstalledVersion} (Latest)", "hub-badge-pass"));
                }
            }
            else
            {
                badgesRow.Add(PackageHubUIStyle.CreateBadge("Available", "hub-badge-neutral"));
            }

            header.Add(badgesRow);
            card.Add(header);

            // Description
            var desc = new Label(item.Description);
            desc.AddToClassList("hub-card-desc");
            desc.style.fontSize = 11.5f;
            desc.style.color = new StyleColor(new Color(0.80f, 0.84f, 0.89f));
            desc.style.marginTop = 4;
            desc.style.marginBottom = 6;
            desc.style.whiteSpace = WhiteSpace.Normal;
            card.Add(desc);

            // Meta Row with Dashboard shortcuts, actions, and Package ID
            var metaRow = new VisualElement();
            metaRow.AddToClassList("hub-card-meta-row");
            metaRow.style.flexDirection = FlexDirection.Row;
            metaRow.style.alignItems = Align.Center;
            metaRow.style.justifyContent = Justify.SpaceBetween;
            metaRow.style.borderTopWidth = 1;
            metaRow.style.borderTopColor = new StyleColor(new Color(1f, 1f, 1f, 0.05f));
            metaRow.style.paddingTop = 8;
            metaRow.style.marginTop = 4;

            var leftMeta = new VisualElement();
            leftMeta.style.flexDirection = FlexDirection.Column;

            var idLabel = new Label(item.PackageId);
            idLabel.AddToClassList("hub-card-id");
            idLabel.style.fontSize = 10;
            idLabel.style.color = new StyleColor(new Color(0.45f, 0.50f, 0.60f));
            idLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            leftMeta.Add(idLabel);

            // DASHBOARD SHORTCUTS FOR INSTALLED PACKAGE
            var dashInfo = PackageDashboardLauncher.GetDashboardInfo(item.PackageId);
            if (item.IsInstalled && dashInfo != null)
            {
                var shortcutsRow = new VisualElement();
                shortcutsRow.AddToClassList("hub-shortcuts-row");
                shortcutsRow.style.flexDirection = FlexDirection.Row;
                shortcutsRow.style.alignItems = Align.Center;
                shortcutsRow.style.flexWrap = Wrap.Wrap;
                shortcutsRow.style.marginTop = 6;

                // Primary Dashboard Button
                if (dashInfo.PrimaryDashboard != null)
                {
                    var dashBtn = PackageHubUIStyle.CreateButton(
                        $"⚡ Open {dashInfo.PrimaryTitle}",
                        "hub-btn-dashboard",
                        () =>
                        {
                            PackageDashboardLauncher.Launch(dashInfo.PrimaryDashboard, item.RepoUrl);
                        }
                    );
                    dashBtn.style.paddingTop = 4;
                    dashBtn.style.paddingBottom = 4;
                    dashBtn.style.paddingLeft = 10;
                    dashBtn.style.paddingRight = 10;
                    dashBtn.style.fontSize = 11;
                    dashBtn.style.marginRight = 6;
                    dashBtn.tooltip = $"Launch {dashInfo.PrimaryTitle} ({dashInfo.PrimaryDashboard.MenuItemPath})";
                    shortcutsRow.Add(dashBtn);
                }

                // Secondary tool buttons
                foreach (var sec in dashInfo.SecondaryTools)
                {
                    var secBtn = PackageHubUIStyle.CreateButton(
                        $"{sec.Icon} {sec.Title}",
                        "hub-btn-secondary",
                        () =>
                        {
                            PackageDashboardLauncher.Launch(sec, item.RepoUrl);
                        }
                    );
                    secBtn.style.paddingTop = 3;
                    secBtn.style.paddingBottom = 3;
                    secBtn.style.paddingLeft = 8;
                    secBtn.style.paddingRight = 8;
                    secBtn.style.fontSize = 10;
                    secBtn.style.marginRight = 4;
                    secBtn.tooltip = $"Open {sec.Title} ({sec.MenuItemPath})";
                    shortcutsRow.Add(secBtn);
                }

                leftMeta.Add(shortcutsRow);
            }

            metaRow.Add(leftMeta);

            // Right Actions: Install / Update, Changelog, GitHub
            var actions = new VisualElement();
            actions.AddToClassList("hub-card-actions");
            actions.style.flexDirection = FlexDirection.Row;
            actions.style.alignItems = Align.Center;

            if (item.IsInstalled)
            {
                if (item.HasUpdate)
                {
                    var updateBtn = PackageHubUIStyle.CreateButton(
                        $"⚡ Update to v{item.LatestRemoteVersion}",
                        "hub-btn-warning",
                        () =>
                        {
                            PackageInstaller.InstallOrUpdate(item, item.LatestRemoteVersion, (success, err) =>
                            {
                                RefreshPackages(false);
                                RebuildHeader();
                                RebuildMetrics();
                                RenderActiveTab();
                            });
                        }
                    );
                    updateBtn.SetEnabled(!PackageInstaller.IsBusy);
                    updateBtn.style.marginRight = 6;
                    actions.Add(updateBtn);
                }
            }
            else
            {
                var installBtn = PackageHubUIStyle.CreateButton(
                    "Install to Project",
                    "hub-btn-primary",
                    () =>
                    {
                        PackageInstaller.InstallOrUpdate(item, null, (success, err) =>
                        {
                            RefreshPackages(false);
                            RebuildHeader();
                            RebuildMetrics();
                            RenderActiveTab();
                        });
                    }
                );
                installBtn.SetEnabled(!PackageInstaller.IsBusy);
                installBtn.style.marginRight = 6;
                actions.Add(installBtn);
            }

            // Release Notes Toggle
            if (!string.IsNullOrEmpty(item.ReleaseNotes))
            {
                var notesBtn = PackageHubUIStyle.CreateButton(
                    item.ExpandedNotes ? "▼ Notes" : "▶ Notes",
                    "hub-btn-secondary",
                    () =>
                    {
                        item.ExpandedNotes = !item.ExpandedNotes;
                        RenderActiveTab();
                    }
                );
                notesBtn.style.marginRight = 6;
                actions.Add(notesBtn);
            }

            // GitHub Button
            var ghBtn = PackageHubUIStyle.CreateButton(
                "GitHub ↗",
                "hub-btn-secondary",
                () =>
                {
                    Application.OpenURL(item.RepoUrl);
                }
            );
            actions.Add(ghBtn);

            metaRow.Add(actions);
            card.Add(metaRow);

            // Inline Release Notes Container
            if (item.ExpandedNotes && !string.IsNullOrEmpty(item.ReleaseNotes))
            {
                var notesContainer = new VisualElement();
                notesContainer.AddToClassList("hub-notes-container");
                notesContainer.style.backgroundColor = new StyleColor(new Color(0.08f, 0.09f, 0.12f));
                notesContainer.style.SetRadius(6);
                notesContainer.style.paddingTop = 10;
                notesContainer.style.paddingBottom = 10;
                notesContainer.style.paddingLeft = 12;
                notesContainer.style.paddingRight = 12;
                notesContainer.style.marginTop = 8;

                var notesHeader = new Label($"Release Notes for v{item.LatestRemoteVersion ?? item.InstalledVersion}");
                notesHeader.style.fontSize = 11;
                notesHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                notesHeader.style.color = new StyleColor(PackageHubUIStyle.ColAccent);
                notesHeader.style.marginBottom = 6;
                notesContainer.Add(notesHeader);

                var notesText = new Label(FormatReleaseNotes(item.ReleaseNotes));
                notesText.style.fontSize = 11;
                notesText.style.color = new StyleColor(new Color(0.80f, 0.84f, 0.89f));
                notesText.style.whiteSpace = WhiteSpace.Normal;
                notesContainer.Add(notesText);

                card.Add(notesContainer);
            }

            return card;
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
                    result.AppendLine("✦ Added:");
                else if (trimmed.StartsWith("### Fixed") || trimmed.StartsWith("#### Fixed"))
                    result.AppendLine("✔ Fixed:");
                else if (trimmed.StartsWith("### Changed") || trimmed.StartsWith("#### Changed"))
                    result.AppendLine("⚡ Changed:");
                else if (trimmed.StartsWith("###") || trimmed.StartsWith("##"))
                    result.AppendLine($"• {trimmed.TrimStart('#').Trim()}");
                else if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                    result.AppendLine($"  - {trimmed.Substring(2)}");
                else if (!string.IsNullOrWhiteSpace(line))
                    result.AppendLine(line);
                else
                    result.AppendLine();
            }

            return result.ToString().Trim();
        }

        #endregion

        #region Tab 2: Sobre Cezar Wagenheimer & Ecosystem

        private VisualElement BuildAboutView()
        {
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;

            // 1. Hero Bio Card
            var hero = new VisualElement();
            hero.AddToClassList("hub-about-hero");
            hero.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColCardBg);
            hero.style.borderTopWidth = 1;
            hero.style.borderRightWidth = 1;
            hero.style.borderBottomWidth = 1;
            hero.style.borderLeftWidth = 4;
            hero.style.borderTopColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            hero.style.borderRightColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            hero.style.borderBottomColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            hero.style.borderLeftColor = new StyleColor(PackageHubUIStyle.ColAccent);
            hero.style.SetRadius(8);
            hero.style.paddingTop = 18;
            hero.style.paddingBottom = 18;
            hero.style.paddingLeft = 18;
            hero.style.paddingRight = 18;
            hero.style.marginBottom = 12;

            var heroTitle = new Label("Cezar Wagenheimer");
            heroTitle.style.fontSize = 20;
            heroTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            heroTitle.style.color = new StyleColor(PackageHubUIStyle.ColTextWhite);
            hero.Add(heroTitle);

            var heroSubtitle = new Label("Lead Game Developer • Tools & Engine Systems Architect • Open-Source Maintainer");
            heroSubtitle.style.fontSize = 12;
            heroSubtitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            heroSubtitle.style.color = new StyleColor(PackageHubUIStyle.ColAccent);
            heroSubtitle.style.marginTop = 2;
            heroSubtitle.style.marginBottom = 8;
            hero.Add(heroSubtitle);

            var bioText = new Label(
                "Passionate game software engineer with extensive experience developing commercial cross-platform titles, " +
                "designing high-throughput CI/CD build automation, and engineering zero-friction Unity packages. " +
                "The Wagenheimer Unity Suite powers production-grade commercial games with clean code, robust architectural patterns, " +
                "and modern UI Toolkit developer workflows."
            );
            bioText.style.fontSize = 12;
            bioText.style.color = new StyleColor(new Color(0.80f, 0.84f, 0.89f));
            bioText.style.whiteSpace = WhiteSpace.Normal;
            bioText.style.marginBottom = 12;
            hero.Add(bioText);

            // Social & Contact links
            var socials = new VisualElement();
            socials.AddToClassList("hub-hero-socials");
            socials.style.flexDirection = FlexDirection.Row;
            socials.style.alignItems = Align.Center;
            socials.style.flexWrap = Wrap.Wrap;

            var b1 = PackageHubUIStyle.CreateButton("🌐 Official Website (wagenheimer.com)", "hub-btn-primary", () =>
            {
                Application.OpenURL("https://wagenheimer.com");
            });
            b1.style.marginRight = 6;
            b1.style.marginBottom = 6;
            socials.Add(b1);

            var b2 = PackageHubUIStyle.CreateButton("🐙 GitHub (@wagenheimer)", "hub-btn-secondary", () =>
            {
                Application.OpenURL("https://github.com/wagenheimer");
            });
            b2.style.marginRight = 6;
            b2.style.marginBottom = 6;
            socials.Add(b2);

            var b3 = PackageHubUIStyle.CreateButton("💼 LinkedIn Profile", "hub-btn-secondary", () =>
            {
                Application.OpenURL("https://www.linkedin.com/in/cezar-wagenheimer/");
            });
            b3.style.marginRight = 6;
            b3.style.marginBottom = 6;
            socials.Add(b3);

            var b4 = PackageHubUIStyle.CreateButton("✉️ Contact & Support", "hub-btn-secondary", () =>
            {
                Application.OpenURL("mailto:cezar@wagenheimer.com");
            });
            b4.style.marginBottom = 6;
            socials.Add(b4);

            hero.Add(socials);
            scroll.Add(hero);

            // 2. Architectural Principles Card
            var principlesHeader = new Label("Engineering Philosophy & Principles");
            principlesHeader.style.fontSize = 13;
            principlesHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            principlesHeader.style.color = new StyleColor(PackageHubUIStyle.ColTextWhite);
            principlesHeader.style.marginBottom = 8;
            principlesHeader.style.marginTop = 6;
            scroll.Add(principlesHeader);

            scroll.Add(BuildPrincipleItem(
                "⚡ Zero-Boilerplate & Frictionless Setup",
                "Every package in the suite is designed to initialize and configure itself out of the box with intelligent defaults. No tedious XML/JSON editing or fragile boilerplate code."
            ));

            scroll.Add(BuildPrincipleItem(
                "🛡️ Commercial Battle-Tested",
                "Proven in live-ops commercial titles with millions of downloads across Steam, Google Play, Apple App Store, and macOS. Engineered to handle memory constraints and multi-platform nuances."
            ));

            scroll.Add(BuildPrincipleItem(
                "🧩 Decoupled & Modular Architecture",
                "Zero forced monolithic dependencies. Each package fulfills one responsibility with precision. Use only what your game requires, whether it is CloudSave, RateControl, or BuildPipeline."
            ));

            scroll.Add(BuildPrincipleItem(
                "🔄 Continuous Automation & SemVer",
                "Enforces Conventional Commits (feat, fix, refactor), automated CHANGELOG extraction, GitHub-backed UPM delivery, and non-intrusive background update discovery."
            ));

            // 3. Full Ecosystem Directory Grid
            var ecoHeader = new Label("The Wagenheimer Unity Ecosystem");
            ecoHeader.style.fontSize = 13;
            ecoHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            ecoHeader.style.color = new StyleColor(PackageHubUIStyle.ColTextWhite);
            ecoHeader.style.marginBottom = 8;
            ecoHeader.style.marginTop = 12;
            scroll.Add(ecoHeader);

            var grid = new VisualElement();
            grid.AddToClassList("hub-ecosystem-grid");
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.justifyContent = Justify.SpaceBetween;

            foreach (var pkg in PackageCatalog.KnownPackages)
            {
                var isInst = _allPackages.Any(p => p.IsInstalled && string.Equals(p.PackageId, pkg.PackageId, StringComparison.OrdinalIgnoreCase));
                var cell = new VisualElement();
                cell.AddToClassList("hub-ecosystem-cell");
                cell.style.width = Length.Percent(49);
                cell.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColCardBg);
                cell.style.borderTopWidth = 1;
                cell.style.borderBottomWidth = 1;
                cell.style.borderLeftWidth = 1;
                cell.style.borderRightWidth = 1;
                cell.style.borderTopColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
                cell.style.borderBottomColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
                cell.style.borderLeftColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
                cell.style.borderRightColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
                cell.style.SetRadius(6);
                cell.style.paddingTop = 10;
                cell.style.paddingBottom = 10;
                cell.style.paddingLeft = 10;
                cell.style.paddingRight = 10;
                cell.style.marginBottom = 8;

                var topCell = new VisualElement();
                topCell.style.flexDirection = FlexDirection.Row;
                topCell.style.justifyContent = Justify.SpaceBetween;
                topCell.style.alignItems = Align.Center;

                var nameLbl = new Label(pkg.DisplayName);
                nameLbl.style.fontSize = 12;
                nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                nameLbl.style.color = new StyleColor(PackageHubUIStyle.ColTextWhite);
                topCell.Add(nameLbl);

                var stBadge = PackageHubUIStyle.CreateBadge(
                    isInst ? "INSTALLED" : "AVAILABLE",
                    isInst ? "hub-badge-pass" : "hub-badge-neutral"
                );
                topCell.Add(stBadge);
                cell.Add(topCell);

                var dLbl = new Label(pkg.Description);
                dLbl.style.fontSize = 10.5f;
                dLbl.style.color = new StyleColor(PackageHubUIStyle.ColTextMuted);
                dLbl.style.marginTop = 4;
                dLbl.style.whiteSpace = WhiteSpace.Normal;
                cell.Add(dLbl);

                var cellFooter = new VisualElement();
                cellFooter.style.flexDirection = FlexDirection.Row;
                cellFooter.style.justifyContent = Justify.SpaceBetween;
                cellFooter.style.alignItems = Align.Center;
                cellFooter.style.marginTop = 6;

                var catLbl = new Label(pkg.Category);
                catLbl.style.fontSize = 9.5f;
                catLbl.style.color = new StyleColor(PackageHubUIStyle.ColAccent);
                cellFooter.Add(catLbl);

                var ghLink = new Label("Repo ↗");
                ghLink.style.fontSize = 10;
                ghLink.style.color = new StyleColor(new Color(0.5f, 0.7f, 1f));
                ghLink.RegisterCallback<ClickEvent>(_ => Application.OpenURL(pkg.RepoUrl));
                cellFooter.Add(ghLink);

                cell.Add(cellFooter);
                grid.Add(cell);
            }

            scroll.Add(grid);
            return scroll;
        }

        private VisualElement BuildPrincipleItem(string title, string description)
        {
            var card = new VisualElement();
            card.AddToClassList("hub-principle-card");
            card.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColCardBg);
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            card.style.borderBottomColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            card.style.borderLeftColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            card.style.borderRightColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            card.style.SetRadius(6);
            card.style.paddingTop = 10;
            card.style.paddingBottom = 10;
            card.style.paddingLeft = 12;
            card.style.paddingRight = 12;
            card.style.marginBottom = 8;

            var t = new Label(title);
            t.style.fontSize = 12;
            t.style.unityFontStyleAndWeight = FontStyle.Bold;
            t.style.color = new StyleColor(PackageHubUIStyle.ColTextWhite);
            t.style.marginBottom = 3;
            card.Add(t);

            var d = new Label(description);
            d.style.fontSize = 11;
            d.style.color = new StyleColor(PackageHubUIStyle.ColTextMuted);
            d.style.whiteSpace = WhiteSpace.Normal;
            card.Add(d);

            return card;
        }

        #endregion

        #region Tab 3: Settings & Maintenance View

        private VisualElement BuildSettingsView()
        {
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;

            // Auto-check settings card
            var autoCard = new VisualElement();
            autoCard.AddToClassList("hub-card");
            autoCard.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColCardBg);
            autoCard.style.SetRadius(8);
            autoCard.style.paddingTop = 12;
            autoCard.style.paddingBottom = 12;
            autoCard.style.paddingLeft = 14;
            autoCard.style.paddingRight = 14;
            autoCard.style.marginBottom = 10;

            var autoTitle = new Label("Automated Background Checks");
            autoTitle.style.fontSize = 13;
            autoTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            autoTitle.style.color = new StyleColor(PackageHubUIStyle.ColTextWhite);
            autoCard.Add(autoTitle);

            var autoCheck = EditorPrefs.GetBool(PackageHubAutoChecker.PrefAutoCheck, true);
            var checkToggle = new Toggle("Check for Package Updates on Editor Startup (runs once daily in background)")
            {
                value = autoCheck
            };
            checkToggle.RegisterValueChangedCallback(evt =>
            {
                EditorPrefs.SetBool(PackageHubAutoChecker.PrefAutoCheck, evt.newValue);
            });
            checkToggle.style.marginTop = 8;
            autoCard.Add(checkToggle);

            var autoOpen = EditorPrefs.GetBool(PackageHubAutoChecker.PrefAutoOpenWindow, false);
            var openToggle = new Toggle("Automatically open Hub window when new package updates are discovered")
            {
                value = autoOpen
            };
            openToggle.RegisterValueChangedCallback(evt =>
            {
                EditorPrefs.SetBool(PackageHubAutoChecker.PrefAutoOpenWindow, evt.newValue);
            });
            openToggle.style.marginTop = 6;
            autoCard.Add(openToggle);

            scroll.Add(autoCard);

            // Cache & Maintenance card
            var cacheCard = new VisualElement();
            cacheCard.AddToClassList("hub-card");
            cacheCard.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColCardBg);
            cacheCard.style.SetRadius(8);
            cacheCard.style.paddingTop = 12;
            cacheCard.style.paddingBottom = 12;
            cacheCard.style.paddingLeft = 14;
            cacheCard.style.paddingRight = 14;
            cacheCard.style.marginBottom = 10;

            var cacheTitle = new Label("Cache & Package Operations");
            cacheTitle.style.fontSize = 13;
            cacheTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            cacheTitle.style.color = new StyleColor(PackageHubUIStyle.ColTextWhite);
            cacheCard.Add(cacheTitle);

            var cacheActions = new VisualElement();
            cacheActions.style.flexDirection = FlexDirection.Row;
            cacheActions.style.marginTop = 8;

            var btnRescan = PackageHubUIStyle.CreateButton("Force Re-scan Packages", "hub-btn-secondary", () =>
            {
                RefreshPackages(true);
                RebuildHeader();
                RebuildMetrics();
                RenderActiveTab();
            });
            btnRescan.style.marginRight = 6;
            cacheActions.Add(btnRescan);

            var btnClearTs = PackageHubUIStyle.CreateButton("Clear Check Schedule Timestamp", "hub-btn-secondary", () =>
            {
                EditorPrefs.DeleteKey(PackageHubAutoChecker.PrefLastCheck);
                Debug.Log("[Wagenheimer Package Hub] Reset check schedule. Next startup will check automatically.");
            });
            btnClearTs.style.marginRight = 6;
            cacheActions.Add(btnClearTs);

            var btnUpm = PackageHubUIStyle.CreateButton("Open Unity Package Manager", "hub-btn-secondary", () =>
            {
                UnityEditor.PackageManager.UI.Window.Open("");
            });
            cacheActions.Add(btnUpm);

            cacheCard.Add(cacheActions);
            scroll.Add(cacheCard);

            // Package Information
            var infoCard = new VisualElement();
            infoCard.AddToClassList("hub-card");
            infoCard.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColCardBg);
            infoCard.style.SetRadius(8);
            infoCard.style.paddingTop = 12;
            infoCard.style.paddingBottom = 12;
            infoCard.style.paddingLeft = 14;
            infoCard.style.paddingRight = 14;

            var infoTitle = new Label("Package Hub Architecture");
            infoTitle.style.fontSize = 13;
            infoTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            infoTitle.style.color = new StyleColor(PackageHubUIStyle.ColTextWhite);
            infoCard.Add(infoTitle);

            AddInfoRow(infoCard, "Package Name", "com.wagenheimer.packagehub");
            AddInfoRow(infoCard, "Installed Version", _hubVersion);
            AddInfoRow(infoCard, "Author", "Cezar Wagenheimer");
            AddInfoRow(infoCard, "License", "MIT");
            AddInfoRow(infoCard, "GitHub Repository", "https://github.com/wagenheimer/UnityPackageHub");

            scroll.Add(infoCard);

            return scroll;
        }

        private static void AddInfoRow(VisualElement container, string label, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.paddingTop = 4;
            row.style.paddingBottom = 4;

            var labelElem = new Label(label);
            labelElem.style.color = new StyleColor(PackageHubUIStyle.ColTextMuted);
            labelElem.style.fontSize = 11;

            var valElem = new Label(value);
            valElem.style.fontSize = 11;
            valElem.style.unityFontStyleAndWeight = FontStyle.Bold;
            valElem.style.color = new StyleColor(PackageHubUIStyle.ColTextWhite);

            row.Add(labelElem);
            row.Add(valElem);
            container.Add(row);
        }

        #endregion

        #region Footer Component

        private VisualElement BuildFooter()
        {
            var footer = new VisualElement();
            footer.AddToClassList("hub-footer");
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.alignItems = Align.Center;
            footer.style.justifyContent = Justify.SpaceBetween;
            footer.style.backgroundColor = new StyleColor(PackageHubUIStyle.ColCardBg);
            footer.style.borderTopWidth = 1;
            footer.style.borderBottomWidth = 1;
            footer.style.borderLeftWidth = 1;
            footer.style.borderRightWidth = 1;
            footer.style.borderTopColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            footer.style.borderBottomColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            footer.style.borderLeftColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            footer.style.borderRightColor = new StyleColor(PackageHubUIStyle.ColCardBorder);
            footer.style.SetRadius(6);
            footer.style.paddingTop = 6;
            footer.style.paddingBottom = 6;
            footer.style.paddingLeft = 12;
            footer.style.paddingRight = 12;
            footer.style.marginTop = 10;

            var left = new Label($"Wagenheimer Package Hub v{_hubVersion} • Built with Unity UI Toolkit");
            left.style.fontSize = 10.5f;
            left.style.color = new StyleColor(new Color(0.45f, 0.50f, 0.60f));
            footer.Add(left);

            var links = new VisualElement();
            links.AddToClassList("hub-footer-links");
            links.style.flexDirection = FlexDirection.Row;
            links.style.alignItems = Align.Center;

            var b1 = PackageHubUIStyle.CreateButton("🌐 wagenheimer.com", "hub-btn-secondary", () =>
            {
                Application.OpenURL("https://wagenheimer.com");
            });
            b1.style.paddingTop = 3;
            b1.style.paddingBottom = 3;
            b1.style.paddingLeft = 8;
            b1.style.paddingRight = 8;
            b1.style.fontSize = 10;
            b1.style.marginRight = 6;
            links.Add(b1);

            var b2 = PackageHubUIStyle.CreateButton("GitHub ↗", "hub-btn-secondary", () =>
            {
                Application.OpenURL("https://github.com/wagenheimer");
            });
            b2.style.paddingTop = 3;
            b2.style.paddingBottom = 3;
            b2.style.paddingLeft = 8;
            b2.style.paddingRight = 8;
            b2.style.fontSize = 10;
            b2.style.marginRight = 6;
            links.Add(b2);

            var b3 = PackageHubUIStyle.CreateButton("Unity UPM", "hub-btn-secondary", () =>
            {
                UnityEditor.PackageManager.UI.Window.Open("");
            });
            b3.style.paddingTop = 3;
            b3.style.paddingBottom = 3;
            b3.style.paddingLeft = 8;
            b3.style.paddingRight = 8;
            b3.style.fontSize = 10;
            links.Add(b3);

            footer.Add(links);
            return footer;
        }

        #endregion

        #region Operations & Updates

        public void CheckAllUpdates()
        {
            _isCheckingAll = true;
            RebuildHeader();

            var toCheck = _allPackages.Where(p => p.IsInstalled).ToList();
            if (toCheck.Count == 0) toCheck = _allPackages;

            PackageUpdateService.CheckUpdates(toCheck, () =>
            {
                _isCheckingAll = false;
                RebuildHeader();
                RebuildMetrics();
                RenderActiveTab();
            });
        }

        private void UpdateAllOutdated()
        {
            var outdated = _allPackages.Where(p => p.IsInstalled && p.HasUpdate).ToList();
            if (outdated.Count == 0) return;

            PackageInstaller.UpdateAll(outdated, () =>
            {
                RefreshPackages(false);
                RebuildHeader();
                RebuildMetrics();
                RenderActiveTab();
            });
        }

        private void RefreshPackages(bool triggerRemoteCheck)
        {
            _allPackages = PackageDiscovery.GetAllPackages();
            if (triggerRemoteCheck)
            {
                CheckAllUpdates();
            }
        }

        private void LoadPackageVersion()
        {
            try
            {
                if (File.Exists(PackageJsonPath))
                {
                    var json = File.ReadAllText(PackageJsonPath);
                    var match = Regex.Match(json, "\"version\"\\s*:\\s*\"([^\"]+)\"");
                    if (match.Success)
                    {
                        _hubVersion = match.Groups[1].Value;
                        return;
                    }
                }
            }
            catch
            {
                // Ignore, use fallback
            }
            _hubVersion = "1.1.0";
        }

        #endregion
    }
}
