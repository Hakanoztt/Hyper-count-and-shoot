using UnityEngine;
using UnityEditor;
using Mobge.Core.Components;
using Mobge.Serialization;

namespace Mobge {
    [CustomEditor(typeof(PolygonRenderer))]
    public class EPolygonRenderer : Editor {

        private static readonly PointEditor<Corner> s_pointEditor =
            new PointEditor<Corner>(
                ToVector3,
                UpdateCorner,
                new PointEditor<Corner>.VisualSettings() {
                    lineWidth = 3f,
                    outlineWidth = 1.5f,
                    mode = PointEditor<Corner>.Mode.Path,
                });
        private static Vector3 ToVector3(Corner c) => c.position;
        private static void UpdateCorner(ref Corner t, Vector3 position) {
            t.position = position;
        }

        PolygonRenderer _go;
        private void OnEnable() {
            _go = target as PolygonRenderer;
        }
        private void EnsureData() {
            if (_go.data.polygons == null) {
                _go.data.polygons = new Polygon[0];
                _go.data.polygons[0].corners = new Corner[0];
            }
            _go.EnsureInstance();
        }
        private PolygonRenderer.Data BeginEdit() {
            return Duplicate(_go.data);
        }
        private T Duplicate<T>(T t) {
            var data = BinarySerializer.Instance.Serialize(typeof(T), t);
            return BinaryDeserializer.Instance.Deserialize<T>(data);
        }
        private void EndEdit(in PolygonRenderer.Data data, bool dirty) {
            if (dirty) {
                Undo.RecordObject(_go, "data edit");
            }
            _go.data = Duplicate(data);
            if (dirty) {
                EditorExtensions.SetDirty(_go);
            }
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (_go == null) return;
            if (!_go.data.IsValid) return;
            
            EnsureData();
            var data = BeginEdit();

            var s = s_pointEditor.SelectedPolygon;
            EditorGUILayout.LabelField("count: " + data.polygons.Length);
            EditorLayoutDrawer.CustomArrayField("polygons", ref data.polygons, (rects, polygon) => {
                polygon.noCollider = EditorGUI.Toggle(rects.NextRect(), "no collider", polygon.noCollider);
                polygon.skinScale = EditorGUI.FloatField(rects.NextRect(), "skin scale", polygon.skinScale);
                polygon.height = EditorGUI.FloatField(rects.NextRect(), "height", polygon.height);
                if (GUI.Button(rects.NextRect(), "Reverse")) {
                    polygon.corners.ReverseDirection();
                }
                if (GUI.Button(rects.NextRect(), "Shift Right")) {
                    polygon.corners.Shift(1);
                }
                if (GUI.Button(rects.NextRect(), "Shift Left")) {

                    polygon.corners.Shift(-1);
                }
                return polygon;
            }, ref s);
            s_pointEditor.SelectedPolygon = s;

            if (GUI.changed) {
                EndEdit(data, true);
                _go.UpdateVisuals();
            }
        }
        private Corner[][] ToCorners(Polygon[] polygons) {
            
            Corner[][] corners = new Corner[polygons.Length][];
            for(int i = 0; i < polygons.Length; i++) {
                corners[i] = polygons[i].corners;
            }
            return corners;
        }
        private void UpdateFromCorners(Polygon[] polygons, Corner[][] corners) {
            for (int i = 0; i < polygons.Length; i++) {
                polygons[i].corners = corners[i];
            }
        }
        private void OnSceneGUI() {
            if(_go == null) return;
            if (!_go.data.IsValid) return;
            EnsureData();
            var data = BeginEdit();

            var mat = Handles.matrix;
            Handles.matrix = _go.transform.localToWorldMatrix;

            var corners = ToCorners(data.polygons);
            bool edited = s_pointEditor.OnSceneGUI(corners, true);
            UpdateFromCorners(data.polygons, corners);
            Handles.matrix = mat;
            
            EndEdit(data, edited);
            
            if (edited) {
                _go.UpdateVisuals();
            }

        }
    }
}