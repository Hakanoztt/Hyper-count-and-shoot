using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Mobge.Animation;

namespace Mobge.Core.Components {

    [CustomEditor(typeof(AnimationComponent))]
    public class EAnimationComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor((AnimationComponent.Data)dataObject, this);
        }
        public class Editor : EditableElement<AnimationComponent.Data> {
            private bool _sequencesOpen;
            public Editor(AnimationComponent.Data component, EComponentDefinition editor) : base(component, editor) {

            }
            public override void DrawGUILayout() {
                base.DrawGUILayout();
                var data = DataObjectT;
                data.animation = EditorLayoutDrawer.ObjectField("animation", data.animation, false);
                if(data.sequences == null) {
                    data.sequences = new AnimationComponent.SequenceMap();
                }
                data.rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("rotation", data.rotation.eulerAngles));
                data.scale = InspectorExtensions.CustomFields.Scale.Draw(data.scale);
                if (data.animation != null) {
                    var anims = data.animation.AnimationList;
                    data.defaultAnimation = EditorLayoutDrawer.Popup("default animation", anims, data.defaultAnimation, "none");
                    EditorLayoutDrawer.CustomListField("sequences", data.sequences, (layout, id) => {
                        var sq = data.sequences[id];
                        EditorGUI.LabelField(layout.NextRect(), "id", id.ToString());
                        sq.name = EditorGUI.TextField(layout.NextRect(), "name", sq.name);
                        sq.mode = (AnimationComponent.PlayMode)EditorGUI.EnumPopup(layout.NextRect(), "mode", sq.mode);
                        sq.track = EditorGUI.IntField(layout.NextRect(), "track", sq.track);
                        sq.crossFadeTime = EditorGUI.FloatField(layout.NextRect(), "Cross fade time", sq.crossFadeTime);
                        bool overrideFirstAnimationTime = sq.firstAnimationTime > 0;
                        bool newOverride = EditorGUI.Toggle(layout.NextRect(), "override first animation time", overrideFirstAnimationTime);
                        if (newOverride != overrideFirstAnimationTime) {
                            sq.firstAnimationTime = newOverride ? 1 : 0;
                        }
                        if (newOverride) {
                            sq.firstAnimationTime = EditorGUI.FloatField(layout.NextRect(), "first animation time", sq.firstAnimationTime);
                            sq.firstAnimationTime = Mathf.Max(0.001f, sq.firstAnimationTime);
                        }
                        SequenceField(layout, data.animation, ref sq.animations);
                        data.sequences[id] = sq;
                    }, ref _sequencesOpen);
                    // if (UnityEngine.GUILayout.Button("add sequence")) {
                    //     data.sequences.AddElement(new AnimationComponent.Sequence());
                    // }
                }
            }
            public override Transform CreateVisuals() {
                var d = DataObjectT;
                if(d.animation == null) {
                    return null;
                }
                var a = d.CreateAnimation(null);
                return a.transform;
            }
            public override void UpdateVisuals(Transform instance) {
                if(instance != null) {
                    var a = instance.GetComponent<AAnimation>();
                    if(a!= null) {
                        DataObjectT.UpdateVisual(a);
                    }
                }
            }
            private void SequenceField(LayoutRectSource layout, AAnimation animation, ref int[] animations) {
                var anims = animation.AnimationList;
                EditorDrawer.CustomArrayField(layout, "sequence", ref animations, (l, a) => {
                    a = EditorGUI.Popup(l.NextRect(), a, anims);
                    return a;
                });

            }
        }

    }
}