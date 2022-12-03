using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class EVersionSettingMenu {
    [MenuItem("Mobge/Version Settings")]
    public static void SelectSettings() {
        var setting = Resources.Load<VersionSetting>("MobgeVersionSetting");
        if (setting == null) {
            setting = ScriptableObject.CreateInstance<VersionSetting>();
            const string path = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(path)) {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/MobgeVersionSetting.asset");
            AssetDatabase.CreateAsset(setting, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = setting;
    }
}


