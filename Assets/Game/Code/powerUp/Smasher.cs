using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.CountAndShoot {
    public class Smasher : MonoBehaviour {
        private void OnTriggerEnter(Collider other) {
            if (other.CompareTag("PlayerTarget")) {
                var playerTarget = other.GetComponent<PlayerTarget>();
                playerTarget.TakeDamage(10);
            }

            if (other.CompareTag("Ground")) {
                gameObject.SetActive(false);
            }
        }

    }
}
