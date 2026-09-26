using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wagenheimer.PackageHub.Editor
{
    /// <summary>
    /// UI Toolkit helper utilities for creating consistent components across the Package Hub.
    /// </summary>
    internal static class PackageHubUIStyle
    {
        public const string PackageStylePath = "Packages/com.wagenheimer.packagehub/Editor/UI/PackageHubCommon.uss";

        public static void Apply(VisualElement element)
        {
            if (element == null) return;

            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(PackageStylePath);
            if (sheet == null)
            {
                var guids = AssetDatabase.FindAssets("PackageHubCommon t:StyleSheet");
                if (guids != null && guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
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

            var topRow = new VisualElement();
            topRow.AddToClassList("hub-header-top");

            // Left: Title + Author badge
            var titleGroup = new VisualElement();
            titleGroup.AddToClassList("hub-title-group");

            var titleRow = new VisualElement();
            titleRow.AddToClassList("hub-title-row");

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("hub-title");
            titleRow.Add(titleLabel);

            var authorBadge = new Label("by Cezar Wagenheimer");
            authorBadge.AddToClassList("hub-author-badge");
            titleRow.Add(authorBadge);

            if (!string.IsNullOrEmpty(version))
            {
                var verBadge = new Label($"v{version}");
                verBadge.AddToClassList("hub-badge");
                verBadge.AddToClassList("hub-badge-info");
                titleRow.Add(verBadge);
            }

            titleGroup.Add(titleRow);

            var subtitleLabel = new Label(subtitle);
            subtitleLabel.AddToClassList("hub-subtitle");
            titleGroup.Add(subtitleLabel);

            // Web links below title
            var linksRow = new VisualElement();
            linksRow.AddToClassList("hub-header-links");

            var webLink = new Label("🌐 wagenheimer.com ↗");
            webLink.AddToClassList("hub-header-link");
            webLink.RegisterCallback<ClickEvent>(_ => Application.OpenURL("https://wagenheimer.com"));
            linksRow.Add(webLink);

            var ghLink = new Label("GitHub Profile ↗");
            ghLink.AddToClassList("hub-header-link");
            ghLink.RegisterCallback<ClickEvent>(_ => Application.OpenURL("https://github.com/wagenheimer"));
            linksRow.Add(ghLink);

            titleGroup.Add(linksRow);
            topRow.Add(titleGroup);

            // Right: Global actions
            var actionsGroup = new VisualElement();
            actionsGroup.AddToClassList("hub-header-actions");

            var checkBtn = CreateButton(
                isChecking ? "Checking..." : "🔄 Check Updates",
                "hub-btn-secondary",
                onCheckUpdates);
            checkBtn.SetEnabled(!isChecking && !PackageInstaller.IsBusy);
            actionsGroup.Add(checkBtn);

            if (updateCount > 0)
            {
                var updateAllBtn = CreateButton(
                    $"⚡ Update All ({updateCount})",
                    "hub-btn-warning",
                    onUpdateAll);
                updateAllBtn.SetEnabled(!PackageInstaller.IsBusy);
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

            valueLabel = new Label(initialValue);
            valueLabel.AddToClassList("hub-metric-value");

            var labelElement = new Label(label);
            labelElement.AddToClassList("hub-metric-label");

            card.Add(valueLabel);
            card.Add(labelElement);

            return card;
        }

        public static Label CreateBadge(string text, string typeClass = "hub-badge-info")
        {
            var badge = new Label(text);
            badge.AddToClassList("hub-badge");
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
            if (!string.IsNullOrEmpty(styleClass))
            {
                btn.AddToClassList(styleClass);
            }
            return btn;
        }
    }
}
