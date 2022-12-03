using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.Components
{
    public class Collider2DShapeEditor
    {
        private static Vector2[][] s_points = new Vector2[1][];
        private PointEditor<Vector2> _editor;
        public Collider2DShapeEditor() {
            _editor = new PointEditor<Vector2>(ToVector, UpdateValue);
            _tools = new EditorTools();
        }
        private EditorTools _tools;

        private static Vector3 ToVector(Vector2 arg) {
            return arg;
        }
        private static void UpdateValue(ref Vector2 t, Vector3 position) {
            t = position;
        }
        public void OnInspectorGUI(ref Collider2DShape shape) {
            shape.EnsureData();
            shape.shape = (Collider2DShape.Shape)EditorGUILayout.EnumPopup("shape", shape.shape);
        }
        public void OnSceneGUI(ref Collider2DShape shape, bool enabled = true) {
            OnSceneGUI(ref shape, enabled, new Color(0.5f, 1f, 0.5f, 1f));
        }
        public void OnSceneGUI(ref Collider2DShape shape, bool enabled, Color c) {
            shape.EnsureData();
            Handles.color = c;
            switch (shape.shape) {
                default:
                case Collider2DShape.Shape.Polygon:
                    s_points[0] = shape.points;
                    _editor.SelectedPolygon = 0;
                    _editor.OnSceneGUI(s_points, enabled);
                    shape.points = s_points[0];
                    break;
                case Collider2DShape.Shape.Circle:
                    if (enabled) {
                        var handleSize = HandleUtility.GetHandleSize(shape.Offset) * 0.15f;
                        shape.Offset = Handles.FreeMoveHandle(shape.Offset, Quaternion.identity, handleSize, Vector3.zero, Handles.SphereHandleCap);
                        shape.Radius = Handles.Slider(shape.Offset + new Vector2(0, shape.Radius), Vector2.up).y - shape.Offset.y;
                        _tools.OnSceneGUI();
                    }
                    var from = shape.Offset;
                    from.y += shape.Radius;
                    Handles.DrawWireArc(shape.Offset, Vector3.forward, from, 360, shape.Radius);
                    break;
                case Collider2DShape.Shape.Capsule: {
                        var size = shape.Size;
                        Rect rect = new Rect(shape.Offset - size * 0.5f, shape.Size);
                        if (enabled) {
                            rect = HandlesExtensions.RectHandle(rect);
                            shape.Offset = rect.center;
                            shape.Size = rect.size;
                            _tools.OnSceneGUI();
                        }
                        float height = Mathf.Max(size.x, size.y);
                        float radius = size.x * 0.5f;
                        Vector2 up = shape.Offset + new Vector2(0, height*0.5f - radius);
                        Vector2 bottom = shape.Offset + new Vector2(0, -height * 0.5f + radius);
                        Handles.DrawWireArc(up, Vector3.forward, new Vector2(radius,0), 180, radius);
                        Handles.DrawWireArc(bottom, Vector3.forward, new Vector2(-radius, 0),180, radius);
                        Handles.DrawLine(up + new Vector2(radius, 0), bottom + new Vector2(radius, 0));
                        Handles.DrawLine(up + new Vector2(-radius, 0), bottom + new Vector2(-radius, 0));
                    }
                    break;
                case Collider2DShape.Shape.Rectangle: {
                        var size = shape.Size;
                        Rect rect = new Rect(shape.Offset - size * 0.5f, shape.Size);
                        if (enabled) {
                            rect = HandlesExtensions.RectHandle(rect);
                            shape.Offset = rect.center;
                            shape.Size = rect.size;
                            _tools.OnSceneGUI();
                        }
                        Handles.DrawSolidRectangleWithOutline(rect, Color.clear, Handles.color);
                    }
                    break;
            }
        }
    }
}