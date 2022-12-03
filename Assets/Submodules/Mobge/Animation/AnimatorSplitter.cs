using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Animation {
    [Serializable]
    public struct AnimatorSplitter {
        [AnimatorState] public int animation;
        [AnimatorFloatParameter] public int animationSpeed;
        public int layer;
        public Division[] divisions;

        public bool HasAnimation {
            get => animation != 0;
        }

        public float TotalTime {
            get {
                float total = 0;
                for (int i = 0; i < divisions.Length; i++) {
                    total = divisions[i].duration;
                }
                return total;
            }
        }
        public void Start(Animator animator, out Updater updater) {
            


            updater.currentIndex = 0;
            updater.lastAnimStartTime = 0;
            updater.stateStartTime = Time.time;
            if (HasAnimation) {

                animator.CrossFadeInFixedTime(animation, 0.07f,layer);
                animator.Update(0);
                updater.animationDuration = animator.GetActiveStateInfo(layer).length;
                ApplySpeed(ref updater, animator, divisions[0]);
            }
            else {
                updater.animationDuration = 1f;
            }
        }

        private void ApplySpeed(ref Updater updater, Animator animator, Division division) {
            
            var animTime = division.animationProgress * updater.animationDuration;
            var animDuration = animTime - updater.lastAnimStartTime;
            updater.lastAnimStartTime = animTime;
            animator.SetFloat(animationSpeed, animDuration / division.duration);
        }
        public bool Update(Animator animator, ref Updater updater, out bool indexChanged) {
            var d = divisions[updater.currentIndex];
            float passedTime = Time.fixedTime - updater.stateStartTime;
            if ((indexChanged = passedTime >= d.duration)) {
                updater.currentIndex++;
                if (updater.currentIndex >= divisions.Length) {
                    return false;
                }
                updater.stateStartTime = Time.time;
                if (HasAnimation) {
                    ApplySpeed(ref updater, animator, divisions[updater.currentIndex]);
                }
            }
            return true;
        }

        [Serializable]
        public struct Division {
            public float duration;
            public float animationProgress;
        }

        public struct Updater {
            public int currentIndex;
            public float stateStartTime;
            public float lastAnimStartTime;
            public float animationDuration;
            public float GetLastStatesDuration(AnimationSpliter splitter) {
                var d = splitter.divisions[currentIndex];
                return d.duration;
            }
            public float LastStatesTime {
                get {
                    return Time.fixedTime - stateStartTime;
                }
            }
            public void FinishCurrentState() {
                stateStartTime = float.NegativeInfinity;
            }

        }
    }

    public class AnimatorSplitterAttribute : PropertyAttribute {
        public string[] constantIndexNames;
        public AnimatorSplitterAttribute(string[] constantIndexNames) {
            this.constantIndexNames = constantIndexNames;
        }
        public AnimatorSplitterAttribute() { }
    }
}