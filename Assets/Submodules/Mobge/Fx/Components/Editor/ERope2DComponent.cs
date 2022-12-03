using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(Rope2DComponent))]
    public class ERope2DComponent : EComponentDefinition {

        private static readonly PointEditor<Vector2> s_pointEditor =
            new PointEditor<Vector2>(
                (t) => t,
                (ref Vector2 t, Vector3 v) => t = v,
                new PointEditor<Vector2>.VisualSettings() {
                        mode = PointEditor<Vector2>.Mode.OpenPath
                    }
                );

        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as Rope2DComponent.Data, this);
        }
        public class Editor : EditableElement<Rope2DComponent.Data> {
            private bool _editMode;
            public Editor(Rope2DComponent.Data component, EComponentDefinition editor) : base(component, editor) {
            }
            // In this method, implement the logic you would normally implement under OnInspectorGUI() 
            public override void DrawGUILayout() {
                // Inspector: Boiler plate for exclusive editing of the element
                base.DrawGUILayout();
                _editMode = ExclusiveEditField("edit on scene");
            }
            // In this method, implement the logic you would normally implement under OnSceneGUI() 
            public override bool SceneGUI(in SceneParams @params) {
                // Scene: Boiler plate for exclusive editing of the element
                bool enabled = @params.selected /* && _editMode */ ;
                bool edited = false;
                if (DataObjectT.positions == null) {
                    DataObjectT.positions = new Vector2[0];
                }
                var temp = ElementEditor.BeginMatrix(@params.matrix);
                edited = s_pointEditor.OnSceneGUI(ref DataObjectT.positions, _editMode);
                ElementEditor.EndMatrix(temp);

                return enabled && edited;
            }
        }
    }
}
