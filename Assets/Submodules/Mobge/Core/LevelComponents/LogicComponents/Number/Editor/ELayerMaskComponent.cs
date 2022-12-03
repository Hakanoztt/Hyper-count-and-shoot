using System;
using UnityEngine;
using UnityEditor;
using Mobge.Core;
using Mobge.Core.Components;

namespace Mobge.Microns {
    [CustomEditor(typeof(LayerMaskComponent))]
    public class ELayerMaskComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor((NumberComponent.Data)dataObject, this);
        }
        public class Editor : EditableElement<NumberComponent.Data> {
            private bool _editMode;
            public Editor(NumberComponent.Data component, ELayerMaskComponent editor) : base(component, editor) {
            }
            public override void DrawGUILayout() {
                //var o = DataObjectT;
                //o._value = InspectorExtensions.LayerMaskField("Value", (int)o._value);
                //o.triggerOnAwake = EditorGUILayout.Toggle("Trigger On Awake", o.triggerOnAwake);
                base.DrawGUILayout();
            }
        }
    }
}