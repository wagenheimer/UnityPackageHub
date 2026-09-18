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

        public static bool IsBusy => _currentAddRequest != null && !_currentAddRequest.IsCompleted;

        public static void InstallOrUpdate(PackageItem item, string targetVersion = null, Action<bool, string> callback = null)
        {
            if (IsBusy)
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

            _onBatchCompleted = onAllCompleted;
            ProcessNextBatch();
        }

        private static void ProcessNextBatch()
        {
            if (_batchQueue.Count == 0)
            {
                _onBatchCompleted?.Invoke();
                _onBatchCompleted = null;
                return;
            }

            var nextItem = _batchQueue.Dequeue();
            InstallOrUpdate(nextItem, nextItem.LatestRemoteVersion, (success, err) =>
            {
                if (!success)
                {
                    Debug.LogWarning($"[Wagenheimer.PackageHub] Failed to update {nextItem.DisplayName}: {err}");
                }
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

            cb?.Invoke(success, error);
        }
    }
}
