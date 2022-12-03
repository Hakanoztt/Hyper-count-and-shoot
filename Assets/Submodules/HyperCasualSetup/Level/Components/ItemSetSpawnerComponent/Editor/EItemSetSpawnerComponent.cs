using Mobge.Core;
using Mobge.Core.Components;
using UnityEditor;
using UnityEngine;

namespace Mobge.HyperCasualSetup {
    [CustomEditor(typeof(ItemSetSpawnerComponent))]
    public class EItemSetSpawnerComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as ItemSetSpawnerComponent.Data, this);
        }
        public class Editor : EPrefabSpawnerComponent.Editor<PrefabSpawnerComponent.BaseData>
        {
            public Editor(PrefabSpawnerComponent.BaseData dataObject, EComponentDefinition editor) : base(dataObject, editor)
            {
            }
            public override void UpdateVisuals(Transform instance)
            {
                base.UpdateVisuals(instance);
                if (instance)
                {
                    var p = DataObjectT.PrefabReference;
                    if (p)
                    {
                        instance.localScale = Vector3.Scale(instance.localScale, p.localScale);
                    }
                }
            }
        }
    }
}
