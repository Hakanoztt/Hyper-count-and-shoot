using System;
using Mobge.Platformer.Character;
using UnityEngine;

namespace Mobge.Platformer {
    public class Projectile : AReusableItem {
        public float life = 5;
        public float afterHitLife = 3;
        private float _birth;
        protected Character2D _character;
        protected float _damage;
        [SerializeField]
        protected Rigidbody2D _rb;
        private Vector3 _scale;
        [NonSerialized]
        public Mobge.Platformer.Character.CharacterEffectGroup damageEffect;
        protected void Awake() {
            gameObject.SetActive(false);
            _scale = transform.localScale;
        }
        public void Initialize(Character2D chr, Vector3 velocity, float gravity, float damage) {
            _character = chr;
            _damage = damage;
            _rb.velocity = velocity;
            if (Physics2D.gravity.y != 0) {
                _rb.gravityScale = gravity / Physics2D.gravity.y;
            }
        }
        public Vector3 Velocity {
            get => _rb.velocity;
            //set => rb.velocity = value;
        }
        protected override void OnPlay()
        {
            gameObject.SetActive(true);
            enabled = true;
            Age = 0;
            _rb.simulated = true;
            transform.localScale = _scale;
        }
        protected void FixedUpdate() {
            var r = _rb.rotation;
            var vel = _rb.velocity;
            float target = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
            var dif = target - r;
            _rb.angularVelocity = dif / Time.fixedDeltaTime;
        }
        protected float Age {
            get => Time.fixedTime - _birth;
            set => _birth = Time.fixedTime - value;
        }
        protected void OnTriggerEnter2D(Collider2D col) {
            if(col.isTrigger) return;
            bool shouldAttach = false;
            var chr = Character2D.FromCollider(col);
            if(chr) {
                if(chr.IsEnemy(_character)) {
                    DamageData dd = new DamageData(_damage, _rb.position);
                    if(damageEffect.effects != null) {
                        damageEffect.ApplyEffect(_character, chr, ref dd);
                    }
                    _character.Attack(chr, ref dd);
                    shouldAttach = true;
                }
            }
            else {
                shouldAttach = true;
            }
            if(shouldAttach) {
                transform.SetParent(col.transform);
                enabled = false;
                Age = 0;
                _rb.simulated = false;
            }
        }
        public override void Stop()
        {
            gameObject.SetActive(false);
            _character = null;
            damageEffect = new CharacterEffectGroup();
        }
        public override void StopImmediately() {
            Stop();
        }
        public override bool IsActive {
            get {
                if(_rb.simulated) {
                    return Age <= life;
                }
                return Age <= afterHitLife;
            }
        }
    }
}