using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(CollectableDefinition))]
    public class ECollectableDefinition : EComponentDefinition
    {
        private CollectableDefinition _go;
        protected void OnEnable() {
            _go = target as CollectableDefinition;
        }
        public override EditableElement CreateEditorElement(BaseComponent dataObject)
        {
            var data = dataObject as CollectableDefinition.Data;
            return new Editor(data, this);
        }
        private class Editor : EditableElement<CollectableDefinition.Data> {

            public Editor(CollectableDefinition.Data data, EComponentDefinition editor) : base(data, editor) {

            }

            public override void DrawGUILayout()
            {
                DataObjectT.collectable = EditorLayoutDrawer.ObjectField("collectable", DataObjectT.collectable, false);
            }
        }
    }
}
