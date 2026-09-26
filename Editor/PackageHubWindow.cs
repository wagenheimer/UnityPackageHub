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
        private string _hubVersion = "1.0.5";

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
            PackageHubUIStyle.Apply(rootVisualElement);

            var root = new VisualElement();
            root.AddToClassList("hub-root");

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
                _tabButtons[i] = btn;
                tabToolbar.Add(btn);
            }
            root.Add(tabToolbar);

            // 4. Dynamic Content Area
            _contentContainer = new VisualElement();
            _contentContainer.AddToClassList("hub-content-container");
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

            // Installed card
            row.Add(PackageHubUIStyle.CreateMetricCard("Installed in Project", $"{installedCount} / {totalCount}", out var instVal));
            instVal.style.color = new StyleColor(new Color(0.22f, 0.74f, 0.97f)); // #38BDF8

            // Up to Date card
            row.Add(PackageHubUIStyle.CreateMetricCard("Up to Date", upToDateCount.ToString(), out var upVal));
            upVal.style.color = new StyleColor(new Color(0.10f, 0.73f, 0.51f)); // #10B981

            // Updates Available card
            row.Add(PackageHubUIStyle.CreateMetricCard("Updates Available", updateCount.ToString(), out var upAvailVal));
            if (updateCount > 0)
                upAvailVal.style.color = new StyleColor(new Color(0.96f, 0.62f, 0.04f)); // #F59E0B
            else
                upAvailVal.style.color = new StyleColor(new Color(0.58f, 0.64f, 0.72f));

            // Dashboards Ready card
            row.Add(PackageHubUIStyle.CreateMetricCard("Dashboards Ready", $"{dashboards.Count} Active", out var dashVal));
            dashVal.style.color = new StyleColor(new Color(0.65f, 0.55f, 0.98f)); // #A78BFA

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
                        _tabButtons[i].AddToClassList("hub-tab-btn--active");
                    else
                        _tabButtons[i].RemoveFromClassList("hub-tab-btn--active");
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

                var qHeader = new VisualElement();
                qHeader.AddToClassList("hub-quickbar-header");

                var qTitle = new Label("⚡ QUICK LAUNCH DASHBOARDS — INSTANT 1-CLICK ACCESS");
                qTitle.AddToClassList("hub-quickbar-title");
                qHeader.Add(qTitle);

                var qCountBadge = PackageHubUIStyle.CreateBadge($"{dashboards.Count} Available", "hub-badge-info");
                qHeader.Add(qCountBadge);
                quickBar.Add(qHeader);

                var chipRow = new VisualElement();
                chipRow.AddToClassList("hub-quickbar-chips");

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
                var emptyLbl = new Label("No installed Wagenheimer packages match your search filter.");
                emptyLbl.style.color = new StyleColor(new Color(0.6f, 0.65f, 0.72f));
                emptyLbl.style.paddingTop = 15;
                emptyLbl.style.paddingBottom = 15;
                emptyLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                emptyCard.Add(emptyLbl);
                scroll.Add(emptyCard);
            }
            else
            {
                // Put packages with updates first, then alphabetical
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

            var searchField = new TextField { value = _searchFilter };
            searchField.AddToClassList("hub-search-field");
            searchField.RegisterValueChangedCallback(evt =>
            {
                _searchFilter = evt.newValue;
                RenderActiveTab();
            });
            bar.Add(searchField);

            var chipRow = new VisualElement();
            chipRow.AddToClassList("hub-filter-chips");

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
                if (isSelected)
                {
                    chip.AddToClassList("hub-filter-chip--active");
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
            if (isFocused) card.AddToClassList("hub-card--focused");

            // Top Header: Name, Category, Badges, and Main Actions
            var header = new VisualElement();
            header.AddToClassList("hub-card-header");

            var identity = new VisualElement();
            identity.AddToClassList("hub-card-identity");

            var titleLbl = new Label(item.DisplayName);
            titleLbl.AddToClassList("hub-card-title");
            identity.Add(titleLbl);

            var catBadge = new Label(item.Category);
            catBadge.AddToClassList("hub-card-category");
            identity.Add(catBadge);

            header.Add(identity);

            // Badges row
            var badgesRow = new VisualElement();
            badgesRow.AddToClassList("hub-card-badges");

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
                    var upBadge = PackageHubUIStyle.CreateBadge($"UPDATE: v{item.InstalledVersion} ➔ v{item.LatestRemoteVersion}", "hub-badge-update");
                    badgesRow.Add(upBadge);
                }
                else
                {
                    var upBadge = PackageHubUIStyle.CreateBadge($"v{item.InstalledVersion} (Latest)", "hub-badge-pass");
                    badgesRow.Add(upBadge);
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
            card.Add(desc);

            // Meta Row with Dashboard shortcuts, actions, and Package ID
            var metaRow = new VisualElement();
            metaRow.AddToClassList("hub-card-meta-row");

            var leftMeta = new VisualElement();
            leftMeta.style.flexDirection = FlexDirection.Column;

            var idLabel = new Label(item.PackageId);
            idLabel.AddToClassList("hub-card-id");
            leftMeta.Add(idLabel);

            // DASHBOARD SHORTCUTS FOR INSTALLED PACKAGE
            var dashInfo = PackageDashboardLauncher.GetDashboardInfo(item.PackageId);
            if (item.IsInstalled && dashInfo != null)
            {
                var shortcutsRow = new VisualElement();
                shortcutsRow.AddToClassList("hub-shortcuts-row");
                shortcutsRow.style.marginTop = 4;

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
                    dashBtn.AddToClassList("hub-btn-sm");
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
                    secBtn.AddToClassList("hub-btn-sm");
                    secBtn.tooltip = $"Open {sec.Title} ({sec.MenuItemPath})";
                    shortcutsRow.Add(secBtn);
                }

                leftMeta.Add(shortcutsRow);
            }

            metaRow.Add(leftMeta);

            // Right Actions: Install / Update, Changelog, GitHub
            var actions = new VisualElement();
            actions.AddToClassList("hub-card-actions");

            if (item.IsInstalled)
            {
                if (item.HasUpdate)
                {
                    var updateBtn = PackageHubUIStyle.CreateButton(
                        $"Update to v{item.LatestRemoteVersion}",
                        "hub-btn-success",
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

                var notesHeader = new Label($"Release Notes for v{item.LatestRemoteVersion ?? item.InstalledVersion}");
                notesHeader.AddToClassList("hub-notes-header");
                notesContainer.Add(notesHeader);

                var notesText = new Label(FormatReleaseNotes(item.ReleaseNotes));
                notesText.AddToClassList("hub-notes-text");
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

            var heroTitle = new Label("Cezar Wagenheimer");
            heroTitle.AddToClassList("hub-hero-title");
            hero.Add(heroTitle);

            var heroSubtitle = new Label("Lead Game Developer • Tools & Engine Systems Architect • Open-Source Maintainer");
            heroSubtitle.AddToClassList("hub-hero-subtitle");
            hero.Add(heroSubtitle);

            var bioText = new Label(
                "Passionate game software engineer with extensive experience developing commercial cross-platform titles, " +
                "designing high-throughput CI/CD build automation, and engineering zero-friction Unity packages. " +
                "The Wagenheimer Unity Suite powers production-grade commercial games with clean code, robust architectural patterns, " +
                "and modern UI Toolkit developer workflows."
            );
            bioText.AddToClassList("hub-hero-bio");
            hero.Add(bioText);

            // Social & Contact links
            var socials = new VisualElement();
            socials.AddToClassList("hub-hero-socials");

            socials.Add(PackageHubUIStyle.CreateButton("🌐 Official Website (wagenheimer.com)", "hub-btn-primary", () =>
            {
                Application.OpenURL("https://wagenheimer.com");
            }));

            socials.Add(PackageHubUIStyle.CreateButton("🐙 GitHub (@wagenheimer)", "hub-btn-secondary", () =>
            {
                Application.OpenURL("https://github.com/wagenheimer");
            }));

            socials.Add(PackageHubUIStyle.CreateButton("💼 LinkedIn Profile", "hub-btn-secondary", () =>
            {
                Application.OpenURL("https://www.linkedin.com/in/cezar-wagenheimer/");
            }));

            socials.Add(PackageHubUIStyle.CreateButton("✉️ Contact & Support", "hub-btn-secondary", () =>
            {
                Application.OpenURL("mailto:cezar@wagenheimer.com");
            }));

            hero.Add(socials);
            scroll.Add(hero);

            // 2. Architectural Principles Card
            var principlesHeader = new Label("Engineering Philosophy & Principles");
            principlesHeader.AddToClassList("hub-card-title");
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
            ecoHeader.AddToClassList("hub-card-title");
            ecoHeader.style.marginBottom = 8;
            ecoHeader.style.marginTop = 12;
            scroll.Add(ecoHeader);

            var grid = new VisualElement();
            grid.AddToClassList("hub-ecosystem-grid");

            foreach (var pkg in PackageCatalog.KnownPackages)
            {
                var isInst = _allPackages.Any(p => p.IsInstalled && string.Equals(p.PackageId, pkg.PackageId, StringComparison.OrdinalIgnoreCase));
                var cell = new VisualElement();
                cell.AddToClassList("hub-ecosystem-cell");

                var topCell = new VisualElement();
                topCell.style.flexDirection = FlexDirection.Row;
                topCell.style.justifyContent = Justify.SpaceBetween;
                topCell.style.alignItems = Align.Center;

                var nameLbl = new Label(pkg.DisplayName);
                nameLbl.style.fontSize = 12;
                nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                topCell.Add(nameLbl);

                var stBadge = PackageHubUIStyle.CreateBadge(
                    isInst ? "INSTALLED" : "AVAILABLE",
                    isInst ? "hub-badge-pass" : "hub-badge-neutral"
                );
                topCell.Add(stBadge);
                cell.Add(topCell);

                var dLbl = new Label(pkg.Description);
                dLbl.style.fontSize = 10.5f;
                dLbl.style.color = new StyleColor(new Color(0.6f, 0.65f, 0.72f));
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
                catLbl.style.color = new StyleColor(new Color(0.22f, 0.74f, 0.97f));
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

            var t = new Label(title);
            t.AddToClassList("hub-principle-title");
            card.Add(t);

            var d = new Label(description);
            d.AddToClassList("hub-principle-desc");
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

            var autoTitle = new Label("Automated Background Checks");
            autoTitle.AddToClassList("hub-card-title");
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

            var cacheTitle = new Label("Cache & Package Operations");
            cacheTitle.AddToClassList("hub-card-title");
            cacheCard.Add(cacheTitle);

            var cacheActions = new VisualElement();
            cacheActions.style.flexDirection = FlexDirection.Row;
            cacheActions.style.marginTop = 8;

            cacheActions.Add(PackageHubUIStyle.CreateButton("Force Re-scan Packages", "hub-btn-secondary", () =>
            {
                RefreshPackages(true);
                RebuildHeader();
                RebuildMetrics();
                RenderActiveTab();
            }));

            cacheActions.Add(PackageHubUIStyle.CreateButton("Clear Check Schedule Timestamp", "hub-btn-secondary", () =>
            {
                EditorPrefs.DeleteKey(PackageHubAutoChecker.PrefLastCheck);
                Debug.Log("[Wagenheimer Package Hub] Reset check schedule. Next startup will check automatically.");
            }));

            cacheActions.Add(PackageHubUIStyle.CreateButton("Open Unity Package Manager", "hub-btn-secondary", () =>
            {
                UnityEditor.PackageManager.UI.Window.Open("");
            }));

            cacheCard.Add(cacheActions);
            scroll.Add(cacheCard);

            // Package Information
            var infoCard = new VisualElement();
            infoCard.AddToClassList("hub-card");

            var infoTitle = new Label("Package Hub Architecture");
            infoTitle.AddToClassList("hub-card-title");
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
            labelElem.style.color = new StyleColor(new Color(0.6f, 0.65f, 0.72f));
            labelElem.style.fontSize = 11;

            var valElem = new Label(value);
            valElem.style.fontSize = 11;
            valElem.style.unityFontStyleAndWeight = FontStyle.Bold;

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

            var left = new Label($"Wagenheimer Package Hub v{_hubVersion} • Built with Unity UI Toolkit");
            left.AddToClassList("hub-footer-text");
            footer.Add(left);

            var links = new VisualElement();
            links.AddToClassList("hub-footer-links");

            links.Add(PackageHubUIStyle.CreateButton("🌐 wagenheimer.com", "hub-btn-secondary hub-btn-sm", () =>
            {
                Application.OpenURL("https://wagenheimer.com");
            }));

            links.Add(PackageHubUIStyle.CreateButton("GitHub ↗", "hub-btn-secondary hub-btn-sm", () =>
            {
                Application.OpenURL("https://github.com/wagenheimer");
            }));

            links.Add(PackageHubUIStyle.CreateButton("Unity UPM", "hub-btn-secondary hub-btn-sm", () =>
            {
                UnityEditor.PackageManager.UI.Window.Open("");
            }));

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
