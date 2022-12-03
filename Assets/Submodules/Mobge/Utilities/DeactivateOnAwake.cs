using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HandStrike {
    public class DeactivateOnAwake : MonoBehaviour {
        protected void Awake() {
            gameObject.SetActive(false);
        }
    }
}