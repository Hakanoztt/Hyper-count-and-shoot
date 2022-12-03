using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.CountAndShoot {
    public class PowerUp2 : MonoBehaviour, IPowerUp {

        bool isTriggered = false;
        public GameObject target;
        public Rigidbody targetRb;

        private void Start() {
            targetRb = target.GetComponent<Rigidbody>();
        }
        void Update() {
            Crash();
        }
        public void Activate() {
            isTriggered = true;
        }
        void Crash() {
            if (isTriggered) {
                target.gameObject.SetActive(true);
            }
        }
    }
}
