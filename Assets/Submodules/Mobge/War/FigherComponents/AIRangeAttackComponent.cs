using Mobge.Animation;
using Mobge.StateMachineAI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.War {
    public class AIRangeAttackComponent : BaseAIAttack, IAnimatorOwner, IAIComponent {


        [AnimatorSplitter(new[] { "pre attack", "post attack" })] public AnimatorSplitter attackAnimation;
        public int animationLayer;
        public float damage = 10f;

        public Pool<FollowingProjectile> projectile;

        private bool _isAttacking;
        private IDamagable _attackTarget;

        private AnimatorSplitter.Updater _updater;

        protected new void Awake() {
            base.Awake();
            projectile.Init();
        }


        protected override bool UpdateAttack() {
            return false;
        }

        public override bool TryStartAttack() {
            if (_isAttacking) {
                return false;
            }
            if(characterDetector.ChooseTarget(out var t) == null) {
                return false;
            }
            _attackTarget = t;
            attackAnimation.Start(_character.GetAnimator(), out _updater);
            _isAttacking = true;
            _character.navigationModule.animationModule.AutoAnimationEnabled = false;
            enabled = false;
            return true;
        }

        private void HandleTargetHit(FollowingProjectile arg1, IDamagable arg2) {

        }

        void IAIComponent.OnAIEnable(bool enabled) {
            _isAttacking = false;
            _attackTarget = null;
            _updater.FinishCurrentState();
        }

        Animator IAnimatorOwner.GetAnimator() {
            var p = transform.parent;
            if (p != null) {
                var c = p.GetComponentInParent<IAnimatorOwner>();
                if (c != null) {
                    return c.GetAnimator();
                }
            }
            return null;
        }

    }
}