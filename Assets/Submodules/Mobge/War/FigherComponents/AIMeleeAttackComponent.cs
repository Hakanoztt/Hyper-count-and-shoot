using Mobge.Animation;
using Mobge.StateMachineAI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.War {

    public class AIMeleeAttackComponent : BaseAIAttack, IAnimatorOwner, IAIComponent {

        [AnimatorSplitter(new[] { "pre attack", "post attack" })] public AnimatorSplitter attackAnimation;
        public int animationLayer;
        public float damage = 10f;


        private AnimatorSplitter.Updater _updater;
        private IDamagable _attackTarget;

        private bool _isAttacking;

        //public float ColliderEnabledDuration {
        //    get => Time.fixedDeltaTime * 2.5f;
        //}

        public bool IsAttacking => _isAttacking;



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

        public override bool TryStartAttack() {
            if (_isAttacking) {
                return false;
            }
            if (characterDetector.ChooseTarget(out var t) == null) {
                return false;
            }
            _attackTarget = t;
            attackAnimation.Start(_character.GetAnimator(), out _updater);
            _isAttacking = true;
            _character.navigationModule.animationModule.AutoAnimationEnabled = false;
            enabled = true;
            return true;
        }

        protected override bool UpdateAttack() {
            if (!_isAttacking) {
                enabled = false;
                return false;
            }
            if(attackAnimation.Update(_character.GetAnimator(), ref _updater, out bool indexChanged)) {
                _character.navigationModule.moveModule.Turn(_attackTarget.GetPosition() - _character.transform.position);
                if (indexChanged && _updater.currentIndex == 1) {
                    _attackTarget.TakeDamage(new DamageData(damage));
                }
            }
            else {
                _isAttacking = false;
                _character.navigationModule.animationModule.AutoAnimationEnabled = true;

            }
            return _isAttacking;
        }

    }
}