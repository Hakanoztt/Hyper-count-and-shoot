using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge
{
    public class Collision2DCallbacks : MonoBehaviour
    {
        public Collision2DListerner listener;
        public void OnCollisionEnter2D(Collision2D collision) {
            listener.OnCollisionEnter2D(this, collision);
        }
        public void OnCollisionExit2D(Collision2D collision) {
            listener.OnCollisionExit2D(this, collision);
        }
    }
    public interface Collision2DListerner
    {
        void OnCollisionEnter2D(Collision2DCallbacks collider2DCallbacks, Collision2D collision);
        void OnCollisionExit2D(Collision2DCallbacks collider2DCallbacks, Collision2D collision);
    }
}