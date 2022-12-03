using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(SpringJointComponent))]
    public class ESpringJointComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor((SpringJointComponent.Data)dataObject, this);
        }
        public class Editor : EditableElement<SpringJointComponent.Data> {

            public Editor(SpringJointComponent.Data component, ESpringJointComponent editor) : base(component, editor) {
            }
        }
    }
}