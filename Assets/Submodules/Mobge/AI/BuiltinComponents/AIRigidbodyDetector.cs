using Mobge.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.StateMachineAI {
    public class AIRigidbodyDetector : AIObjectDetector<Rigidbody> {
        public override bool IsValid(Rigidbody t) {
            return true;
        }

        public override bool TryGetObject(Collider tr, out Rigidbody t) {
            t = tr.attachedRigidbody;
            return t != null;
        }

    }
}
