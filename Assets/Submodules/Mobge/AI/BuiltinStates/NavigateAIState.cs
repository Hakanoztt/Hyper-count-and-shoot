using Mobge.Core.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mobge.StateMachineAI {

    public class NavigateAIState : BaseAIState {

        public AIComponentReference<Transform> target;
        //public AIComponentReference<TriggerTracker<Brain>> brainSource;
        public override void InitializeComponents(StateAI ai) {
            base.InitializeComponents(ai);
            target.Init(ai);
            ai.gameObject.GetComponent<IComponentExtension>();
        }

    }


    public class Brain : MonoBehaviour {

    }
}