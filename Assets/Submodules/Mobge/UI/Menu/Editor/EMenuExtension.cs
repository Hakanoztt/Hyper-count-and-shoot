using UnityEditor;
using UnityEngine;



namespace Mobge.UI {
    [CustomEditor(typeof(MenuExtension), true)]
    public class EMenuExtension : Editor {
        public override void OnInspectorGUI() {
            var registered = IsRegistered();
            using (Scopes.GUIEnabled(!registered)) {
                if (GUILayout.Button("Register To Menu")) {
                    Register();
                }
            }
            using (Scopes.GUIEnabled(registered)) {
                if (GUILayout.Button("UnRegister To Menu")) {
                    UnRegister();
                }
            }
            base.OnInspectorGUI();
        }
        public bool IsRegistered() {
            return false;
        }
        public void Register() {
        }
        public void UnRegister() {
        }
    }
}
