using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace Mobge.IdleGame.CustomerShopSystem
{
    [CustomEditor(typeof(CustomerSpawnControllerComponent))]
    public class ECustomerSpawnControllerComponent : EComponentDefinition
    {
        public override EditableElement CreateEditorElement(BaseComponent dataObject)
        {
            return new Editor(dataObject as CustomerSpawnControllerComponent.Data, this);
        }
        public class Editor : EditableElement<CustomerSpawnControllerComponent.Data>
        {
            //private bool _editMode = false;
            public Editor(CustomerSpawnControllerComponent.Data component, EComponentDefinition editor) : base(component, editor) { }

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
