using Mobge.Core;
using Mobge.Platformer;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(Rigidbody2DInstantiaterComponent))]
    public class ERigidbody2DInstantiaterComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as Rigidbody2DInstantiaterComponent.Data, this);
        }
        public class Editor : EditableElement<Rigidbody2DInstantiaterComponent.Data> {
            private bool _editMode;
            public Editor(Rigidbody2DInstantiaterComponent.Data component, EComponentDefinition editor) : base(component, editor) {
            }
            // In this method, implement the logic you would normally implement under OnInspectorGUI() 
            public override void DrawGUILayout() {
                // Inspector: Boiler plate for exclusive editing of the element
                base.DrawGUILayout();
                //_editMode = ExclusiveEditField("edit on scene");
            }
            private Rigidbody2D BodyRes {
                get {
                    var pr = DataObjectT.PrefabReference;
                    if (pr == null)
                        return null;
                    return pr.GetComponent<Rigidbody2D>();
                }
            }
            // In this method, implement the logic you would normally implement under OnSceneGUI() 
            public override bool SceneGUI(in SceneParams @params) {
                // Scene: Boiler plate for exclusive editing of the element
                bool enabled = @params.selected /* && _editMode */ ;
                bool edited = false;
                var m = ElementEditor.BeginMatrix(@params.matrix);
                var bodyRes = BodyRes;
                var gravityScale = bodyRes ? bodyRes.gravityScale : 1;
                EProjectileShootData.DrawProjectile(Vector3.zero, Physics2D.gravity * gravityScale, DataObjectT.spawnVelocity, 4, 0.1f);
                ElementEditor.EndMatrix(m);

                // Logic here. Explicit edit is on, do what you need
                // On Change set edited variable to true to save the data and update the visual

                return enabled && edited;
            }
        }
    }
}
