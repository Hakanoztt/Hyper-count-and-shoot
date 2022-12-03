
using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(FixedJointComponent))]
    public class EFixedJointComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as FixedJointComponent.Data, this);
        }
        public class Editor : EditableElement<FixedJointComponent.Data> {
            //private bool _editMode = false;
            public Editor(FixedJointComponent.Data component, EComponentDefinition editor) : base(component, editor) { }
            public override void DrawGUILayout() {
                base.DrawGUILayout();
                //_editMode = ExclusiveEditField("edit on scene");
                //if (GUILayout.Button("example button")) {
                //    Debug.Log("button does stuff");
                //}
                //GUILayout.Label("example label");
            }
            //public override bool SceneGUI(in SceneParams @params) {
            //    bool enabled = @params.selected;
            //    bool edited = false;
            //    // var oldMatrix = Handles.matrix;
            //    // Handles.matrix = @params.matrix;
            //    //always on gui
            //    if (_editMode) {
            //        //edit mode gui
            //    }
            //    // Handles.matrix = oldMatrix;
            //    return enabled && edited;
            //}
        }
    }
}
             