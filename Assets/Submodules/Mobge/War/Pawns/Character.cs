using Mobge.Animation;
using Mobge.IdleGame;
using Mobge.Platformer.Character;
using Mobge.StateMachineAI;
using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Mobge.War {
    public class Character : MonoBehaviour, IDamagable, IAnimatorOwner {
        public const string c_tag = "Damagable";
        [OwnComponent(true)] public StateAI ai;
        public BaseNavigationCharacter navigationModule;
        [SerializeField] private Health health = new Health(100f);
        [AnimatorState] public int deathAnimation;
        public HealthBarModule healthBarModule;
        public TeamMaterialModule teamMaterialModule;

        public Action<Character> onDeath;
        public Action<Character> handleDispose;


        private int _team = -1;

        public int Team {
            get => _team;
            set {
                if (_team != value) {
                    _team = value;
                    RefreshTeamColors();
                }
            }
        }


        public WarManagerComponent.Data WarManager { get; private set; }
        public bool IsAlive => health.Alive;

        public Collider Collider { get; private set; }

        public Animator GetAnimator() {
            return navigationModule != null ? navigationModule.animationModule.animator : null;
        }
        protected void Awake() {
            gameObject.tag = c_tag;
            Collider = GetComponent<Collider>();
            Team = 0;
        }
        protected void Start() {
            Revive(WarManagerComponent.Get(this.GetLevelPlayer()));

        }
        public void Teleport(Pose pose) {
            bool en = navigationModule.NavigationEnabled;
            navigationModule.NavigationEnabled = false;
            transform.localPosition = pose.position;
            transform.localRotation = pose.rotation;
            navigationModule.NavigationEnabled = en;
        }
        public void Revive(WarManagerComponent.Data fightManager) {
            WarManager = fightManager;
            RefreshTeamColors();
            health.Current = health.Max;
            SetAlive(true);
        }

        private void RefreshTeamColors() {
            if (WarManager != null && _team >= 0) {
                teamMaterialModule.ApplyMaterial(WarManager.teamMaterials.GetTeamMaterial(_team));
            }
        }
        private void SetAlive(bool alive) {
            if (ai != null) {
                ai.Enabled = alive;
                ai.animator.enabled = alive;
            }
            if (alive) {
                healthBarModule.SetEnabled(WarManager.Player, this.health);
            }
            else {
                healthBarModule.SetDisabled();
            }
            navigationModule.animationModule.AutoAnimationEnabled = alive;
        }

        private void HandleDeath() {
            SetAlive(false);
            navigationModule.animationModule.Play(deathAnimation, 0, 0f, out float duration);
            this.WarManager.Player.RoutineManager.DoAction(OnDeathFinish, duration);
        }

        private void OnDeathFinish(bool complete, object data) {
            if (handleDispose != null) {
                handleDispose(this);
                handleDispose = null;
            }
        }

        public bool TakeDamage(in DamageData data) {
            if (health.Alive) {
                health.Current -= data.damage;
                healthBarModule.UpdateHealth(this.health);
                if (!health.Alive) {
                    HandleDeath();
                    if (onDeath != null) {
                        onDeath(this);
                    }
                }
                return true;
            }
            return false;
        }
#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            healthBarModule.OnDrawGizmos();
        }
#endif
    }
}