#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Mobge {
    public static class ProperSave {
        [MenuItem("File/Proper Save %&s", false, 170)]
        public static void DoProperSave() {
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorApplication.delayCall += () => {
                var allOpenWindows = UnityEngine.Resources.FindObjectsOfTypeAll<EditorWindow>();
                foreach (var window in allOpenWindows) {
                    if (window.titleContent.text == "Project") {
                        window.ShowNotification(new GUIContent("Saved Properly"));
                        window.Repaint();
                    }
                }
            };
        }
        [InitializeOnLoadMethod]
        public static void FixRebinds() {
            ShortcutManager.instance.activeProfileId = "Default";
        }
    }
}
#endif
