using UnityEditor;
using UnityEngine;

namespace Wagenheimer.PackageHub.Editor
{
    /// <summary>
    /// Keeps package dashboards reachable. Unity restores a window at its last saved position, which can be on a
    /// monitor that is no longer connected: the menu item "does nothing" because the window opens off-screen.
    /// Every Wagenheimer dashboard should call <see cref="EnsureOnScreen"/> before <c>Show()</c>.
    /// </summary>
    public static class EditorWindowPlacement
    {
        private const float VisibleMargin = 40f;
        private const float MinUsableSize = 50f;
        private const float ScreenPadding = 40f;

        /// <summary>Moves/resizes <paramref name="window"/> to the middle of the Unity main window when it is not visible there. Docked windows are left alone.</summary>
        public static void EnsureOnScreen(EditorWindow window, Vector2 minSize, Vector2 defaultSize)
        {
            if (window == null || window.docked)
                return;

            if (!TryGetHostRect(out var host))
                return;

            var target = ComputeVisibleRect(window.position, host, minSize, defaultSize);
            if (target.HasValue)
                window.position = target.Value;
        }

        /// <summary>Centers <paramref name="window"/> in the Unity main window at <paramref name="size"/> (clamped to the host).</summary>
        public static void Center(EditorWindow window, Vector2 minSize, Vector2 size)
        {
            if (window == null || !TryGetHostRect(out var host))
                return;

            window.position = CenterIn(host, minSize, size);
        }

        /// <summary>
        /// Pure placement rule. Returns null when <paramref name="window"/> is already usable and visible inside
        /// <paramref name="host"/>; otherwise the rect to move it to (centered, keeping its size unless degenerate).
        /// </summary>
        public static Rect? ComputeVisibleRect(Rect window, Rect host, Vector2 minSize, Vector2 defaultSize)
        {
            if (host.width < 1f || host.height < 1f)
                return null;

            var degenerate = IsInvalid(window.x) || IsInvalid(window.y) ||
                             window.width < MinUsableSize || window.height < MinUsableSize;

            var overlapsHost = window.xMax > host.x + VisibleMargin &&
                               window.yMax > host.y + VisibleMargin &&
                               window.x < host.xMax - VisibleMargin &&
                               window.y < host.yMax - VisibleMargin;

            if (!degenerate && overlapsHost)
                return null;

            var size = degenerate ? defaultSize : new Vector2(window.width, window.height);
            return CenterIn(host, minSize, size);
        }

        private static Rect CenterIn(Rect host, Vector2 minSize, Vector2 size)
        {
            var width = Mathf.Clamp(size.x, minSize.x, Mathf.Max(minSize.x, host.width - ScreenPadding));
            var height = Mathf.Clamp(size.y, minSize.y, Mathf.Max(minSize.y, host.height - ScreenPadding));

            return new Rect(
                Mathf.Round(host.x + (host.width - width) * 0.5f),
                Mathf.Round(host.y + (host.height - height) * 0.5f),
                Mathf.Round(width),
                Mathf.Round(height));
        }

        private static bool TryGetHostRect(out Rect host)
        {
            try
            {
                host = EditorGUIUtility.GetMainWindowPosition();
                return host.width >= 1f && host.height >= 1f;
            }
            catch
            {
                host = default;
                return false;
            }
        }

        private static bool IsInvalid(float value) => float.IsNaN(value) || float.IsInfinity(value);
    }
}
