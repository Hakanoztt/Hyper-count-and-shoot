using UnityEditor;
using UnityEngine;

namespace Mobge {

    [CustomEditor(typeof(AudioEffect))]
    public class EAudioEffect : Editor {

        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            AudioEffect o = target as AudioEffect;
            if (o != null && o.audioSource != null && o.audioSource.playOnAwake) {
                EditorGUILayout.HelpBox("Warning! Audio Source is set to play on awake! Turning off is advised!", MessageType.Warning);
                if (GUILayout.Button("Click me to fix!")) {
                    o.audioSource.playOnAwake = false;
                }
            }
        }
    }
}