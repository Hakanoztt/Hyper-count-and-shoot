using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components
{
    [UnityEditor.CustomEditor(typeof(ConnectionControl))]
    public partial class EConnectionControl : EComponentDefinition
    {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor((ConnectionControl.Data)dataObject, this);
        }

        public class Editor : EditableElement<ConnectionControl.Data>
        {
            public Editor(ConnectionControl.Data component, EComponentDefinition editor) : base(component, editor) {
            }

            public override bool SceneGUI(in SceneParams @params) {
                TryGetConnectedInputSlot(ConnectionControl.Data.c_idInput, out _, out Slot slot);
                DataObjectT.e_type = slot._slot.parameter;
                return base.SceneGUI(@params);
            }
        }
    }
}