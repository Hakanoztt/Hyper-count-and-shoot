using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace Mobge {
    [CustomEditor(typeof(PointComponent))]
    public class EPointComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as PointComponent.Data, this);
        }
        public class Editor : EditableElement<PointComponent.Data> {
            //private bool _editMode = false;
            public Editor(PointComponent.Data component, EComponentDefinition editor) : base(component, editor) { }
            // public override void DrawGUILayout() {
            //     base.DrawGUILayout();
            //     _editMode = ExclusiveEditField("Edit On Scene");
            //     if (GUILayout.Button("Example Button")) {
            //         Debug.Log("Button Does Stuff");
            //     }
            //     GUILayout.Label("Example Label");
            // }
            // public override bool SceneGUI(in SceneParams @params) {
            //     bool enabled = @params.selected;
            //     bool edited = false;
            //     var matrix = ElementEditor.BeginMatrix(@params.matrix);
            //     // always on scene gui code goes here
            //     if (_editMode) {
            //         //edit mode scene gui code goes here
            //     }
            //     ElementEditor.EndMatrix(matrix);
            //     return enabled && edited;
            // }
        }
    }
}
             