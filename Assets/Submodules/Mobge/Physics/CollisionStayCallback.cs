using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public class CollisionStayCallback : MonoBehaviour {
        public CollisionStayListerner listener;
        private void OnCollisionStay(Collision collision) {
            listener.OnCollisionStay(this, collision);
        }
    }
    public interface CollisionStayListerner {
        void OnCollisionStay(CollisionStayCallback collider2DCallbacks, Collision collision);
    }
}
