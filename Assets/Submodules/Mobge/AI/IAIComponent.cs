using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.StateMachineAI {
    public interface IAIComponent {
        void OnAIEnable(bool enabled);
    }
}