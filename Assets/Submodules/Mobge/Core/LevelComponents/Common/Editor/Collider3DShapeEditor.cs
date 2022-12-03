using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mobge.Core.Components {
    public class Collider3DShapeEditor {
        private EditorTools _tools;
        private const float c_sphereDivisionCount = 10;
        public Collider3DShapeEditor() {
            _tools = new EditorTools();
        }
        public void OnInspectorGUI(ref Collider3DShape shape) {
            shape.EnsureData();
            shape.shape = (Collider3DShape.Shape)EditorGUILayout.EnumPopup("shape", shape.shape);
        }
        public void OnSceneGUI(ref Collider3DShape shape, bool enabled = true) {
            OnSceneGUI(ref shape, enabled, new Color(0.5f, 1f, 0.5f, 1f));
        }
        public void OnSceneGUI(ref Collider3DShape shape, bool enabled, Color c) {
            shape.EnsureData();
            Handles.color = c;
            switch (shape.shape) {
                case Collider3DShape.Shape.Box:
                    if (enabled) {
                        var handleSize = HandleUtility.GetHandleSize(shape.Offset) * 0.15f;
                        Draw3DCubeHandle(shape, handleSize, out var offset, out var size);
                        shape.Offset = offset;
                        shape.Size = size;
                        _tools.OnSceneGUI();
                    }
                    Handles.DrawWireCube(shape.Offset, shape.Size);
                    break;
                case Collider3DShape.Shape.Sphere:
                    if (enabled) {
                        var handleSize = HandleUtility.GetHandleSize(shape.Offset) * 0.15f;
                        shape.Offset = Handles.FreeMoveHandle(shape.Offset, Quaternion.identity, handleSize, Vector3.zero, Handles.SphereHandleCap);
                        shape.Radius = Handles.Slider(shape.Offset + new Vector3(0, shape.Radius), Vector3.up).y - shape.Offset.y;
                        _tools.OnSceneGUI();
                    }
                    var facing = Quaternion.identity;
                    for(int i=0;i< c_sphereDivisionCount; i++) {
                        Handles.DrawWireDisc(shape.Offset, facing * Vector3.forward, shape.Radius);
                        facing = Quaternion.AngleAxis(180f / c_sphereDivisionCount, Vector3.up) * facing;
                    }
                    facing = new Quaternion(0, 0.707f, 0, 0.707f);
                    for (int i = 0; i < c_sphereDivisionCount; i++) {
                        Handles.DrawWireDisc(shape.Offset, facing * Vector3.forward, shape.Radius);
                        facing = Quaternion.AngleAxis(180f / c_sphereDivisionCount, Vector3.forward) * facing;
                    }
                    break;
                default:
                    break;
            }
        }
        public static void Draw3DCubeHandle(ref Bounds bounds, float handleSize) {
            // Offset
            var offset = Handles.DoPositionHandle(bounds.center, Quaternion.identity);
            // Size
            var size = bounds.size;
            var halfSize = size / 2f;
            Vector3 newSize;
            newSize = new Vector3(Handles.Slider(offset + new Vector3(halfSize.x, 0, 0), Vector3.right).x,
                                  Handles.Slider(offset + new Vector3(0, halfSize.y, 0), Vector3.up).y,
                                  Handles.Slider(offset + new Vector3(0, 0, halfSize.z), Vector3.forward).z) - offset + halfSize;
            Vector3 newSize2;
            newSize2 = (new Vector3(Handles.Slider(offset + new Vector3(-halfSize.x, 0, 0), -Vector3.right).x,
                                  Handles.Slider(offset + new Vector3(0, -halfSize.y, 0), -Vector3.up).y,
                                  Handles.Slider(offset + new Vector3(0, 0, -halfSize.z), -Vector3.forward).z) - offset) * -1 + halfSize;
            size += newSize + newSize2 - size * 2;
            bounds.center = offset;
            bounds.size = size;
        }
        private void Draw3DCubeHandle(in Collider3DShape shape, float handleSize, out Vector3 offset, out Vector3 size) {
            Bounds b = new Bounds(shape.Offset, shape.Size);
            Draw3DCubeHandle(ref b, handleSize);
            offset = b.center;
            size = b.size;

        }
    }
}