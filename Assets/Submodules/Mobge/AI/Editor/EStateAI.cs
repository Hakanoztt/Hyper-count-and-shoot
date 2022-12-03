using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.StateMachineAI {
    [CustomEditor(typeof(StateAI))]
    public class EStateAI : Editor {

        public static StateAI LastSelectedAI { get; private set; }
        private void OnEnable() {
            LastSelectedAI = target as StateAI;
        }


        public static bool TryGetAI(Object stateMachineBehaviour, out StateAI ai) {
            if (stateMachineBehaviour is StateMachineBehaviour b) {
                var controller = UnityEditor.Animations.AnimatorController.FindStateMachineBehaviourContext(b)[0].animatorController;
                var testAi = EStateAI.LastSelectedAI;
                if (testAi != null && testAi.animator != null && testAi.animator.runtimeAnimatorController == controller) {
                    ai = testAi;
                    return true;
                }
            }
            ai = null;
            return false;
        }
    }
}