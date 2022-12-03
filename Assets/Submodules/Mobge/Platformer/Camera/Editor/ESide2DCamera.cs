using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mobge.Platformer {
    [CustomEditor(typeof(Side2DCamera))]
    public class ESide2DCamera : Editor {
        private Side2DCamera _go;

        private void OnEnable() {
            _go = target as Side2DCamera;
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if(_go == null) {
                return;
            }
            if (!_go.camera || _go.camera.transform.parent != _go.transform) {
                EditorGUILayout.HelpBox("Camera property must be set and the camera must be direct child of this object.", MessageType.Error);
            }
            else {
                if (_go.camera.orthographic) {
                    EditorGUILayout.HelpBox("\"Orthographic\" property of the camera must NOT be set.", MessageType.Error);
                }
            }
            using (new EditorGUILayout.HorizontalScope()) {
                float labelWidth = EditorGUIUtility.labelWidth * 0.2f;
                EditorGUILayout.LabelField("match", GUILayout.Width(labelWidth));
                EditorGUILayout.LabelField("width", GUILayout.Width(labelWidth));
                _go.matchWidthHeight = EditorGUILayout.Slider(_go.matchWidthHeight, 0, 1);
                EditorGUILayout.LabelField("height", GUILayout.Width(labelWidth));

            }

            if (Application.isPlaying && _go.Data != null) {
                var cd = _go.Data.centerData;
                EditorGUILayout.Vector3Field("center", cd.Position);
                EditorLayoutDrawer.ObjectField("center object", cd.CenterObject);
            }



            if (GUI.changed) {
                EditorExtensions.SetDirty(_go);
            }
        }
    }
}
