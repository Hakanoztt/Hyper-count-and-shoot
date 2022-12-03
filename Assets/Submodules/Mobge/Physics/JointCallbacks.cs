using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public class JointCallbacks : MonoBehaviour {
        public JointListener listener;

        public void OnJointBreak(float breakForce) {
            listener.OnJointBreak(this, breakForce);
        }
    }
    public interface JointListener {
        void OnJointBreak(JointCallbacks sender, float breakForce);
    }
}
