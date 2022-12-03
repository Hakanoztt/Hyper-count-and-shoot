using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public class TriggerStayCallback : MonoBehaviour {
        public TriggerStayListener listener;
        private void OnTriggerStay(Collider other) {
            listener.OnTriggerStay(this, other);
        }
    }
    public interface TriggerStayListener {
        void OnTriggerStay(TriggerStayCallback collider2DCallbacks, Collider collision);
    }
}
