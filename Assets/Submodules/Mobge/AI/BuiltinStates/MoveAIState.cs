using Mobge.Animation;
using Mobge.IdleGame;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.StateMachineAI {
    public class MoveAIState : BaseAIState {
        
        public AIComponentReference<IAITarget> target;
        public AIComponentReference<BaseNavigationCharacter> navigationCharacter;

        public float rePathRadius = 0.3f;

        public float followDistance = 0.5f;

        private Vector3 _lastPosition;

        public override void InitializeComponents(StateAI ai) {
            base.InitializeComponents(ai);
            target.Init(ai);
            navigationCharacter.Init(ai);

        }
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            base.OnStateEnter(animator, stateInfo, layerIndex);

            _lastPosition = ai.transform.position;
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            base.OnStateUpdate(animator, stateInfo, layerIndex);
           
            var targetPos = this.target.component.WorldTarget;

            targetPos = Vector3.MoveTowards(targetPos, navigationCharacter.component.transform.position, followDistance);

            var dif = targetPos - _lastPosition;
            if (dif.sqrMagnitude > rePathRadius * rePathRadius) {
                _lastPosition = targetPos;
                navigationCharacter.component.moveModule.Navigate(targetPos);
            }
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            base.OnStateExit(animator, stateInfo, layerIndex);

            navigationCharacter.component.moveModule.StopNavigating();

        }

        public enum ChooseMethod {
            Nearest,
            Farthest,
            Random,
        }
    }
}