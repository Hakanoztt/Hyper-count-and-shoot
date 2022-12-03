using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(ColliderComponent), true)]
    public class EColliderComponent : EComponentDefinition {
        private static Collider2DShapeEditor s_shapeEditor = new Collider2DShapeEditor();
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as ColliderComponent.Data, this);
        }
        public class Editor : EditableElement<ColliderComponent.Data> {
            private bool _editMode;
            private void EnsureObject() {
                DataObjectT.shape.EnsureData();
            }
            public Editor(ColliderComponent.Data component, EColliderComponent editor) : base(component, editor) {
            }
            public override void DrawGUILayout() {
                _editMode = ExclusiveEditField("edit on scene");
                var obj = DataObjectT;
                obj.EnsureLayer(level);
                base.DrawGUILayout();
                s_shapeEditor.OnInspectorGUI(ref DataObjectT.shape);
            }
            public override bool SceneGUI(in SceneParams @params) {
                bool enabled = @params.selected && _editMode;
                {
                    var temp = this.ElementEditor.BeginMatrix(@params.matrix);
                    //Handles.matrix = mat * Matrix4x4.Translate(@params.position);
                    EnsureObject();
                    UpdateShape();
                    this.ElementEditor.EndMatrix(temp);
                }
                var t = Event.current.type;
                return enabled && (t == EventType.Used || t == EventType.MouseUp);
            }
            private void UpdateShape() {
                s_shapeEditor.OnSceneGUI(ref DataObjectT.shape, _editMode);
            }
        }
    }
}
