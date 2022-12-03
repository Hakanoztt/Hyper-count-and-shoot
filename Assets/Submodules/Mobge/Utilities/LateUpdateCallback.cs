using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge
{
    public class LateUpdateCallback : MonoBehaviour
    {
        public Action<LateUpdateCallback> onLateUpdate;
        protected void LateUpdate() {
            onLateUpdate(this);
        }
    }
}