using Mobge.Platformer.Character;
using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.War {
    public class Building : MonoBehaviour, IDamagable {


        public Health health = new Health(1000);

        public HealthBarModule healthBarModule;

        public TeamMaterialModule teamMaterialModule;

        public System.Action<Building> onDeath;

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

        public Collider Collider { get; private set; }

        public bool IsAlive => health.Alive;


        public WarManagerComponent.Data WarManager{ get; private set; }

        protected void Awake() {
            gameObject.tag = Character.c_tag;
            Collider = GetComponent<Collider>();
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

        public void Revive(WarManagerComponent.Data warManager) {

            WarManager = warManager;
            RefreshTeamColors();
            health.Current = health.Max;
            SetAlive(true);
        }
        private void HandleDeath() {
            SetAlive(false);
        }
        private void SetAlive(bool alive) {
            if (alive) {
                healthBarModule.SetEnabled(WarManager.Player, this.health);
            }
            else {
                healthBarModule.SetDisabled();
            }
        }

        private void RefreshTeamColors() {
            if (WarManager != null && _team >= 0) {
                teamMaterialModule.ApplyMaterial(WarManager.teamMaterials.GetTeamMaterial(_team));
            }
        }


#if UNITY_EDITOR    
        private void OnDrawGizmosSelected() {
            healthBarModule.OnDrawGizmos();
        }
#endif
    }
}