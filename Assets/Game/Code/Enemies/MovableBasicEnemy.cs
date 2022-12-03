using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.CountAndShoot {
    public class MovableBasicEnemy : MovablePlayerTarget {
        public CharacterTrigger characterTrigger;
        private new void Awake() {
            base.Awake();
            characterTrigger.OnPlayerTriggered += OnPlayerTriggered;
        }
        private void OnPlayerTriggered() {
            isMoving = true;
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