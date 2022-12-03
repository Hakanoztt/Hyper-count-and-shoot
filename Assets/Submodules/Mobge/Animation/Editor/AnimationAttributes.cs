using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Mobge.UnityExtensions;

namespace Mobge.Animation {
    public static class AnimationEditorUtility {
        public static AAnimation FindAnimation(UnityEngine.Object @object) {
            var c = @object as Component;
            if (c == null) {
                return null;
            }
            IAnimationOwner anim = c as IAnimationOwner;
            if (anim == null) {
                anim = UnityExtensions.GetComponentInChildren<IAnimationOwner>(c.transform);
            }
            if (anim == null) {
                anim = UnityExtensions.GetComponentInParent<IAnimationOwner>(c.transform);
            }
            if (anim != null) {
                return anim.Animation;
            }
            return c.GetComponent<AAnimation>();
        }
        public static void ErrorField(Rect position) {
            EditorGUI.LabelField(position, "Must have " + typeof(IAnimationOwner) + " component attached to the gameobject.");
        }
    }
    [CustomPropertyDrawer(typeof(AnimationAttribute))]
    public class AnimationAttributeDrawer : BasePropertyDrawer {
        private LayoutRectSource _source = new LayoutRectSource();
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            _source.Reset(position);
            var c = property.serializedObject.targetObject;
            var anim = AnimationEditorUtility.FindAnimation(property.serializedObject.targetObject);
            if (!anim) {
                AnimationEditorUtility.ErrorField(position);
                return;
            }
            AnimationAttribute attribute = (AnimationAttribute)this.attribute;
            var anims = anim.AnimationList;

            EditorGUI.BeginChangeCheck();
            property.intValue = Mobge.EditorDrawer.Popup(_source, label.text, anims, property.intValue, attribute.includeNone ? "<no animation>" : null);

            if (EditorGUI.EndChangeCheck()) {
                property.serializedObject.ApplyModifiedProperties();
            }
            SetHeight(property, _source.Height);
        }
    }

    [CustomPropertyDrawer(typeof(AnimationSplitterAttribute))]
    [CustomPropertyDrawer(typeof(AnimationSpliter))]
    public class AnimationSpliterDrawer : BasePropertyDrawer {
        private LayoutRectSource _source = new LayoutRectSource();
        private AnimationSplitterAttribute _attribute;
        public AnimationSplitterAttribute Attribute {
            get {
                if (_attribute == null) return attribute as AnimationSplitterAttribute;
                return _attribute;
            }
            set {
                _attribute = value;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            _source.Reset(position);
            var anim = AnimationEditorUtility.FindAnimation(property.serializedObject.targetObject);
            if (!anim) {
                AnimationEditorUtility.ErrorField(_source.NextRect());
            }
            else {
                AnimationSplitterAttribute attribute = Attribute;
                string[] constantNames = attribute != null ? attribute.constantIndexNames : null;
                var animation = property.FindPropertyRelative(nameof(AnimationSpliter.animation));
                animation.intValue = EditorDrawer.Popup(_source, animation.displayName, anim.AnimationList, animation.intValue);
                if (animation.intValue >= 0 && animation.intValue < anim.AnimationList.Length) {
                    anim.EnsureInit();
                    var curStt = anim.GetCurrent(0);
                    AnimationState stt = (curStt != null && curStt.AnimationId == animation.intValue) ? curStt : null;
                    Func<AnimationState> updateState = () => {
                        stt = anim.PlayIfNotPlaying(0, animation.intValue);
                        stt.Speed = 0;
                        return stt;
                    };
                    var animData = anim.GetAnimation(animation.intValue);
                    EditorGUI.BeginChangeCheck();
                    var animProg = EditorGUI.Slider(_source.NextRect(), "animation progress", (stt == null ? 0 : stt.Time) / animData.Duration, 0f, 1f);
                    if (EditorGUI.EndChangeCheck()) {
                        updateState();
                        stt.Time = animProg * animData.Duration;
                        anim.ForceUpdate();
                    }
                    var divisions = property.FindPropertyRelative(nameof(AnimationSpliter.divisions));

                    if (divisions.arraySize == 0) {
                        EditorGUI.LabelField(_source.NextRect(), "At least one split have to be added.");
                    }
                    float lastAnimTime = 0;
                    var list = EditorDrawer.CustomListField(_source, divisions, (rects, element, index) => {
                        if (index == 0) {
                            lastAnimTime = 0;
                        }
                        var duration = element.FindPropertyRelative(nameof(AnimationSpliter.Division.duration));
                        var aimationProgress = element.FindPropertyRelative(nameof(AnimationSpliter.Division.aimationProgress));
                        EditorGUI.BeginChangeCheck();
                        aimationProgress.floatValue = EditorGUI.Slider(rects.NextRect(), aimationProgress.displayName, aimationProgress.floatValue, 0f, 1f);
                        bool changed = EditorGUI.EndChangeCheck();
                        duration.floatValue = EditorGUI.FloatField(rects.NextRect(), duration.displayName, duration.floatValue);
                        var animationTime = animData.Duration * aimationProgress.floatValue;
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
                            updateState();
                            stt.Time = aimationProgress.floatValue * stt.Duration;
                            anim.ForceUpdate();
                        }
                    });
                    list.constantElements = constantNames;
                }
            }
            SetHeight(property, _source.Height);
        }
    }
}