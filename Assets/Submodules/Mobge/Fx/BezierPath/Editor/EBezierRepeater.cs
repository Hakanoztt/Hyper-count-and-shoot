using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Mobge.Serialization;

namespace Mobge {
    [CustomEditor(typeof(BezierRepeater))]
    public class EBezierRepeater : Editor {


        private static BezierPath3D s_tempBezier = new BezierPath3D();
        static EBezierPath _bezierEditor = new EBezierPath();

        private BezierRepeater _go;

        private void OnEnable() {
            _go = target as BezierRepeater;
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (!_go) return;
            _go.repeater.EnsurePath();
            Undo.undoRedoPerformed = UndoRedoPerformed;

            if (_bezierEditor.OnInspectorGUI(_go.repeater.path)) {
            }
            if (GUI.changed) {
                UpdateVisuals();
                EditorExtensions.SetDirty(_go.gameObject);
            }
        }

        private void UpdateVisuals() {
            EBezierRepeaterComponent.Editor.UpdateEditorVisuals(_go.transform, _go.repeater);
            var to = TemporaryEditorObjects.Shared;
            for(int i = 0; i < _go.transform.childCount; i++) {
                TemporaryEditorObjects.SetHideFlags(_go.transform.GetChild(i), TemporaryEditorObjects.Shared.hideFlags);

            }
        }

        private void UndoRedoPerformed() {
            if (_go) {
                UpdateVisuals();
            }
        }

        private void OnSceneGUI() {
            if (!_go) return;
            _go.repeater.EnsurePath();

            CopyTo(_go.repeater.path, s_tempBezier);
            Handles.matrix = _go.transform.localToWorldMatrix;
            var edited = _bezierEditor.OnSceneGUI(s_tempBezier);
            Handles.matrix = Matrix4x4.identity;
            if (edited) {

                Undo.RecordObject(_go, "path edit");
            }
            CopyTo(s_tempBezier, _go.repeater.path);
            if (edited) {
                UpdateVisuals();
                EditorExtensions.SetDirty(_go.gameObject);
            }

        }
        void CopyTo(object source, object target) {

            var bod = BinarySerializer.Instance.Serialize(source.GetType(), source);
            BinaryDeserializer.Instance.DeserializeTo(bod, target);
        }
    }
}