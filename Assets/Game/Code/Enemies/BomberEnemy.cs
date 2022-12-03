using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.CountAndShoot {
    public class BomberEnemy : PlayerTarget {
        public CharacterTrigger characterTrigger;
        public ParticleSystem takeDamageEffect;
        public Transform bulletTransform;
        public Bullet bullet;
        Vector3 _targetPos;
        bool isBombDead = false;

        Player player;
        private new void Awake() {
            base.Awake();
            _targetPos = transform.position; //.
            characterTrigger.OnPlayerTriggered += OnPlayerTriggered;
        }
        private void OnPlayerTriggered() {
            InstantiateBomb();
        }

        public override void TakeDamage(float damage) {
            base.TakeDamage(damage);
            if (takeDamageEffect != null) {
                takeDamageEffect.Play();
            }
        }
        void InstantiateBomb() {
            if (player == null && LevelPlayer != null) {
                player = GameComponent.GetForPlayer(LevelPlayer).Player;
            } else return;

            var bomb = Instantiate(bullet, _targetPos, Quaternion.identity);
            bomb.transform.position = bulletTransform.position;
            bomb.transform.forward = bulletTransform.position - player.transform.position;
        }
        private void OnTriggerEnter(Collider other) {
            if (Player.TryGetCharacter(other, out Player p)) {
                p.healthModule.TakeDamage(damage);
                if (p.healthModule.IsAlive) {
                    SetActive(false);
                }
            }
        }
    }

    public abstract class Sekil {
        public abstract double Cevre();
        public abstract double Alan();
    }

    public class Kare : Sekil {

        public double kenar;

        public void SetKenar(double kenar) {
            this.kenar = kenar;

        }
        public override double Alan() {
            return kenar * kenar;
        }

        public override double Cevre() {
            return kenar * 4;
        }

        
    }
}