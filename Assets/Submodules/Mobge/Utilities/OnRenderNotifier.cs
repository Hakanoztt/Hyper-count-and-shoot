using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public class OnRenderNotifier : MonoBehaviour {
        public Listener listener;
        private void OnWillRenderObject() {
            var cam = Camera.current;
            if(cam == null) {
                cam = Camera.main;
            }
            listener.OnWillRenderObject(cam);
        }
        public interface Listener {
            void OnWillRenderObject(Camera cam);
        }
    }
}