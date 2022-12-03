using Mobge.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.StateMachineAI {
    public class AIActionState : BaseAIState {
        public AIComponentReference<IAIActionSource> action;
        [HideInInspector] public int actionIndex;
        [AnimatorBoolParameter] public int conditionParameter;
        public bool isReversed;

        void ApplyAction(Animator animator) {
            bool condition = action.component.CheckAction(actionIndex);
            animator.SetBool(conditionParameter, condition != isReversed);
        }

        public override void InitializeComponents(StateAI ai) {
            base.InitializeComponents(ai);
            action.Init(ai);
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            base.OnStateEnter(animator, stateInfo, layerIndex);
            action.component.StartAction(actionIndex);

        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            base.OnStateUpdate(animator, stateInfo, layerIndex);
            ApplyAction(animator);
        }

    }
}