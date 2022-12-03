using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace Mobge.HyperCasualSetup {
    [CustomEditor(typeof(PrizeSpawnerComponent))]
    public class EPrizeSpawnerComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as PrizeSpawnerComponent.Data, this);
        }
        public class Editor : EditableElement<PrizeSpawnerComponent.Data> {
            private bool _editMode;
            public Editor(PrizeSpawnerComponent.Data component, EComponentDefinition editor) : base(component, editor) {
            }
            // In this method, implement the logic you would normally implement under OnInspectorGUI() 
            public override void DrawGUILayout() {
                // Inspector: Boiler plate for exclusive editing of the element
                base.DrawGUILayout();
                //_editMode = ExclusiveEditField("edit on scene");
            }
            // In this method, implement the logic you would normally implement under OnSceneGUI() 
            public override bool SceneGUI(in SceneParams @params) {
                // Scene: Boiler plate for exclusive editing of the element
                bool enabled = @params.selected /* && _editMode */ ;
                bool edited = false;

                // Logic here. Explicit edit is on, do what you need
                // On Change set edited variable to true to save the data and update the visual

                return enabled && edited;
            }
            public override Transform CreateVisuals() {

                var v = base.CreateVisuals();
                if (v != null) {
                    return v;
                }
                var av = DataObjectT.CreateAutoVisual();
                if(av != null) {
                    return av.transform;
                }
                return null;
            }
            public override void UpdateVisuals(Transform instance) {
                if (instance) {
                    var v = instance.GetComponent<SpriteRenderer>();
                    if (v) {
                        DataObjectT.SelectPrize(out int pIndex);
                        DataObjectT.UpdateVisual(v, pIndex);
                    }
                }
            }
        }
    }
}
