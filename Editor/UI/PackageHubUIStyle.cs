using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wagenheimer.PackageHub.Editor
{
    /// <summary>
    /// UI Toolkit helper utilities for creating consistent components across the Package Hub.
    /// Provides fail-safe inline styling so components never collapse into unstyled vertical layouts.
    /// </summary>
    internal static class PackageHubUIStyle
    {
        public const string PackageStylePath = "Packages/com.wagenheimer.packagehub/Editor/UI/PackageHubCommon.uss";

        public static readonly Color ColBgDark = new Color(0.08f, 0.09f, 0.12f);       // #14171F
        public static readonly Color ColCardBg = new Color(0.11f, 0.13f, 0.17f);       // #1C202B
        public static readonly Color ColCardBorder = new Color(0.18f, 0.21f, 0.28f);   // #2E3647
        public static readonly Color ColAccent = new Color(0.22f, 0.74f, 0.97f);       // #38BDF8
        public static readonly Color ColTextWhite = new Color(0.95f, 0.96f, 0.98f);
        public static readonly Color ColTextMuted = new Color(0.58f, 0.64f, 0.72f);
        public static readonly Color ColGreen = new Color(0.10f, 0.73f, 0.51f);
        public static readonly Color ColAmber = new Color(0.96f, 0.62f, 0.04f);

        public static void SetRadius(this IStyle s, float r)
        {
            s.borderTopLeftRadius = r;
            s.borderTopRightRadius = r;
            s.borderBottomLeftRadius = r;
            s.borderBottomRightRadius = r;
        }

        public static void Apply(VisualElement element)
        {
            if (element == null) return;

            StyleSheet sheet = null;

            // 1. Try direct package path
            sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(PackageStylePath);

            // 2. Try searching by name
            if (sheet == null)
            {
                var guids = AssetDatabase.FindAssets("PackageHubCommon t:StyleSheet");
                if (guids != null && guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                }
            }

            // 3. Fallback search inside PackageCache
            if (sheet == null)
            {
                var guids = AssetDatabase.FindAssets("t:StyleSheet");
                foreach (var g in guids)
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    if (p.EndsWith("PackageHubCommon.uss", StringComparison.OrdinalIgnoreCase))
                    {
                        sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(p);
                        if (sheet != null) break;
                    }
                }
            }

            if (sheet != null && !element.styleSheets.Contains(sheet))
            {
                element.styleSheets.Add(sheet);
            }
        }

        public static VisualElement CreateHeader(
            string title,
            string subtitle,
            string version,
            Action onCheckUpdates,
            Action onUpdateAll,
            int updateCount,
            bool isChecking)
        {
            var header = new VisualElement();
            header.AddToClassList("hub-header");
            header.style.backgroundColor = new StyleColor(ColCardBg);
            header.style.borderTopWidth = 1;
            header.style.borderRightWidth = 1;
            header.style.borderBottomWidth = 1;
            header.style.borderLeftWidth = 4;
            header.style.borderTopColor = new StyleColor(ColCardBorder);
            header.style.borderRightColor = new StyleColor(ColCardBorder);
            header.style.borderBottomColor = new StyleColor(ColCardBorder);
            header.style.borderLeftColor = new StyleColor(ColAccent);
            header.style.SetRadius(8);
            header.style.paddingTop = 14;
            header.style.paddingBottom = 14;
            header.style.paddingLeft = 18;
            header.style.paddingRight = 18;
            header.style.marginBottom = 12;

            var topRow = new VisualElement();
            topRow.AddToClassList("hub-header-top");
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.justifyContent = Justify.SpaceBetween;
            topRow.style.alignItems = Align.Center;

            // Left: Title + Author badge
            var titleGroup = new VisualElement();
            titleGroup.AddToClassList("hub-title-group");
            titleGroup.style.flexDirection = FlexDirection.Column;

            var titleRow = new VisualElement();
            titleRow.AddToClassList("hub-title-row");
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("hub-title");
            titleLabel.style.fontSize = 17;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = new StyleColor(ColTextWhite);
            titleRow.Add(titleLabel);

            var authorBadge = new Label("by Cezar Wagenheimer");
            authorBadge.AddToClassList("hub-author-badge");
            authorBadge.style.backgroundColor = new StyleColor(new Color(0.22f, 0.74f, 0.97f, 0.15f));
            authorBadge.style.borderTopWidth = 1;
            authorBadge.style.borderBottomWidth = 1;
            authorBadge.style.borderLeftWidth = 1;
            authorBadge.style.borderRightWidth = 1;
            authorBadge.style.borderTopColor = new StyleColor(ColAccent);
            authorBadge.style.borderBottomColor = new StyleColor(ColAccent);
            authorBadge.style.borderLeftColor = new StyleColor(ColAccent);
            authorBadge.style.borderRightColor = new StyleColor(ColAccent);
            authorBadge.style.SetRadius(10);
            authorBadge.style.paddingTop = 2;
            authorBadge.style.paddingBottom = 2;
            authorBadge.style.paddingLeft = 8;
            authorBadge.style.paddingRight = 8;
            authorBadge.style.marginLeft = 10;
            authorBadge.style.fontSize = 10;
            authorBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
            authorBadge.style.color = new StyleColor(ColAccent);
            titleRow.Add(authorBadge);

            if (!string.IsNullOrEmpty(version))
            {
                var verBadge = CreateBadge($"v{version}", "hub-badge-info");
                verBadge.style.marginLeft = 6;
                titleRow.Add(verBadge);
            }

            titleGroup.Add(titleRow);

            var subtitleLabel = new Label(subtitle);
            subtitleLabel.AddToClassList("hub-subtitle");
            subtitleLabel.style.fontSize = 11;
            subtitleLabel.style.color = new StyleColor(ColTextMuted);
            subtitleLabel.style.marginTop = 3;
            titleGroup.Add(subtitleLabel);

            // Web links below title
            var linksRow = new VisualElement();
            linksRow.AddToClassList("hub-header-links");
            linksRow.style.flexDirection = FlexDirection.Row;
            linksRow.style.alignItems = Align.Center;
            linksRow.style.marginTop = 6;

            var webLink = new Label("🌐 wagenheimer.com ↗");
            webLink.AddToClassList("hub-header-link");
            webLink.style.fontSize = 11;
            webLink.style.color = new StyleColor(ColAccent);
            webLink.style.unityFontStyleAndWeight = FontStyle.Bold;
            webLink.style.marginRight = 14;
            webLink.RegisterCallback<ClickEvent>(_ => Application.OpenURL("https://wagenheimer.com"));
            linksRow.Add(webLink);

            var ghLink = new Label("GitHub Profile ↗");
            ghLink.AddToClassList("hub-header-link");
            ghLink.style.fontSize = 11;
            ghLink.style.color = new StyleColor(ColAccent);
            ghLink.style.unityFontStyleAndWeight = FontStyle.Bold;
            ghLink.RegisterCallback<ClickEvent>(_ => Application.OpenURL("https://github.com/wagenheimer"));
            linksRow.Add(ghLink);

            titleGroup.Add(linksRow);
            topRow.Add(titleGroup);

            // Right: Global actions
            var actionsGroup = new VisualElement();
            actionsGroup.AddToClassList("hub-header-actions");
            actionsGroup.style.flexDirection = FlexDirection.Row;
            actionsGroup.style.alignItems = Align.Center;

            var checkBtn = CreateButton(
                isChecking ? "Checking..." : "🔄 Check Updates",
                "hub-btn-secondary",
                onCheckUpdates);
            checkBtn.SetEnabled(!isChecking && !PackageInstaller.IsBusy);
            checkBtn.style.height = 28;
            actionsGroup.Add(checkBtn);

            if (updateCount > 0)
            {
                var updateAllBtn = CreateButton(
                    $"⚡ Update All ({updateCount})",
                    "hub-btn-warning",
                    onUpdateAll);
                updateAllBtn.SetEnabled(!PackageInstaller.IsBusy);
                updateAllBtn.style.height = 28;
                updateAllBtn.style.marginLeft = 8;
                actionsGroup.Add(updateAllBtn);
            }

            topRow.Add(actionsGroup);
            header.Add(topRow);

            return header;
        }

        public static VisualElement CreateMetricCard(string label, string initialValue, out Label valueLabel)
        {
            var card = new VisualElement();
            card.AddToClassList("hub-metric-card");
            card.style.flexGrow = 1;
            card.style.flexBasis = 0;
            card.style.flexShrink = 0;
            card.style.flexDirection = FlexDirection.Column;
            card.style.justifyContent = Justify.Center;
            card.style.alignItems = Align.Center;
            card.style.height = 58;
            card.style.minHeight = 58;
            card.style.backgroundColor = new StyleColor(ColCardBg);
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopColor = new StyleColor(ColCardBorder);
            card.style.borderBottomColor = new StyleColor(ColCardBorder);
            card.style.borderLeftColor = new StyleColor(ColCardBorder);
            card.style.borderRightColor = new StyleColor(ColCardBorder);
            card.style.SetRadius(7);
            card.style.paddingTop = 6;
            card.style.paddingBottom = 6;
            card.style.paddingLeft = 10;
            card.style.paddingRight = 10;
            card.style.marginRight = 8;

            valueLabel = new Label(initialValue);
            valueLabel.AddToClassList("hub-metric-value");
            valueLabel.style.fontSize = 18;
            valueLabel.style.height = 22;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueLabel.style.color = new StyleColor(ColTextWhite);
            valueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            var labelElement = new Label(label);
            labelElement.AddToClassList("hub-metric-label");
            labelElement.style.fontSize = 10.5f;
            labelElement.style.height = 14;
            labelElement.style.color = new StyleColor(ColTextMuted);
            labelElement.style.marginTop = 2;
            labelElement.style.unityTextAlign = TextAnchor.MiddleCenter;

            card.Add(valueLabel);
            card.Add(labelElement);

            return card;
        }

        public static Label CreateBadge(string text, string typeClass = "hub-badge-info")
        {
            var badge = new Label(text);
            badge.AddToClassList("hub-badge");
            badge.style.paddingTop = 3;
            badge.style.paddingBottom = 3;
            badge.style.paddingLeft = 8;
            badge.style.paddingRight = 8;
            badge.style.SetRadius(12);
            badge.style.fontSize = 10;
            badge.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.style.borderTopWidth = 1;
            badge.style.borderBottomWidth = 1;
            badge.style.borderLeftWidth = 1;
            badge.style.borderRightWidth = 1;

            if (typeClass.Contains("pass"))
            {
                badge.style.backgroundColor = new StyleColor(new Color(0.10f, 0.73f, 0.51f, 0.15f));
                badge.style.borderTopColor = new StyleColor(ColGreen);
                badge.style.borderBottomColor = new StyleColor(ColGreen);
                badge.style.borderLeftColor = new StyleColor(ColGreen);
                badge.style.borderRightColor = new StyleColor(ColGreen);
                badge.style.color = new StyleColor(new Color(0.43f, 0.91f, 0.72f));
            }
            else if (typeClass.Contains("update"))
            {
                badge.style.backgroundColor = new StyleColor(new Color(0.96f, 0.62f, 0.04f, 0.18f));
                badge.style.borderTopColor = new StyleColor(ColAmber);
                badge.style.borderBottomColor = new StyleColor(ColAmber);
                badge.style.borderLeftColor = new StyleColor(ColAmber);
                badge.style.borderRightColor = new StyleColor(ColAmber);
                badge.style.color = new StyleColor(new Color(0.99f, 0.83f, 0.30f));
            }
            else if (typeClass.Contains("time"))
            {
                badge.style.backgroundColor = new StyleColor(new Color(0.58f, 0.64f, 0.72f, 0.12f));
                badge.style.borderTopColor = new StyleColor(new Color(0.58f, 0.64f, 0.72f, 0.30f));
                badge.style.borderBottomColor = new StyleColor(new Color(0.58f, 0.64f, 0.72f, 0.30f));
                badge.style.borderLeftColor = new StyleColor(new Color(0.58f, 0.64f, 0.72f, 0.30f));
                badge.style.borderRightColor = new StyleColor(new Color(0.58f, 0.64f, 0.72f, 0.30f));
                badge.style.color = new StyleColor(new Color(0.70f, 0.78f, 0.88f));
            }
            else
            {
                badge.style.backgroundColor = new StyleColor(new Color(0.22f, 0.74f, 0.97f, 0.15f));
                badge.style.borderTopColor = new StyleColor(ColAccent);
                badge.style.borderBottomColor = new StyleColor(ColAccent);
                badge.style.borderLeftColor = new StyleColor(ColAccent);
                badge.style.borderRightColor = new StyleColor(ColAccent);
                badge.style.color = new StyleColor(new Color(0.49f, 0.83f, 0.99f));
            }

            if (!string.IsNullOrEmpty(typeClass))
            {
                badge.AddToClassList(typeClass);
            }
            return badge;
        }

        public static Button CreateButton(string text, string styleClass, Action onClick)
        {
            var btn = new Button(onClick) { text = text };
            btn.AddToClassList("hub-btn");
            btn.style.SetRadius(5);
            btn.style.paddingTop = 5;
            btn.style.paddingBottom = 5;
            btn.style.paddingLeft = 12;
            btn.style.paddingRight = 12;
            btn.style.fontSize = 11;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.borderTopWidth = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = new StyleColor(new Color(0.22f, 0.26f, 0.34f));
            btn.style.borderBottomColor = new StyleColor(new Color(0.22f, 0.26f, 0.34f));
            btn.style.borderLeftColor = new StyleColor(new Color(0.22f, 0.26f, 0.34f));
            btn.style.borderRightColor = new StyleColor(new Color(0.22f, 0.26f, 0.34f));
            btn.style.backgroundColor = new StyleColor(new Color(0.14f, 0.16f, 0.22f));
            btn.style.color = new StyleColor(ColTextWhite);

            if (styleClass != null && styleClass.Contains("primary"))
            {
                btn.style.backgroundColor = new StyleColor(new Color(0.15f, 0.39f, 0.92f));
                btn.style.borderTopColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f));
                btn.style.borderBottomColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f));
                btn.style.borderLeftColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f));
                btn.style.borderRightColor = new StyleColor(new Color(0.23f, 0.51f, 0.96f));
            }
            else if (styleClass != null && styleClass.Contains("dashboard"))
            {
                btn.style.backgroundColor = new StyleColor(new Color(0.05f, 0.45f, 0.56f));
                btn.style.borderTopColor = new StyleColor(new Color(0.02f, 0.71f, 0.83f));
                btn.style.borderBottomColor = new StyleColor(new Color(0.02f, 0.71f, 0.83f));
                btn.style.borderLeftColor = new StyleColor(new Color(0.02f, 0.71f, 0.83f));
                btn.style.borderRightColor = new StyleColor(new Color(0.02f, 0.71f, 0.83f));
            }
            else if (styleClass != null && styleClass.Contains("warning"))
            {
                btn.style.backgroundColor = new StyleColor(new Color(0.85f, 0.47f, 0.02f));
                btn.style.borderTopColor = new StyleColor(ColAmber);
                btn.style.borderBottomColor = new StyleColor(ColAmber);
                btn.style.borderLeftColor = new StyleColor(ColAmber);
                btn.style.borderRightColor = new StyleColor(ColAmber);
            }
            else if (styleClass != null && styleClass.Contains("success"))
            {
                btn.style.backgroundColor = new StyleColor(new Color(0.02f, 0.59f, 0.41f));
                btn.style.borderTopColor = new StyleColor(ColGreen);
                btn.style.borderBottomColor = new StyleColor(ColGreen);
                btn.style.borderLeftColor = new StyleColor(ColGreen);
                btn.style.borderRightColor = new StyleColor(ColGreen);
            }

            if (!string.IsNullOrEmpty(styleClass))
            {
                btn.AddToClassList(styleClass);
            }
            return btn;
        }
    }
}
