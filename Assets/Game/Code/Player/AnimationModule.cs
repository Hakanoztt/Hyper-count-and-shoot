using System.Collections;
using Mobge.Animation;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Mobge.CountAndShoot {
    public partial class Player {

        [Serializable]
        public class AnimModule {

            public Animator Animator;

            [AnimatorState] public int ThrowAnim;
            [AnimatorState] public int IdleAnim;
            [AnimatorState] public int runAnim;
            [AnimatorState] public int TopShoot;
            [AnimatorState] public int MidShoot;
            [AnimatorState] public int BotShoot;
            [AnimatorState] public int Death;
            [AnimatorState] public int TakeDamage;
            [AnimatorState] public int Dance;
            public void Play(int anim) {
                Animator.CrossFade(anim, 0.1f);
                //     Animator.Play(anim);
            }
        }
    }
}