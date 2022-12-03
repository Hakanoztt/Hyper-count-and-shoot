using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public static class AnimationExtensions {
        private class AnimationUpdater {
            public void UpdateAnimation(in ActionManager.UpdateParams @params) {
                var stt = (AnimationState)@params.data;
                stt.time = @params.progress  * stt.length;
            }
        }
        private static AnimationUpdater _instance;
        static AnimationExtensions() {
            _instance = new AnimationUpdater();
        }
        public static ActionManager.Action PlayState(this UnityEngine.Animation animation, string anim, ActionManager actionManager) {
            animation.Stop();
            if (animation.Play(anim)) {
                var stt = animation[anim];
                stt.speed = 0;
                return actionManager.DoTimedAction(stt.length, _instance.UpdateAnimation, null, stt);
                
            }
            return new ActionManager.Action();
        }
        public static ActionManager.Action CrossFadeState(this UnityEngine.Animation animation, string anim, ActionManager actionManager, float fadeTime) {
            animation.CrossFade(anim, fadeTime);
            var stt = animation[anim];
            stt.speed = 0;
            return actionManager.DoTimedAction(stt.length, _instance.UpdateAnimation, null, stt);
        }



        public static AnimatorStateInfo GetActiveStateInfo(this Animator animator, int layer) {
            var next = animator.GetNextAnimatorStateInfo(layer);
            if (next.shortNameHash == 0) {
                return animator.GetCurrentAnimatorStateInfo(layer);
            }
            return next;
        }
    }




}