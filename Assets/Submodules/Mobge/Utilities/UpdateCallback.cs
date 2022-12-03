using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge
{
    public class UpdateCallback : MonoBehaviour
    {
        public Action<UpdateCallback> onUpdate;
        protected void Update() {
            onUpdate(this);
        }
    }
}