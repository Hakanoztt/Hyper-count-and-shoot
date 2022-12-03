using System;
using Mobge;
using UnityEditor;
using UnityEngine;
using static Mobge.InspectorExtensions;
using static SubmoduleManager.SubmoduleManager;

namespace SubmoduleManager {
    public class SubmoduleManagerWindow : EditorWindow {
        private const string PersistentSettingKey = "mobge_submodule_manager_config";
        
        private Vector2 _scroll;
        private bool _isSettingsOn;
        private SubmoduleManagerConfig _config;

        [MenuItem("Mobge/Submodule Manager")]
        private static void Init() {
            var editorAssembly = typeof(Editor).Assembly;
            var inspectorWindowType = editorAssembly.GetType("UnityEditor.InspectorWindow");
            var window = GetWindow<SubmoduleManagerWindow>(nameof(SubmoduleManagerWindow), true, inspectorWindowType);
            window.Show();
        }
        private void OnEnable() {
            LoadConfig();
        }
        private void OnGUI() {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawControls();
            DrawSettings();
            EditorGUILayout.EndScrollView();
        }
        private void DrawControls() {
            using (new EditorGUILayout.VerticalScope("box")) {
                foreach (var submodule in _config.submodules) {
                    using (new EditorGUI.DisabledGroupScope(!submodule.IsValid)) {
                        using (Scopes.GUIBackgroundColor(EditorColors.AlternatingColor)) {
                            using (new EditorGUILayout.VerticalScope("box")) {
                                using (new EditorGUILayout.HorizontalScope()) {
                                    EditorGUILayout.LabelField(submodule.Name);
                                    bool exists = GetIsSubmoduleAdded(submodule);
                                    using (Scopes.GUIBackgroundColor(EditorColors.PastelOliveGreen)) {
                                        using (Scopes.GUIEnabled(!exists)) {
                                            if (GUILayout.Button("Add")) {
                                                AddSubmodule(submodule);
                                            }
                                        }
                                    }
                                    using (Scopes.GUIBackgroundColor(EditorColors.PastelOrange)) {
                                        using (Scopes.GUIEnabled(exists)) {
                                            if (GUILayout.Button("Remove")) {
                                                RemoveSubmodule(submodule);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        private void DrawSettings() {
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.VerticalScope("box")) {
                var buttonText = _isSettingsOn ? "Hide Settings" : "Show Settings";
                if (GUILayout.Button(buttonText)) _isSettingsOn = !_isSettingsOn;

                if (!_isSettingsOn)
                    return;

                var count = _config.submodules.Length;
                EditorGUI.BeginChangeCheck();
                count = EditorGUILayout.DelayedIntField("Count", count);
                if (EditorGUI.EndChangeCheck()) {
                    Array.Resize(ref _config.submodules, count);
                    SaveConfig();
                }

                EditorGUI.BeginChangeCheck();
                DrawRepoConfigs();
                if (EditorGUI.EndChangeCheck()) {
                    SaveConfig();
                }
                DrawImportExportButtons();
            }
        }
        private void DrawRepoConfigs() {
            for (var i = 0; i < _config.submodules.Length; i++)
                using (Scopes.GUIBackgroundColor(EditorColors.AlternatingColor)) {
                    using (new EditorGUILayout.VerticalScope("box")) {
                        _config.submodules[i].url = EditorGUILayout.DelayedTextField("git url", _config.submodules[i].url);
                    }
                }
        }
        private void DrawImportExportButtons() {
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Import")) {
                    _config = ImportExporter.Import<SubmoduleManagerConfig>();
                    SaveConfig();
                }
                if (GUILayout.Button("Export")) {
                    ImportExporter.Export(_config);
                }
            }
        }
        private void LoadConfig() {
            var json = EditorPrefs.GetString(PersistentSettingKey, null);
            _config = !string.IsNullOrEmpty(json) ? JsonUtility.FromJson<SubmoduleManagerConfig>(json) : SubmoduleManagerConfig.DefaultConfig;
            // var configExist = SettingsManagerEditor.TryGetValue(SettingsManagerPath, SettingsManagerKey, out _config);
            // if (!configExist) {
            //     _config = SubmoduleManagerConfig.DefaultConfig;
            // }
        }
        private void SaveConfig() {
            var json = JsonUtility.ToJson(_config, true);
            EditorPrefs.SetString(PersistentSettingKey, json);
            // SettingsManagerEditor.SetValue(SettingsManagerPath, SettingsManagerKey, _config);
        }
    }
}