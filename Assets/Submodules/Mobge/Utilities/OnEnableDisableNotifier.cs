using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    [ExecuteInEditMode]
    public class OnEnableDisableNotifier : MonoBehaviour {
        public Action<OnEnableDisableNotifier, bool> onStateChange;
        protected void OnEnable() {
            if (onStateChange != null) onStateChange(this, true);
        }
        protected void OnDisable() {
            if (onStateChange != null) onStateChange(this, false);
        }
    }
}