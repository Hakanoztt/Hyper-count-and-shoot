using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.StateMachineAI {
    [System.Serializable]
    public struct AIComponentReference<T> {
        [AIComponentIndex] public int index;
        [NonSerialized] public T component;
        public T Init(StateAI ai) {
            component = ai.GetVariable<T>(index);
            return component;
        }
#if UNITY_EDITOR
        public bool editor_TryGetComponent(StateAI ai, out T t) {
            if(index>=0 && ai.componentVariables!=null && index< ai.componentVariables.Count) {
                if(ai.componentVariables[index] is T tt) {
                    t = tt;
                    return true;
                }
            }
            t = default;
            return false;
        }
#endif
    }

    public class AIComponentIndexAttribute : PropertyAttribute{

    }
}