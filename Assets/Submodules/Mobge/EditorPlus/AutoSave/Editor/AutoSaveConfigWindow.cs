using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Mobge.AutoSave {
    public class AutoSaveConfigWindow : EditorWindow {
        private AutoSaveConfig _conf;
        [MenuItem("Mobge/Auto Save Config")]
        public static void Init() {
            var window = GetWindow<AutoSaveConfigWindow>();
            var settingIcon = EditorGUIUtility.FindTexture("d__Popup@2x");
            window.titleContent.image = settingIcon;
            window.titleContent.text = "AutoSave Config";
            window.maxSize = new Vector2(280f, 80f);
            window.minSize = window.maxSize;
            window.ShowPopup();
        }
        private void OnGUI() {
            using (new EditorGUILayout.VerticalScope("box")) {
                EditorGUI.BeginChangeCheck();
                _conf.saveIntervalSeconds = EditorGUILayout.IntField("Auto save interval", _conf.saveIntervalSeconds);
                _conf.isAutoSaveOn = EditorGUILayout.Toggle("Is auto save on", _conf.isAutoSaveOn);
                _conf.isDebugOn = EditorGUILayout.Toggle("Is debug on", _conf.isDebugOn);
                if (EditorGUI.EndChangeCheck()) {
                    SaveConfig();
                }
            }
        }
        private void OnEnable() {
            if (!SettingsManagerEditor.TryGetValue(AutoSaveTracker.SavePath, AutoSaveTracker.SaveKey, out _conf)) {
                _conf = new AutoSaveConfig();
            }
        }
        private void SaveConfig() {
            SettingsManagerEditor.SetValue(AutoSaveTracker.SavePath, AutoSaveTracker.SaveKey, _conf);
            AutoSaveTracker.config = _conf;
        }
    }
}