using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Wagenheimer.PackageHub.Editor
{
    public class ToolShortcut
    {
        public string Title;
        public string MenuItemPath;
        public string TypeName;
        public string MethodName;
        public string Icon;
        public bool IsPrimary;
    }

    public class PackageDashboardInfo
    {
        public string PackageId;
        public string DisplayName;
        public string PrimaryTitle;
        public string Icon;
        public ToolShortcut PrimaryDashboard;
        public List<ToolShortcut> SecondaryTools = new List<ToolShortcut>();
    }

    /// <summary>
    /// Automated discovery and launch engine for Wagenheimer package dashboards and setup windows.
    /// Uses reflection and EditorApplication.ExecuteMenuItem to decouple dependencies.
    /// </summary>
    public static class PackageDashboardLauncher
    {
        private static readonly Dictionary<string, PackageDashboardInfo> Registry = new Dictionary<string, PackageDashboardInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["com.wagenheimer.unityutils"] = new PackageDashboardInfo
            {
                PackageId = "com.wagenheimer.unityutils",
                DisplayName = "Unity Utils",
                PrimaryTitle = "Utils Dashboard",
                Icon = "⚡",
                PrimaryDashboard = new ToolShortcut
                {
                    Title = "Dashboard",
                    MenuItemPath = "Tools/Wagenheimer/Unity Utils/Dashboard...",
                    TypeName = "Wagenheimer.UnityUtils.Editor.UnityUtilsHubWindow",
                    MethodName = "OpenDashboard",
                    Icon = "⚡",
                    IsPrimary = true
                },
                SecondaryTools = new List<ToolShortcut>
                {
                    new ToolShortcut { Title = "Project Cleanup", MenuItemPath = "Tools/Wagenheimer/Unity Utils/Cleanup/Open Project Cleanup...", Icon = "🧹" },
                    new ToolShortcut { Title = "Third-Party Slimmer", MenuItemPath = "Tools/Wagenheimer/Unity Utils/Third-Party/Open Third-Party Slimmer...", Icon = "📦" },
                    new ToolShortcut { Title = "Diagnostics", MenuItemPath = "Tools/Wagenheimer/Unity Utils/Bootstrap/Run Diagnostic Checker...", Icon = "🔍" }
                }
            },
            ["com.wagenheimer.buildpipeline"] = new PackageDashboardInfo
            {
                PackageId = "com.wagenheimer.buildpipeline",
                DisplayName = "Build Pipeline",
                PrimaryTitle = "Build Window",
                Icon = "🔨",
                PrimaryDashboard = new ToolShortcut
                {
                    Title = "Build Window",
                    MenuItemPath = "Tools/Wagenheimer/Build Pipeline/Open Build Window",
                    TypeName = "Wagenheimer.BuildPipeline.Editor.BuildPipelineWindow",
                    MethodName = "OpenBuildWindow",
                    Icon = "🔨",
                    IsPrimary = true
                },
                SecondaryTools = new List<ToolShortcut>
                {
                    new ToolShortcut { Title = "Guide & Docs", MenuItemPath = "Tools/Wagenheimer/Build Pipeline/Documentation & Integration Guide", Icon = "📖" },
                    new ToolShortcut { Title = "Config Migration", MenuItemPath = "Tools/Wagenheimer/Build Pipeline/Migrate or Create Project Config", Icon = "⚙️" }
                }
            },
            ["com.wagenheimer.ratecontrol"] = new PackageDashboardInfo
            {
                PackageId = "com.wagenheimer.ratecontrol",
                DisplayName = "Rate Control",
                PrimaryTitle = "Rate Control Hub",
                Icon = "⭐",
                PrimaryDashboard = new ToolShortcut
                {
                    Title = "Dashboard",
                    MenuItemPath = "Tools/Wagenheimer/Rate Control/Dashboard...",
                    TypeName = "Wagenheimer.RateControl.Editor.RateControlHubWindow",
                    MethodName = "OpenDashboard",
                    Icon = "⭐",
                    IsPrimary = true
                },
                SecondaryTools = new List<ToolShortcut>
                {
                    new ToolShortcut { Title = "Live QA & Tester", MenuItemPath = "Tools/Wagenheimer/Rate Control/Live QA & Testing...", Icon = "🧪" },
                    new ToolShortcut { Title = "Setup Checklist", MenuItemPath = "Tools/Wagenheimer/Rate Control/Setup & Diagnostics...", Icon = "📋" }
                }
            },
            ["com.wagenheimer.iaphelper"] = new PackageDashboardInfo
            {
                PackageId = "com.wagenheimer.iaphelper",
                DisplayName = "IAP Helper",
                PrimaryTitle = "IAP Dashboard",
                Icon = "💳",
                PrimaryDashboard = new ToolShortcut
                {
                    Title = "IAP Dashboard",
                    MenuItemPath = "Tools/Wagenheimer/IAP Helper/Dashboard",
                    TypeName = "Wagenheimer.IAPHelper.Editor.IAPHelperDashboardWindow",
                    MethodName = "ShowWindow",
                    Icon = "💳",
                    IsPrimary = true
                },
                SecondaryTools = new List<ToolShortcut>
                {
                    new ToolShortcut { Title = "Verify Setup", MenuItemPath = "Tools/Wagenheimer/IAP Helper/Verify Setup...", Icon = "🔍" }
                }
            },
            ["com.wagenheimer.cloudsave"] = new PackageDashboardInfo
            {
                PackageId = "com.wagenheimer.cloudsave",
                DisplayName = "Cloud Save",
                PrimaryTitle = "Cloud Save Dashboard",
                Icon = "☁️",
                PrimaryDashboard = new ToolShortcut
                {
                    Title = "Dashboard",
                    MenuItemPath = "Tools/Wagenheimer/Cloud Save/Dashboard...",
                    TypeName = "Wagenheimer.CloudSave.Editor.UI.CloudSaveHubWindow",
                    MethodName = "Open",
                    Icon = "☁️",
                    IsPrimary = true
                },
                SecondaryTools = new List<ToolShortcut>
                {
                    new ToolShortcut { Title = "Setup & Verify", MenuItemPath = "Tools/Wagenheimer/Cloud Save/Setup && Verification...", Icon = "⚙️" },
                    new ToolShortcut { Title = "Test Window", MenuItemPath = "Tools/Wagenheimer/Cloud Save/Cloud Tester (Legacy)...", Icon = "🧪" },
                    new ToolShortcut { Title = "Audit Integration", MenuItemPath = "Tools/Wagenheimer/Cloud Save/Audit Integration", Icon = "🔍" }
                }
            },
            ["com.wagenheimer.levelplayhelper"] = new PackageDashboardInfo
            {
                PackageId = "com.wagenheimer.levelplayhelper",
                DisplayName = "LevelPlay Helper",
                PrimaryTitle = "LevelPlay Manager",
                Icon = "📊",
                PrimaryDashboard = new ToolShortcut
                {
                    Title = "LevelPlay Manager",
                    MenuItemPath = "Tools/Wagenheimer/Level Play Helper/LevelPlay Manager...",
                    TypeName = "Wagenheimer.LevelPlayHelper.Editor.LevelPlaySetupWindow",
                    MethodName = "ShowWindow",
                    Icon = "📊",
                    IsPrimary = true
                }
            },
            ["com.wagenheimer.rewiredhelper"] = new PackageDashboardInfo
            {
                PackageId = "com.wagenheimer.rewiredhelper",
                DisplayName = "Rewired Helper",
                PrimaryTitle = "Rewired Setup",
                Icon = "🎮",
                PrimaryDashboard = new ToolShortcut
                {
                    Title = "Setup & Help",
                    MenuItemPath = "Tools/Wagenheimer/Rewired Helper/Setup Checker & Help",
                    TypeName = "Wagenheimer.RewiredHelper.Editor.RewiredHelperSetupWindow",
                    MethodName = "ShowWindow",
                    Icon = "🎮",
                    IsPrimary = true
                },
                SecondaryTools = new List<ToolShortcut>
                {
                    new ToolShortcut { Title = "Create Manager", MenuItemPath = "Tools/Wagenheimer/Rewired Helper/Create Rewired Input Manager", Icon = "⚙️" },
                    new ToolShortcut { Title = "Create Event System", MenuItemPath = "Tools/Wagenheimer/Rewired Helper/Create Rewired Event System", Icon = "🖱️" }
                }
            },
            ["com.wagenheimer.nativesocial"] = new PackageDashboardInfo
            {
                PackageId = "com.wagenheimer.nativesocial",
                DisplayName = "Native Social",
                PrimaryTitle = "Social Dashboard",
                Icon = "📱",
                PrimaryDashboard = new ToolShortcut
                {
                    Title = "Dashboard",
                    MenuItemPath = "Tools/Wagenheimer/Native Social/Dashboard...",
                    TypeName = "Wagenheimer.NativeSocial.Editor.UI.NativeSocialHubWindow",
                    MethodName = "Open",
                    Icon = "📱",
                    IsPrimary = true
                },
                SecondaryTools = new List<ToolShortcut>
                {
                    new ToolShortcut { Title = "Integration Guide", MenuItemPath = "Tools/Wagenheimer/Native Social/Integration Guide", Icon = "📖" }
                }
            },
            ["com.wagenheimer.tk2dporter"] = new PackageDashboardInfo
            {
                PackageId = "com.wagenheimer.tk2dporter",
                DisplayName = "Tk2d Porter",
                PrimaryTitle = "Auto Converter",
                Icon = "🔄",
                PrimaryDashboard = new ToolShortcut
                {
                    Title = "Auto Converter",
                    MenuItemPath = "Tools/Wagenheimer/Tk2d Porter/Auto-detect Conversion (Selection)",
                    Icon = "🔄",
                    IsPrimary = true
                }
            },
            ["com.wagenheimer.timelinetypewriter"] = new PackageDashboardInfo
            {
                PackageId = "com.wagenheimer.timelinetypewriter",
                DisplayName = "Timeline Typewriter",
                PrimaryTitle = "Documentation",
                Icon = "✍️",
                PrimaryDashboard = new ToolShortcut
                {
                    Title = "GitHub Docs",
                    MenuItemPath = null,
                    Icon = "✍️",
                    IsPrimary = true
                }
            }
        };

        public static bool HasDashboard(string packageId)
        {
            if (string.IsNullOrEmpty(packageId)) return false;
            return Registry.ContainsKey(packageId);
        }

        public static PackageDashboardInfo GetDashboardInfo(string packageId)
        {
            if (string.IsNullOrEmpty(packageId)) return null;
            Registry.TryGetValue(packageId, out var info);
            return info;
        }

        public static List<PackageDashboardInfo> GetInstalledDashboards(IEnumerable<PackageItem> installedPackages)
        {
            var list = new List<PackageDashboardInfo>();
            if (installedPackages == null) return list;

            foreach (var pkg in installedPackages)
            {
                if (!pkg.IsInstalled) continue;
                if (Registry.TryGetValue(pkg.PackageId, out var info))
                {
                    list.Add(info);
                }
            }
            return list;
        }

        public static bool Launch(ToolShortcut shortcut, string fallbackRepoUrl = null)
        {
            if (shortcut == null) return false;

            // 1. Try Reflection if TypeName is provided
            if (!string.IsNullOrEmpty(shortcut.TypeName))
            {
                try
                {
                    var type = FindTypeInAssemblies(shortcut.TypeName);
                    if (type != null)
                    {
                        var methodName = string.IsNullOrEmpty(shortcut.MethodName) ? "ShowWindow" : shortcut.MethodName;
                        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        if (method != null)
                        {
                            method.Invoke(null, null);
                            return true;
                        }

                        // Fallback: If it's an EditorWindow, call EditorWindow.GetWindow(type).Show()
                        if (typeof(EditorWindow).IsAssignableFrom(type))
                        {
                            var win = EditorWindow.GetWindow(type);
                            if (win != null)
                            {
                                win.Show();
                                win.Focus();
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PackageHub] Reflection launch for {shortcut.TypeName} failed: {ex.Message}");
                }
            }

            // 2. Try Menu Item path
            if (!string.IsNullOrEmpty(shortcut.MenuItemPath))
            {
                if (EditorApplication.ExecuteMenuItem(shortcut.MenuItemPath))
                {
                    return true;
                }
            }

            // 3. Fallback to web link if provided
            if (!string.IsNullOrEmpty(fallbackRepoUrl))
            {
                Application.OpenURL(fallbackRepoUrl);
                return true;
            }

            return false;
        }

        private static Type FindTypeInAssemblies(string typeFullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(typeFullName);
                if (t != null) return t;
            }
            return null;
        }
    }
}
