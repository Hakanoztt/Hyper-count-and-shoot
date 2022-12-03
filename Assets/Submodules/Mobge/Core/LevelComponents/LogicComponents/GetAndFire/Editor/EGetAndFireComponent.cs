using UnityEditor;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(GetAndFireComponent))]
    public class EGetAndFireComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor((GetAndFireComponent.Data)dataObject, this);
        }
        public class Editor : EditableElement<GetAndFireComponent.Data> {
            public Editor(GetAndFireComponent.Data component, EComponentDefinition editor) : base(component, editor) {
            }
            public override bool SceneGUI(in SceneParams @params) {
                TryGetConnectedSlot(0, out _, out Slot slot);
                DataObjectT.parameterType = slot._slot.returnType;
                return base.SceneGUI(@params);
            }
        }
    }
}