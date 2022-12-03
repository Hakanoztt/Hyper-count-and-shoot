using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mobge.StateMachineAI {
    public interface IAITarget {
        Vector3 WorldTarget { get; }
    }
}