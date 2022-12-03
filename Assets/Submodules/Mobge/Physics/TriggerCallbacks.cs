using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public class TriggerCallbacks : MonoBehaviour
    {
        public TriggerListener listener;
        
        public void OnTriggerEnter(Collider collider) {
            if(listener != null)
                listener.OnTriggerEnter(this, collider);
        }
        public void OnTriggerExit(Collider collider) {
            if (listener != null)
                listener.OnTriggerExit(this, collider);
        }
    }
    public interface TriggerListener {
        void OnTriggerEnter(TriggerCallbacks sender, Collider collider);
        void OnTriggerExit(TriggerCallbacks sender, Collider collider);
    }
}