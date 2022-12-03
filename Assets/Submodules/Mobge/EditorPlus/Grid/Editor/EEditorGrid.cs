using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Mobge {
    [CustomEditor(typeof(EditorGrid))]
    public class EEditorGrid : Editor {

        public const int c_minRadiusRate = 20;
        private static Quaternion s_xzRotation = Quaternion.Euler(90, 0, 0);
        private static Quaternion s_yzRotation = Quaternion.Euler(0, -90, 0);
        private EditorGrid _go;

        private void OnEnable() {
            _go = target as EditorGrid;
        }
        public string[] GetDimensionNames(EditorGrid.GridType gridType) {
            switch (gridType) {
                case EditorGrid.GridType.Square:
                    return new[] { "edge length" };
                case EditorGrid.GridType.Rectangle:
                    return new[] { "x", "y" };
                case EditorGrid.GridType.Triangle:
                    return new[] { "edge length" };
                case EditorGrid.GridType.Radial:
                    return new[] { "radius step", "angle count" };
                case EditorGrid.GridType.None:
                    break;
                default:
                    break;
            }
            return new string[0];
        }
        public override void OnInspectorGUI() {
            if (!_go) {
                return;
            }
            base.OnInspectorGUI();
            var dims = GetDimensionNames(_go.gridType);
            if (_go.dimensions == null || _go.dimensions.Length != dims.Length) {
                _go.dimensions = new float[dims.Length];
            }
            for(int i = 0; i < _go.dimensions.Length; i++) {
                _go.dimensions[i] = EditorGUILayout.FloatField(dims[i], _go.dimensions[i]);
            }
            for(int i = 0; i < _go.dimensions.Length; i++) {
                _go.dimensions[i] = Mathf.Max(0.01f, _go.dimensions[i]);
            }


            if (GUI.changed) {
                EditorExtensions.SetDirty(_go);
            }
        }
        public static void DrawGizmos(EditorGrid grid, Vector3 min, Vector3 max) {
            var dims = grid.dimensions;
            float dim0;
            if (grid.dimensions==null|| grid.dimensions.Length == 0) {
                dim0 = 1;
            }
            else {
                dim0 = grid.dimensions[0];
            }
            min = grid.ToGridPlane(min);
            max = grid.ToGridPlane(max);
            float minRadius = c_minRadiusRate * dim0;
            min = min - new Vector3(minRadius, minRadius, minRadius);
            max = max + new Vector3(minRadius, minRadius, minRadius);
            Matrix4x4 m = Handles.matrix;
            switch (grid.plane) {
                default:
                case EditorGrid.Plane.XY:
                    break;
                case EditorGrid.Plane.XZ:
                    Handles.matrix = m * Matrix4x4.Rotate(s_xzRotation);
                    break;
                case EditorGrid.Plane.YZ:
                    Handles.matrix = m * Matrix4x4.Rotate(s_yzRotation);
                    break;
            }
            Handles.color = grid.color;
            switch (grid.gridType) {
                case EditorGrid.GridType.Square:
                    DrawRectangularGrid(min, max, new Vector2(dims[0], dims[0]));
                    break;
                case EditorGrid.GridType.Rectangle:
                    DrawRectangularGrid(min, max, new Vector2(dims[0], dims[1]));
                    break;
                case EditorGrid.GridType.Triangle:
                    DrawTriangleGrid(min, max, grid.TriangleSnapLocal((min + max) * 0.5f), dims[0]);
                    break;
                case EditorGrid.GridType.Radial:
                    break;
                case EditorGrid.GridType.None:
                    break;
                default:
                    break;
            }
            Handles.color = Color.white;
            Handles.matrix = m;
        }
        static void DrawRectangularGrid(Vector2 min, Vector2 max, Vector2 dims) {
            var iDim = new Vector2(1f / dims.x, 1f / dims.y);
            min.Scale(iDim);
            max.Scale(iDim);
            var minI = Vector2Int.RoundToInt(min);
            var maxI = Vector2Int.RoundToInt(max);
            min = minI;
            max = maxI;
            min.Scale(dims);
            max.Scale(dims);
            for (int i = minI.x; i <= maxI.x; i++) {
                float x = i * dims.x;
                Handles.DrawLine(new Vector3(x, min.y, 0), new Vector3(x, max.y, 0));
            }
            for (int i = minI.y; i <= maxI.y; i++) {
                float y = i * dims.y;
                Handles.DrawLine(new Vector3(min.x, y, 0), new Vector3(max.x, y, 0));
            }
        }
        //     _____
        //    /\   /\
        //   /__\_/__\
        //   \  / \  /
        //    \/___\/
        //
        static void DrawTriangleGrid(Vector2 min, Vector2 max, Vector2 center, float dim) {
            //var center = grid.ToGridPlane((min + max) * 0.5f);
            var radius = (max - min).magnitude * 0.5f + dim * c_minRadiusRate;
            int radiusI = Mathf.RoundToInt(radius / dim);
            radius = radiusI * dim;
            float angle = 0;
            float angleStep = Mathf.PI / 3f;
            Vector2 prevDir = DirectionFromAngle(angle);
            for (int i = 0; i < 3; i++) {
                angle += angleStep;
                var nextDir = DirectionFromAngle(angle);
                var prevDirStep = dim * prevDir;
                var nextDirStep = dim * nextDir;
                var p1 = center + prevDir * radius;
                var p2 = center + nextDir * radius;
                for (int j = 0; j < radiusI; j++) {
                    Handles.DrawLine(p1, p2);
                    p1 -= nextDirStep;
                    p2 -= prevDirStep;
                }
                for (int j = 0; j <= radiusI; j++) {
                    Handles.DrawLine(p1, p2);
                    p1 -= prevDirStep;
                    p2 -= nextDirStep;
                }
                prevDir = nextDir;

            }
        }
        static Vector2 DirectionFromAngle(float radian) {
            return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
        }
    }
}