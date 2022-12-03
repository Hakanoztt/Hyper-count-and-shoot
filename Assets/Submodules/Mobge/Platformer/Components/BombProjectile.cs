using Mobge.Platformer.Character;
using UnityEngine;

namespace Mobge.Platformer {
    public class BombProjectile : Projectile
    {
        public float effectRadius;
        public Mode mode;
        public bool scaleEffectWithRadius;
        //[InterfaceConstraint(typeof(ParticleEffect), nameof(ReusableReference.ReferenceFieldName))]
        public ReusableReference effect;
        private bool timer;
        protected override void OnPlay() {
            base.OnPlay();
        }
        public void OnEnable() {
        }
        protected new void OnTriggerEnter2D(Collider2D col) {
            if(col.isTrigger) return;
            if(!_rb.simulated) return;
            var chr = Character2D.FromCollider(col);
            bool shouldExplode;
            if(chr) {
                switch(mode) {
                    case Mode.ExplodeOnCharacter:
                    shouldExplode = chr.Alive;
                    break;
                    case Mode.ExplodeOnEnemy:
                    shouldExplode = chr.IsEnemy(_character);
                    break;
                    default:
                    shouldExplode = false;
                    break;
                }
            }
            else {
                shouldExplode = true;
            }
            if(shouldExplode) {
                _rb.simulated = false;
                if(afterHitLife == 0) {
                    Explode();
                }
                else{
                    Age = 0;
                }
            }
        }
        protected void Explode() {
            var pos = transform.position;
            var mask = Physics2D.GetLayerCollisionMask(this.gameObject.layer);
            var l = Collider2dList.OverlapCircle(pos, this.effectRadius, mask);
            for(int i = 0; i < l.Count; i++) {
                if(l[i].isTrigger) {
                    continue;
                }
                var chr = Character2D.FromCollider(l[i]);
                if(chr){
                    if(_character.IsEnemy(chr)) {
                        DamageData dd = new DamageData(_damage, pos);
                        damageEffect.ApplyEffect(_character, chr, ref dd);
                        _character.Attack(chr, ref dd);
                    }
                }
            }
            var e = effect.SpawnItem(pos);
            if(scaleEffectWithRadius && e != null) {
                e.transform.localScale = new Vector3(effectRadius,effectRadius,effectRadius);
            }
            Stop();
        }
        protected new void FixedUpdate() {
            if(IsActive) {
                Explode();
            }
        }
        public enum Mode {
            DontExplodeOnCharacter = 0,
            ExplodeOnCharacter = 1,
            ExplodeOnEnemy = 2,
        }
    }
}