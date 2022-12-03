using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(Collider3DComponent))]
    public class ECollider3DComponent : EComponentDefinition {
        private static Collider3DShapeEditor _shapeEditor = new Collider3DShapeEditor();
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as Collider3DComponent.Data, this);
        }
        public class Editor : EditableElement<Collider3DComponent.Data> {
            private bool _editMode;
            private void EnsureObject() {
                DataObjectT.shape.EnsureData();
            }
            public Editor(Collider3DComponent.Data component, EComponentDefinition editor) : base(component, editor) {
            }
            public override void DrawGUILayout() {
                _editMode = ExclusiveEditField("edit on scene");
                var obj = DataObjectT;
                obj.EnsureLayer(level);
                base.DrawGUILayout();
                obj.layerMask = InspectorExtensions.LayerMaskField("Layer Mask", obj.layerMask);
                _shapeEditor.OnInspectorGUI(ref DataObjectT.shape);
            }
            public override bool SceneGUI(in SceneParams @params) {
                bool enabled = @params.selected && _editMode;
                if (true) {
                    var mat = Handles.matrix;
                    Handles.matrix = mat * @params.matrix;
                    EnsureObject();
                    UpdateShape(@params.selected);
                    Handles.matrix = mat;
                }
                var t = Event.current.type;
                return enabled && (t == EventType.Used || t == EventType.MouseUp);
            }
            private void UpdateShape(bool enabled) {
                _shapeEditor.OnSceneGUI(ref DataObjectT.shape, _editMode);
            }
        }
    }
}
