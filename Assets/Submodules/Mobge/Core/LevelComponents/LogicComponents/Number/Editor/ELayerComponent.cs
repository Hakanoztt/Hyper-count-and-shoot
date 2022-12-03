using UnityEngine;
using UnityEditor;
using Mobge.Core;
using Mobge.Core.Components;

namespace Mobge.Microns {
    [CustomEditor(typeof(LayerComponent))]
    public class ELayerComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor((NumberComponent.Data)dataObject, this);
        }
        public class Editor : EditableElement<NumberComponent.Data> {
            private bool _editMode;
            public Editor(NumberComponent.Data component, ELayerComponent editor) : base(component, editor) {
            }
            public override void DrawGUILayout() {
                var o = DataObjectT;
                o._value = EditorGUILayout.LayerField("Value", (int) o._value);
                o.triggerOnAwake = (NumberComponent.Data.Mode)EditorGUILayout.EnumPopup("modes", o.triggerOnAwake);
            }
        }
    }
}