using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mobge.AutoSave {
    [InitializeOnLoad]
    public static class AutoSaveTracker {
        public const string SavePath = "AutoSave";
        public const string SaveKey = "config";
        private static AutoSaveConfig Config {
            get {
                if (config != null) {
                    return config;
                }
                if (!SettingsManagerEditor.TryGetValue(SavePath, SaveKey, out config)) {
                    config = new AutoSaveConfig();
                }
                return config;
            }
        }
        internal static AutoSaveConfig config;
        private static long _lastAutoSaveTime;
        static AutoSaveTracker() {
            EditorApplication.hierarchyChanged += SaveProjectIfNeeded;
            EditorApplication.update += SaveProjectIfNeeded;
        }
        private static void SaveProjectIfNeeded() {
            if (!Config.isAutoSaveOn) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isPlaying) return;
            if (EditorApplication.isCompiling) return;
            var currentTime = DateTime.Now.Ticks;
            var timePassed = currentTime - _lastAutoSaveTime;
            if (Config.isDebugOn) Debug.Log($"time remaining before autosave: {(Config.saveIntervalSeconds * 10000000) - timePassed} ticks");
            if (timePassed < (long)Config.saveIntervalSeconds * 10000000L) return;
            if (string.IsNullOrWhiteSpace(SceneManager.GetActiveScene().path)) return;
            _lastAutoSaveTime = currentTime;
            ProperSave.DoProperSave();
            if (Config.isDebugOn) Debug.Log("Automatically Properly Saved");
        }
    }
}