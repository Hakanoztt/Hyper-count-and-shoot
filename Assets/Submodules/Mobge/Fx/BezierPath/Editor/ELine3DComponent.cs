using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Mobge.Core;
using System;

namespace Mobge {
    [CustomEditor(typeof(Line3DComponent))]
    public class ELine3DComponent : EComponentDefinition
    {
        private static EBezierPath _bezierEditor = new EBezierPath();
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor((Line3DComponent.Data)dataObject, this);
        }
        public class Editor : EditableElement<Line3DComponent.Data>
        {
            private bool _editMode;

            public Editor(Line3DComponent.Data component, ELine3DComponent editor) : base(component, editor)
            {
            }
            public override void DrawGUILayout() {
                base.DrawGUILayout();
                var data = DataObjectT;
                if(data.path == null) {
                    data.path = new BezierPath3D();
                }
                data.Rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("rotation", data.Rotation.eulerAngles));
                _bezierEditor.OnInspectorGUI(data.path);

                if (UnityEngine.GUILayout.Button("reverse")) {
                    Array.Reverse(data.path.Points.array, 0, data.path.Points.Count);
                }
                _editMode = ExclusiveEditField();
            }
            public override bool SceneGUI(in SceneParams @params) {
                if (_editMode) {
                    var data = DataObjectT;
                    var te = ElementEditor.BeginMatrix(@params.matrix);
                    var e = _bezierEditor.OnSceneGUI(data.path);
                    ElementEditor.EndMatrix(te);
                    return e;
                }
                return false;
            }
            public override Transform CreateVisuals() {
                var line = DataObjectT.CreateLine(null);
                return line.transform;
            }
            public override void UpdateVisuals(Transform instance) {
                if (instance == null) return;
                var line = instance.GetComponent<Line3D>();
                if (line == null) return;
                DataObjectT.UpdateVisuals(line, true);
            }
        }
    }
}