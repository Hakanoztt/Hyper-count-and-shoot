using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Mobge.Animation {
    [CustomPropertyDrawer(typeof(AnimatorSplitter))]
    public class AnimatorSplitterPropertyDrawer : BasePropertyDrawer {
        private LayoutRectSource _source = new LayoutRectSource();
        private AnimatorSplitterAttribute _attribute;

        public AnimatorSplitterAttribute Attribute {
            get {
                if (_attribute == null) return attribute as AnimatorSplitterAttribute;
                return _attribute;
            }
            set {
                _attribute = value;
            }
        }
        private void SetTime(Animator anim, int state, int track, float normalizedTime) {
            if (anim == null) {
                return;
            }
            anim.Play(state, track, normalizedTime);
            anim.Update(0);
        }
        private float _lastProgress = 0;
        private bool TryGetState(AnimatorController animator, int layer, int hash, out AnimatorState state) {
            var ls = animator.layers;
            if (layer >= 0 && layer < ls.Length) {
                var l = ls[layer];
                var ss = l.stateMachine.states;
                for (int i = 0; i < ss.Length; i++) {
                    if (ss[i].state.nameHash == hash) {
                        state = ss[i].state;
                        return true;
                    }
                }
            }
            state = null;
            return false;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            _source.Reset(position);
            var anim = AnimatorUtility.FindAnimator(property.serializedObject.targetObject);
            AnimatorController controller = null;
            if (anim && anim.runtimeAnimatorController) {
                controller = (AnimatorController)anim.runtimeAnimatorController;
            }

            AnimatorSplitterAttribute attribute = Attribute;
            string[] constantNames = attribute != null ? attribute.constantIndexNames : null;
            var animation = property.FindPropertyRelative(nameof(AnimatorSplitter.animation));
            var animationSpeed = property.FindPropertyRelative(nameof(AnimatorSplitter.animationSpeed));
            var layer = property.FindPropertyRelative(nameof(AnimatorSplitter.layer));
            if (controller != null) {
                animation.intValue = AnimatorUtility.AnimatorStateField(_source.NextRect(), label, controller, animation.intValue);
                EditorGUI.PropertyField(_source.NextRect(), animationSpeed);
            }
            else {
                animation.intValue = 0;
            }


            float motionTime = 1f;
            {
                if (controller != null && TryGetState(controller, layer.intValue, animation.intValue, out var state)) {

                    if (state.motion is AnimationClip clip) {
                        motionTime = clip.length;
                    }
                }
            }


            EditorGUI.BeginChangeCheck();
            var animProg = EditorGUI.Slider(_source.NextRect(), "animation progress", _lastProgress, 0f, 1f);
            if (EditorGUI.EndChangeCheck()) {
                _lastProgress = animProg;
                SetTime(anim, animation.intValue, layer.intValue, animProg);
            }


            var divisions = property.FindPropertyRelative(nameof(AnimatorSplitter.divisions));

            if (divisions.arraySize == 0) {
                EditorGUI.LabelField(_source.NextRect(), "At least one split have to be added.");
            }

            float lastAnimTime = 0;
            var list = EditorDrawer.CustomListField(_source, divisions, (rects, element, index) => {
                if (index == 0) {
                    lastAnimTime = 0;
                }
                var duration = element.FindPropertyRelative(nameof(AnimatorSplitter.Division.duration));
                var aimationProgress = element.FindPropertyRelative(nameof(AnimatorSplitter.Division.animationProgress));
                EditorGUI.BeginChangeCheck();
                aimationProgress.floatValue = EditorGUI.Slider(rects.NextRect(), aimationProgress.displayName, aimationProgress.floatValue, 0f, 1f);
                bool changed = EditorGUI.EndChangeCheck();
                duration.floatValue = EditorGUI.FloatField(rects.NextRect(), duration.displayName, duration.floatValue);
                var animationTime = motionTime * aimationProgress.floatValue;
                var animationDuration = animationTime - lastAnimTime;
                EditorGUI.BeginChangeCheck();
                var speed = EditorGUI.FloatField(rects.NextRect(), "animation speed ", animationDuration / duration.floatValue);
                LayoutRectSource lrs = rects;

                lastAnimTime = animationTime;
                if (EditorGUI.EndChangeCheck()) {
                    duration.floatValue = animationDuration / speed;
                }
                EditorGUI.LabelField(rects.NextRect(), "animation duration", "" + animationDuration);
                if (GUI.Button(rects.NextRect(), "show pose") || changed) {
                    SetTime(anim, animation.intValue, layer.intValue, aimationProgress.floatValue);
                }
            });

            SetHeight(property, _source.Height);
        }



    }
}