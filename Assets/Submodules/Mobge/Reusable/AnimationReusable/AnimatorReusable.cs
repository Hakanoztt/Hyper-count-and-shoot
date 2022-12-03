using Mobge.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.AcneRunner {
    public class AnimatorReusable : AReusableItem {
        [OwnComponent] public Animator animator;
        [AnimatorState] public int[] startAnims;

        public override bool IsActive => animator.enabled && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f;

        protected void Awake() {
            animator.enabled = false;
        }

        public override void Stop() {
            animator.enabled = false;
        }

        public override void StopImmediately() {
            animator.enabled = false;
        }

        protected override void OnPlay() {
            animator.enabled = true;
            PlayAnim(startAnims);
        }

        private void PlayAnim(int[] states) {
            if (states != null && states.Length > 0) {
                animator.Play(states[UnityEngine.Random.Range(0, states.Length)], 0, 0);
            }
        }
    }
}