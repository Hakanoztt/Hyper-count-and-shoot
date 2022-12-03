using Mobge.Animation;
using Mobge.StateMachineAI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.War {
    public class AIAttackRangeTrigger : BaseAIState{
        public AIComponentReference<BaseAIAttack> attack;
        public AIComponentReference<AICharacterDetector> characterDetector;
        [AnimatorTrigger] public int trigger;
        public float updateTargetCooldown = 0.15f;

        private float _nextUpdateTime;
        public override void InitializeComponents(StateAI ai) {
            base.InitializeComponents(ai);
            attack.Init(ai);
            characterDetector.Init(ai);
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            base.OnStateUpdate(animator, stateInfo, layerIndex);
            float time = Time.time;
            if (_nextUpdateTime < time) {
                _nextUpdateTime = time + updateTargetCooldown;
                if (characterDetector.component.ChooseTarget(out IDamagable c)) {
                    var dif = c.GetPosition() - animator.transform.position;
                    float range = attack.component.attackRange;
                    if (dif.sqrMagnitude <= range * range) {
                        animator.SetTrigger(trigger);
                    }
                }
            }
        }
    }
}