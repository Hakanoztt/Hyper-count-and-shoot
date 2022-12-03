using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public class Trigger2DCallbacks : MonoBehaviour
    {
        public Trigger2DListener listener;
        
        public void OnTriggerEnter2D(Collider2D collider) {
            listener.OnTriggerEnter2D(this, collider);
        }
        public void OnTriggerExit2D(Collider2D collider) {
            listener.OnTriggerExit2D(this, collider);
        }
    }
    public interface Trigger2DListener {
        
        void OnTriggerEnter2D(Trigger2DCallbacks sender, Collider2D collider);
        void OnTriggerExit2D(Trigger2DCallbacks sender, Collider2D collider) ;
    }
}