using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Mobge.HyperCasualSetup.RoadGenerator;
using Mobge.Serialization;

namespace Mobge {
    [CustomEditor(typeof(Line3D))]
    public class ELine3D : Editor {

        private static BezierPath3D s_tempBezier = new BezierPath3D();

        Line3D _go;
        static EBezierPath _bezierEditor = new EBezierPath();
        protected void OnEnable() {
            _go = target as Line3D;
            _go.MeshFilter.hideFlags = HideFlags.NotEditable;
        }

        private void OnDisable() {
            if (_go.transform.parent != null) {
                var line3DRoadPiece = _go.transform.parent.GetComponent<Line3DRoadPiece>();
                if (line3DRoadPiece != null) {
                    line3DRoadPiece.OnChildDisabled();
                }
            }
        }

        public override void OnInspectorGUI() {
            if (!_go) {
                return;
            }
            if(_go.path == null) {
                _go.path = new BezierPath3D();
            }
            base.OnInspectorGUI();
            _go.EnsureInitialization();
            Undo.undoRedoPerformed = UndoRedoPerformed;
            _go.MeshScale = EditorGUILayout.Vector3Field("mesh scale", _go.MeshScale);
            _go.Mesh = EditorLayoutDrawer.ObjectField("mesh", _go.Mesh);
            _go.TileMode = (Line3D.Mode)EditorGUILayout.EnumPopup("tile mode", _go.TileMode);
            _go.TileDirection = (Line3D.Direction)EditorGUILayout.EnumPopup("tile direction", _go.TileDirection);

            if (_bezierEditor.OnInspectorGUI(_go.path)){
                _go.SetDirty();
            }

            ColliderField();
            if (GUI.changed) {
                _go.ReconstructImmediate();
                EditorExtensions.SetDirty(_go.gameObject);
            }
        }

        private void ColliderField() {
            bool hasCollider = _go.GetComponent<MeshCollider>();
            bool value = EditorGUILayout.Toggle("has collider", hasCollider);
            if(value != hasCollider) {
                if (value) {
                    _go.gameObject.AddComponent<MeshCollider>();
                }
                else {
                    _go.GetComponent<MeshCollider>().DestroySelf();
                }
            }
            if (value) {
                _go.GetComponent<MeshCollider>().sharedMesh = _go.MeshFilter.sharedMesh;
            }
        }

        private void UndoRedoPerformed() {
            if (_go) {
                _go.SetDirty();
                _go.ReconstructImmediate();
            }
        }

        private void OnSceneGUI() {
            if (!_go) {
                return;
            }

            _go.EnsureInitialization();
            CopyTo(_go.path, s_tempBezier);

            Handles.matrix = _go.transform.localToWorldMatrix;
            var edited = _bezierEditor.OnSceneGUI(s_tempBezier);
            Handles.matrix = Matrix4x4.identity;
            if (edited) {

                Undo.RecordObject(_go, "path edit");
            }

            CopyTo(s_tempBezier, _go.path);
            if (edited) {
                _go.SetDirty();
                _go.ReconstructImmediate();
                EditorExtensions.SetDirty(_go.gameObject);
            }
        }

        void CopyTo(object source, object target) {

            var bod = BinarySerializer.Instance.Serialize(source.GetType(), source);
            BinaryDeserializer.Instance.DeserializeTo(bod, target);
        }
    }

}