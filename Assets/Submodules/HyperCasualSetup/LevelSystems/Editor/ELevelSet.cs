using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace Mobge.HyperCasualSetup {
    [CustomEditor(typeof(LevelSet))]
    public class ELevelSet : Editor {
        private LevelSet _go;
        private void OnEnable() {
            _go = target as LevelSet;
        }
        public override void OnInspectorGUI() {
            if (GUILayout.Button("OPEN WINDOW")) {
                ELevelSetWindow.OpenWindow(_go);
            }
        }
        public void DrawBaseInspector() {
            base.OnInspectorGUI();
        }
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line) {

            var obj = EditorUtility.InstanceIDToObject(instanceId);
            LevelSet levelSet;
            bool isSubClass = obj.GetType().IsSubclassOf(typeof(LevelSet));

            if ((levelSet = obj as LevelSet) == null || isSubClass) return false;

            ELevelSetWindow.OpenWindow(levelSet);
            return true;
        }
    }
}