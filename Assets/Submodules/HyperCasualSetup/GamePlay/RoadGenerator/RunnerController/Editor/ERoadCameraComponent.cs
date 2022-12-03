using Mobge.Core;
using Mobge.Core.Components;
using UnityEditor;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    [CustomEditor(typeof(RoadCameraComponent))]
    public class ERoadCameraComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as RoadCameraComponent.Data, this);
        }
        public class Editor : EditableElement<RoadCameraComponent.Data> {

            private bool _apply = false;

            public Editor(RoadCameraComponent.Data component, EComponentDefinition editor) : base(component, editor) { }


            public override void DrawGUILayout() {
                base.DrawGUILayout();

                _apply = EditorGUILayout.Toggle("apply scene camer", _apply);
            }

            public override bool SceneGUI(in SceneParams @params) {
                return ECameraComponentData.SceneGUI(ElementEditor, @params.matrix, DataObjectT, _apply, @params.solelySelected);

            }
        }
    }
}
             