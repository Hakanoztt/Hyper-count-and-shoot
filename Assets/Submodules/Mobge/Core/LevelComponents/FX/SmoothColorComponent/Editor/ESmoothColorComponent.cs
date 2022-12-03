using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(SmoothColorComponent))]
    public class ESmoothColorComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as SmoothColorComponent.Data, this);
        }
        public class Editor : EditableElement<SmoothColorComponent.Data> {
            private bool _editMode;
            public Editor(SmoothColorComponent.Data component, EComponentDefinition editor) : base(component, editor) {
            }
            // In this method, implement the logic you would normally implement under OnInspectorGUI() 
            public override void DrawGUILayout() {
                // Inspector: Boiler plate for exclusive editing of the element
                base.DrawGUILayout();
                //_editMode = ExclusiveEditField("edit on scene");
            }
            // In this method, implement the logic you would normally implement under OnSceneGUI() 
            public override bool SceneGUI(in SceneParams @params) {
                // Scene: Boiler plate for exclusive editing of the element
                bool enabled = @params.selected /* && _editMode */ ;
                bool edited = false;

                // Logic here. Explicit edit is on, do what you need
                // On Change set edited variable to true to save the data and update the visual

                return enabled && edited;
            }
        }
    }
}
