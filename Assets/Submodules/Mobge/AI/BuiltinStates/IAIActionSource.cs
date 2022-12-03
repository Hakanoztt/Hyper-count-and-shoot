using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.StateMachineAI {
    public interface IAIActionSource {
        public int ActionCount { get; }
        public string GetActionName(int index);

        public bool StartAction(int index);
        public bool CheckAction(int index);
    }
}