using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.CountAndShoot {
    public class Bullet : MonoBehaviour, ITarget {
        public float bombSpeed;
        public float damage;
        public Health health;
        public bool isBulletDead = false;
        public void Update() {
            transform.position += -transform.forward * Time.deltaTime * bombSpeed;
        }
        public void TakeDamage(float damage) {
            health.TakeDamage(damage);
            if (!health.isAlive) {
                Explosion();
            }
        }
        public void Explosion() {
            gameObject.SetActive(false);
            isBulletDead = true;
            // effect.
        }
        private void OnTriggerEnter(Collider other) {
            if (Player.TryGetCharacter(other, out Player p)) {
                p.healthModule.TakeDamage(damage);
                Explosion();
            }
        }
    }
}