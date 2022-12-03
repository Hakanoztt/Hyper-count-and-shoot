using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mobge.HyperCasualSetup;

namespace Mobge.CountAndShoot {
    public class Ball : MonoBehaviour {
        public Rigidbody rb;
        public Collider col;
        public float damage;
        private void Start() {
            rb = GetComponent<Rigidbody>();
        }

            private void OnTriggerEnter(Collider other) {
            if (other.CompareTag("PlayerTarget")) {
                gameObject.SetActive(false);
                var enemy = other.GetComponent<ITarget>();
                enemy.TakeDamage(damage);

            }
            if (other.CompareTag("PowerUp")) {
                gameObject.SetActive(false);
                var powerUp = other.GetComponent<IPowerUp>();
                powerUp.Activate();
            }
            if (other.CompareTag("Ground")) {
                col.isTrigger = true;
                rb.useGravity = false;
                rb.velocity = Vector3.zero;

            }
           
        }
    }
}


