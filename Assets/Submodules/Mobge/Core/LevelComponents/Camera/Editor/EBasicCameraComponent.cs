using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(BasicCameraComponent))]
    public class EBasicCameraComponent : EComponentDefinition {


        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as BasicCameraComponent.Data, this);
        }
        public class Editor : EditableElement<BasicCameraComponent.Data> {

            private bool _apply = false;

            //private bool _editMode = false;
            public Editor(BasicCameraComponent.Data component, EComponentDefinition editor) : base(component, editor) { }
            public override void DrawGUILayout() {
                base.DrawGUILayout();

                _apply = EditorGUILayout.Toggle("apply scene camera", _apply);
            }
            public override bool SceneGUI(in SceneParams @params) {
                return ECameraComponentData.SceneGUI(ElementEditor, @params.matrix, DataObjectT, _apply, @params.solelySelected);

            }

        }
    }
}
             