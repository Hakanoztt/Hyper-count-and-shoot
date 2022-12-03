using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mobge.Core.Components;
using Mobge.Core;
using Mobge.HyperCasualSetup;
using Mobge.BasicRunner;
using Mobge.HyperCasualSetup.RoadGenerator;
using Mobge.Animation;

namespace Mobge.CountAndShoot {

    [Serializable]
    public struct Health {
        public float CurrentHealth => _currentHealth;

        private float _currentHealth;
        public float maxHealth;

        public TMP_Text healthText;
        public bool isAlive => _currentHealth > 0;
        public void Init() {
            _currentHealth = maxHealth;
        }
        public void TakeDamage(float damage) {
            if (isAlive) {
                _currentHealth -= damage;
            }
        }
        public void UpdateHealth(float currentHealth) {
            if (healthText != null) {
                healthText.text = currentHealth.ToString();
            }
        }
    }
    public interface ITarget {
        public void TakeDamage(float damage);
    }
    public abstract class PlayerTarget : MonoBehaviour, ITarget, IComponentExtension {

        public Health health;
        public ParticleSystem deathEffect;
        private BaseLevelPlayer _player;
        public Player player;
        public AnimationModule animationModule;
        public float damage;
        public float setActiveDelayf;
        private bool isDead;
        public ParticleEffect hitEffect;
      
        public BaseLevelPlayer LevelPlayer => _player;
        public virtual void TakeDamage(float damage) {
            health.TakeDamage(damage);
            if(animationModule.Animator != null)
            animationModule.Play(animationModule.DamageAnim);
            hitEffect.Play();

            if (!health.isAlive) {
                gameObject.GetComponent<Collider>().enabled = false;
                Death();
            }
            health.UpdateHealth(health.CurrentHealth);
        }
        public virtual void Awake() {
            health.Init();
            health.UpdateHealth(health.CurrentHealth);
        }
        public virtual void Death() {
            if (isDead) return;
            isDead = true;
            
            if (!health.isAlive) {
                if (animationModule.Animator != null) {
                    animationModule.Play(animationModule.Death);
                }
                health.healthText.gameObject.SetActive(false);
                SetActive(false);
                if (deathEffect != null) {
                    deathEffect.Play();
                }
            }
        }
        public virtual void Update() {
              
        }
        public virtual void SetActive(bool input) {
            _player.RoutineManager.DoAction((complete, data) => {
                gameObject.SetActive(input);

            }, setActiveDelayf);
         
        }

        void IComponentExtension.Start(in BaseComponent.InitArgs initData) {
            _player = (BaseLevelPlayer)initData.player;
        }

        [Serializable]
        public class AnimationModule {
            public Animator Animator;

            [AnimatorState] public int IdleAnim;
            [AnimatorState] public int runAnim;
            [AnimatorState] public int DamageAnim;
            [AnimatorState] public int Death;
            [AnimatorState] public int Punch;
            public void Play(int anim) {
                Animator.CrossFade(anim, 0.1f);
            }
        }
    }
    public abstract class MovablePlayerTarget : PlayerTarget {
        public float speed;
        public bool isMoving = false;   
        private RunnerController target;
        public override void Update() {
            if (target != null)
                Move();
            else {
                if (LevelPlayer != null) {
                    target = GameComponentBasicRunner.GetForPlayer(LevelPlayer).Character;
                }
            }
        }
        void Move() {
            if (isMoving) {
         //       animationModule.Play(animationModule.runAnim);
                var targetPos = target.transform.position;
                targetPos.y = transform.position.y;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            }
        }
    }

}