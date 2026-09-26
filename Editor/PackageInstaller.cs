using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Wagenheimer.PackageHub.Editor
{
    public static class PackageInstaller
    {
        private static AddRequest _currentAddRequest;
        private static PackageItem _currentUpdatingItem;
        private static Action<bool, string> _currentCallback;

        private static readonly Queue<PackageItem> _batchQueue = new Queue<PackageItem>();
        private static Action _onBatchCompleted;
        private static bool _isBatchRunning;

        public static event Action OnInstallStateChanged;

        public static bool IsBusy => (_currentAddRequest != null && !_currentAddRequest.IsCompleted) || _batchQueue.Count > 0 || _isBatchRunning;
        public static int BatchTotalCount { get; private set; }
        public static int BatchCompletedCount { get; private set; }
        public static PackageItem CurrentUpdatingItem => _currentUpdatingItem;
        public static string CurrentOperationTitle { get; private set; }
        public static float BatchProgress => BatchTotalCount > 0 ? (float)BatchCompletedCount / BatchTotalCount : (IsBusy ? 0.5f : 0f);

        public static void InstallOrUpdate(PackageItem item, string targetVersion = null, Action<bool, string> callback = null)
        {
            if (IsBusy && _currentAddRequest != null && !_currentAddRequest.IsCompleted)
            {
                callback?.Invoke(false, "Another package installation is already in progress.");
                return;
            }

            var gitUrl = item.GitUrl;
            var ver = !string.IsNullOrEmpty(targetVersion) ? targetVersion : item.LatestRemoteVersion;

            string packageSpec;
            if (!string.IsNullOrEmpty(ver))
            {
                var cleanVer = PackageDiscovery.CleanVersion(ver);
                packageSpec = $"{gitUrl}#v{cleanVer}";
            }
            else
            {
                packageSpec = gitUrl;
            }

            item.IsUpdating = true;
            item.UpdateError = null;
            _currentUpdatingItem = item;
            _currentCallback = callback;
            CurrentOperationTitle = $"Installing {item.DisplayName} (v{ver ?? "latest"})...";

            OnInstallStateChanged?.Invoke();

            Debug.Log($"[Wagenheimer.PackageHub] Installing {item.DisplayName} via: {packageSpec}");
            _currentAddRequest = Client.Add(packageSpec);
            EditorApplication.update += MonitorRequest;
        }

        public static void UpdateAll(List<PackageItem> itemsToUpdate, Action onAllCompleted = null)
        {
            if (itemsToUpdate == null || itemsToUpdate.Count == 0)
            {
                onAllCompleted?.Invoke();
                return;
            }

            _batchQueue.Clear();
            foreach (var item in itemsToUpdate)
            {
                _batchQueue.Enqueue(item);
            }

            BatchTotalCount = itemsToUpdate.Count;
            BatchCompletedCount = 0;
            _isBatchRunning = true;
            _onBatchCompleted = onAllCompleted;

            OnInstallStateChanged?.Invoke();
            ProcessNextBatch();
        }

        private static void ProcessNextBatch()
        {
            if (_batchQueue.Count == 0)
            {
                _isBatchRunning = false;
                CurrentOperationTitle = null;
                var cb = _onBatchCompleted;
                _onBatchCompleted = null;
                OnInstallStateChanged?.Invoke();
                cb?.Invoke();
                return;
            }

            var nextItem = _batchQueue.Dequeue();
            CurrentOperationTitle = $"Updating {nextItem.DisplayName} ({BatchCompletedCount + 1} of {BatchTotalCount})...";
            OnInstallStateChanged?.Invoke();

            InstallOrUpdate(nextItem, nextItem.LatestRemoteVersion, (success, err) =>
            {
                BatchCompletedCount++;
                if (!success)
                {
                    Debug.LogWarning($"[Wagenheimer.PackageHub] Failed to update {nextItem.DisplayName}: {err}");
                }
                OnInstallStateChanged?.Invoke();
                ProcessNextBatch();
            });
        }

        private static void MonitorRequest()
        {
            if (_currentAddRequest == null || !_currentAddRequest.IsCompleted)
                return;

            EditorApplication.update -= MonitorRequest;

            var success = _currentAddRequest.Status == StatusCode.Success;
            string error = null;

            if (_currentUpdatingItem != null)
            {
                _currentUpdatingItem.IsUpdating = false;
                if (success)
                {
                    _currentUpdatingItem.IsInstalled = true;
                    _currentUpdatingItem.InstalledVersion = _currentUpdatingItem.LatestRemoteVersion;
                    _currentUpdatingItem.HasUpdate = false;
                    Debug.Log($"[Wagenheimer.PackageHub] Successfully installed/updated {_currentUpdatingItem.DisplayName}!");
                }
                else
                {
                    error = _currentAddRequest.Error?.message ?? "Installation failed.";
                    _currentUpdatingItem.UpdateError = error;
                    Debug.LogError($"[Wagenheimer.PackageHub] Error installing {_currentUpdatingItem.DisplayName}: {error}");
                }
            }

            var cb = _currentCallback;
            _currentAddRequest = null;
            _currentUpdatingItem = null;
            _currentCallback = null;
            if (!_isBatchRunning)
            {
                CurrentOperationTitle = null;
            }

            OnInstallStateChanged?.Invoke();
            cb?.Invoke(success, error);
        }
    }
}

