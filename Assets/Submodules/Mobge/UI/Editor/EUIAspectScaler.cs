using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.UI {
    [CustomEditor(typeof(UIAspectScaler))]
    public class EUIAspectScaler : Editor {
        private UIAspectScaler _go;

        private void OnEnable() {
            _go = target as UIAspectScaler;
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (_go == null) {
                return;
            }

            _go.aspectMode = (UIAspectScaler.AspectMode)EditorGUILayout.EnumPopup("Aspect Mode", _go.aspectMode);

            if (GUI.changed) {
                EditorExtensions.SetDirty(_go);
            }
        }
    }
}