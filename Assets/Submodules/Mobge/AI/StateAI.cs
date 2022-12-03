using Mobge.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.StateMachineAI
{
    [RequireComponent(typeof(Animator))]
    public class StateAI : MonoBehaviour
    {
        //public Map map;
        [InterfaceConstraint(typeof(Component))] public List<Component> componentVariables;
        public List<float> floatVariables;
        public Animator animator;
        public int layer;
        [AnimatorTrigger] public int resetTrigger;

        private bool _enabled;

        public bool Enabled {
            get {
                return _enabled;
            }
            set {
                if (value != _enabled) {
                    _enabled = value;
                    if (_enabled) {
                        animator.SetTrigger(resetTrigger);
                    }
                    SignalComponents(_enabled);
                }
            }
        }
        private void SignalComponents(bool enabled) {
            for(int i = 0; i < componentVariables.Count; i++) {
                if(componentVariables[i] is IAIComponent c) {
                    c.OnAIEnable(enabled);
                }
            }
        }

        public T GetVariable<T>(int index) 
        {
            return (T)(object)componentVariables[index];
        }

        public void SetVariable<T>(T var, int index) where T : Component
        {
            if(componentVariables.Count > index){
                componentVariables[index] = var;

            }
        }

    }

}