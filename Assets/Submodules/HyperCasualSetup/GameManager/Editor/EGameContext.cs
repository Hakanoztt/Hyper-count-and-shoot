using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mobge.HyperCasualSetup {
    [CustomEditor(typeof(GameContext), true)]
    public class EGameContext : Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (GUILayout.Button("Delete Save Data")) {
                var gameContext = (GameContext) target;
                var path = Path.Combine(Application.persistentDataPath, gameContext.saveFile);
                File.Delete(path);
                Debug.Log("Success! Deleted Save Data!");
            }
        }
    }
}