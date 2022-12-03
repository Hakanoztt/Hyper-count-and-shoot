using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    [ExecuteInEditMode]
    public class OnDestroyNotifier : MonoBehaviour {
        public Action<OnDestroyNotifier> onDestroy; 
        private void OnDestroy() {
            if (onDestroy != null) {
                onDestroy(this);
            }
        }
    }
}