using Mobge.StateMachineAI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mobge.War {
    public class AIAttackstate : Mobge.StateMachineAI.BaseAIState {
        
        public AIComponentReference<BaseAIAttack> attack;

        public override void InitializeComponents(StateAI ai) {
            base.InitializeComponents(ai);
            attack.Init(ai);
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            base.OnStateEnter(animator, stateInfo, layerIndex);
            
        }
    }
}