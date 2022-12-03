using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mobge.Animation;
using System;

namespace Mobge.StateMachineAI
{
    

    public abstract class BaseAIState : StateMachineBehaviour, IAnimatorControllerOwner
    {
        //public Map map;
        public StateAI ai { get; private set; }

        RuntimeAnimatorController IAnimatorControllerOwner.GetAnimatorController()
        {
#if UNITY_EDITOR
            return UnityEditor.Animations.AnimatorController.FindStateMachineBehaviourContext(this)[0].animatorController;
#else
            return null;
#endif
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            base.OnStateEnter(animator, stateInfo, layerIndex);
            // Debug.Log("state machine enter: " + this);
            if (ai == null || animator.gameObject != ai.gameObject) {
                ai = animator.GetComponent<StateAI>();
                InitializeComponents(ai);
            }
        }
        public virtual void InitializeComponents(StateAI ai)
        {

        }
    }
}