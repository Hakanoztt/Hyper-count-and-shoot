using System;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Mobge.SimpleScreenshot {
    public class SimpleScreenshotWindow : EditorWindow {
        public const string OUTPUT_FOLDER_NAME = "Screenshots";
        private bool _paused = false;
        private GameObject _updater;

        public int SuperSampleSize {
            get => EditorPrefs.GetInt($"{nameof(SimpleScreenshot)}:SuperSampleSize", 1);
            set => EditorPrefs.SetInt($"{nameof(SimpleScreenshot)}:SuperSampleSize", value);
        }
        public KeyCode PauseKey {
            get => (KeyCode) EditorPrefs.GetInt($"{nameof(SimpleScreenshot)}:PauseKey", (int) KeyCode.P);
            set => EditorPrefs.SetInt($"{nameof(SimpleScreenshot)}:PauseKey", (int) value);
        }
        public KeyCode ScreenshotKey {
            get => (KeyCode) EditorPrefs.GetInt($"{nameof(SimpleScreenshot)}:ScreenshotKey", (int) KeyCode.O);
            set => EditorPrefs.SetInt($"{nameof(SimpleScreenshot)}:ScreenshotKey", (int) value);
        }
        [MenuItem("Mobge/Simple Screenshot")]
        private static void Init() {
            GetWindow<SimpleScreenshotWindow>();
        }
        private void OnGUI() {
            PauseKey = (KeyCode)EditorGUILayout.EnumPopup("Pause Key", PauseKey);
            ScreenshotKey = (KeyCode)EditorGUILayout.EnumPopup("Screenshot Key", ScreenshotKey);
            SuperSampleSize = EditorGUILayout.IntField("Super Sample Size", SuperSampleSize);
            if (GUILayout.Button("Take Screenshot")) {
                TakeScreenShot();
            }
            if (Application.isPlaying && _updater == null) {
                _updater = new GameObject(nameof(_updater));
                _updater.AddComponent<LateUpdateCallback>().onLateUpdate = CheckKeys;
            }
        }
        private void CheckKeys(LateUpdateCallback obj) {
            if (Input.GetKeyDown(PauseKey)) {
                TogglePlayPauseState();
            }
            if (Input.GetKeyDown(ScreenshotKey)) {
                TakeScreenShot();
            }  
        }
        private void TogglePlayPauseState() {
            if (Application.isPlaying) {
                _paused = !_paused;
                var timeScale = _paused ? 0f : 1f;
                Debug.Log($"setting time scale to {timeScale}");
                Time.timeScale = timeScale;
            }
        }
        private void TakeScreenShot() {
            var gameViewSize = Handles.GetMainGameViewSize();
            var directoryPath = Path.Combine(OUTPUT_FOLDER_NAME, $"{gameViewSize.x}X{gameViewSize.y}");
            new DirectoryInfo(directoryPath).Create();
            var path = Path.Combine(directoryPath, DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH'.'mm'.'ss") + ".png");
            ScreenCapture.CaptureScreenshot(path, SuperSampleSize);
            Debug.Log($"{nameof(SimpleScreenshot)}: screenshot saved to {path}");
        }
    }
}