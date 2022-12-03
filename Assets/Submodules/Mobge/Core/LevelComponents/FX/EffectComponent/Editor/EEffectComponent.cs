using UnityEngine;
using UnityEditor;
using Mobge.Core;
using Mobge.Core.Components;

namespace Mobge.Microns {
    [CustomEditor(typeof(EffectComponent))]
    public class EEffectComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor((EffectComponent.Data)dataObject, this);
        }
        public class Editor : EditableElement<EffectComponent.Data> {
            private AReusableItem _editorReusable;
            private AReusableItem _lastReference;
            public Editor(EffectComponent.Data component, EEffectComponent editor) : base(component, editor) { }

            public override void DrawGUILayout() {
                var comp = DataObjectT;
                comp.effect.ReferenceItem = EditorLayoutDrawer.ObjectField("Effect", comp.effect.ReferenceItem, false);
                comp.playOnAwake = EditorGUILayout.Toggle("Play On Awake", comp.playOnAwake);
                using (new GUILayout.VerticalScope("Box"))
                    {
                    Position = EditorGUILayout.Vector3Field("Position", Position);
                    comp.rotation = InspectorExtensions.CustomFields.Rotation.DrawAsVector3(comp.rotation);
                    comp.scale = InspectorExtensions.CustomFields.Scale.Draw(comp.scale);
                }
            }
            
            public override void UpdateVisuals(Transform instance) {
                if (instance != null) {
                    var comp = DataObjectT;
                    _editorReusable = instance.GetComponent<AReusableItem>();
                    _editorReusable.EditorUpdate(comp.position, comp.rotation, comp.scale, Time.realtimeSinceStartup);
                }
            }

            public override bool SceneGUI(in SceneParams @params) {
                var comp = DataObjectT;
                if (@params.selected) {
                    if (_editorReusable != null) {
                        _editorReusable.EditorUpdate(comp.position, comp.rotation, comp.scale, Time.realtimeSinceStartup);
                        SceneView.currentDrawingSceneView.Repaint();
                    }
                }
                return false;
            }
        }
    }
}