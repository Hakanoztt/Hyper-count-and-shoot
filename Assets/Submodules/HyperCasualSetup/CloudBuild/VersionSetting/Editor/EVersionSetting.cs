using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VersionSetting))]
public class EVersionSetting : Editor
{
    private string _currentVersion;

    private void OnEnable() {
        VersionSetting.GetCurrentSetting((b, setting) => {
            _currentVersion = setting.FullVersionString;
            UpdateSettings();
        });
    }

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        if (GUILayout.Button("Get New Version")) {
            VersionSetting.GetNewSetting((b, setting) => {
                _currentVersion = b ? setting.FullVersionString : Application.version;
                UpdateSettings();
            });
        }
        if (GUILayout.Button("Get Current Version")) {
            VersionSetting.GetCurrentSetting((b, setting) => {
                _currentVersion = setting.FullVersionString;
                UpdateSettings();
            });
        }
        GUILayout.Label("Current Version: " + _currentVersion);
    }

    private void UpdateSettings() {
        VersionSetting.GetCurrentSetting((b, setting) => {
            PlayerSettings.bundleVersion = _currentVersion;
            AssetDatabase.SaveAssets();
        });
    }
}
