using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.UI {
    [CustomEditor(typeof(ContentSizeFitterPlus))]
    public class EContentSizeFitterPlus : Editor {
        private ContentSizeFitterPlus _go;

        private void OnEnable() {
            _go = target as ContentSizeFitterPlus;
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (!_go) {
                return;
            }

            _go.MinSize = EditorGUILayout.Vector2Field("Min Size", _go.MinSize);
            _go.MinSizeRatio = EditorGUILayout.Vector2Field("Min Size Ratio", _go.MinSizeRatio);
            _go.HorizontalFitMode = (ContentSizeFitterPlus.FitMode)EditorGUILayout.EnumPopup("Horizontal Fit Mode", _go.HorizontalFitMode);
            _go.VerticalFitMode = (ContentSizeFitterPlus.FitMode)EditorGUILayout.EnumPopup("Vertical Fit Mode", _go.VerticalFitMode);

            if (GUI.changed) {
                EditorExtensions.SetDirty(_go);
            }
        }
    }
}