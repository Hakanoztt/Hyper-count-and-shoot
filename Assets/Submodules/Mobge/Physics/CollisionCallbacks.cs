using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge
{
    public class CollisionCallbacks : MonoBehaviour
    {
        public CollisionListerner listener;
        private void OnCollisionEnter(Collision collision) {
            listener.OnCollisionEnter(this, collision);

        }
        private void OnCollisionExit(Collision collision) {
            listener.OnCollisionExit(this, collision);
        }
    }
    public interface CollisionListerner
    {
        void OnCollisionEnter(CollisionCallbacks collider2DCallbacks, Collision collision);
        void OnCollisionExit(CollisionCallbacks collider2DCallbacks, Collision collision);
    }
}