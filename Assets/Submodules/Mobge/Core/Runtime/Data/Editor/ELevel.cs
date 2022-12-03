using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Mobge.Core {
    [CustomEditor(typeof(Level), true)]
    public class ELevel : Editor {
        private Level _level;
        protected void OnEnable() {
            _level = target as Level;
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if(_level) {
                if(GUILayout.Button("edit on scene")) {
                     OpenLevel(_level);
                }
            }
        }
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line) {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            Level level;
            if ((level = obj as Level) == null) return false;
            OpenLevel(level);
            return true;
        }
        public static void OpenLevel(Level level) {
            var levelPlayer = FindObjectOfType(level.PlayerType) as LevelPlayer;
            if (levelPlayer == null) 
                levelPlayer = (LevelPlayer) new GameObject("level editor").AddComponent(level.PlayerType);
            levelPlayer.gameObject.hideFlags = HideFlags.DontSaveInBuild;
            levelPlayer.ResetData();
            levelPlayer.level = level;
            Selection.activeObject = levelPlayer;
        }
    }
}