using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.CountAndShoot {
    public class BasicEnemy : PlayerTarget {
        private new void Awake() {
            base.Awake();
        }
        public override void TakeDamage(float damage) {
            base.TakeDamage(damage);
        }
        private new void Update() {
            base.Update();
        }
        private void OnTriggerEnter(Collider other) {
            if (Player.TryGetCharacter(other, out Player p)) {
                p.healthModule.TakeDamage(damage);
                animationModule.Play(animationModule.Punch);
                gameObject.GetComponent<Collider>().enabled = false;

                if (p.healthModule.IsAlive) {
                    SetActive(false);
                }

            }
        }
    }
}