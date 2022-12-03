using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public class Joint2DCallbacks : MonoBehaviour {
        public Joint2DListener listener;
        public void OnJointBreak2D(Joint2D joint) {
            listener.OnJointBreak2D(this, joint);
        }
    }
    public interface Joint2DListener {
        void OnJointBreak2D(Joint2DCallbacks  sender, Joint2D joint);
    }
}