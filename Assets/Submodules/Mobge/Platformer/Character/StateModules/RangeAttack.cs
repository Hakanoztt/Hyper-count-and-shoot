using UnityEngine;
using Mobge.Animation;

namespace Mobge.Platformer.Character {
    public class RangeAttack : AAttackModule {
        [InterfaceConstraint(typeof(Projectile), ReusableReference.ReferenceFieldName)]
        public ReusableReference projectile;
        public Vector2 shootOffset;
        [AnimationSplitter(constantIndexNames = new string[] { "shot time", "end time"})]
        public AnimationSpliter times;
        public float damage;
        public ProjectileShootData shootData; 
        public float gravity = -10;
        public CharacterEffectGroup attackEffect;
        public override float DPS => damage / times.TotalTime;
        public override int DamageCount => 1;
        private Character2D _target;
        private AnimationSpliter.Updater _updater;

        public override float GetDamage(int index)
        {
            return damage;
        }

        public override void Interrupted(Character2D character)
        {
            Finish();
        }

        public override void SetDamage(int index, float value)
        {
            damage = value;
        }

        public override bool TryEnable(Character2D character, in CharacterInput.Button button, int actionIndex)
        {
            Vector3 velocity;
            _target = character.Input.Target;
            if(TryCalculateVelocity(character, _target, out velocity)) {
                times.Start(0, character.Animation, out _updater);
                return true;
            }
            return false;
        }
        private void ShootProjectile(Character2D character, Vector2 velocity) {
            var projectile = (Projectile)this.projectile.SpawnItem(CurrentShootPosition);
            projectile.Initialize(character, velocity, gravity, damage);
            projectile.damageEffect = attackEffect;
        }
        public Vector3 CurrentShootPosition {
            get {
                return transform.TransformPoint(shootOffset);
            }
        }
        private void Finish() {
            _target = null;
        }
        bool TryCalculateVelocity(Character2D character, Character2D target, out Vector3 velocity) {
            var t = target;
            Vector3 rt;
            if(!t) {
                rt = character.Direction < 0 ? new Vector3(0,-1,0) : new Vector3(0,1,0);
            }
            else {
                rt = t.PhysicalCenter - CurrentShootPosition;
            }
            bool facingLeft = character.Direction < 0;
            var r = shootData.TryCalculatingVelocity(out velocity, gravity, rt, facingLeft);
            //Debug.Log((t.Position - from) + " " + r);
            return r;
        }
        public override bool UpdateModule(Character2D character, in CharacterInput.Button button, ref Character2D.State state)
        {
            state.walkMode = WalkMode.NoAnimation;
            character.Input.MoveInput= Vector2.zero;
            bool indexChanged;
            if(!times.Update(character.Animation, ref _updater, out indexChanged)) {
                Finish();
                return false;
            }
            if(indexChanged) {
                if(_updater.currentIndex == 1) {
                    Vector3 velocity;
                    if(!TryCalculateVelocity(character, _target, out velocity)) {
                        Finish();
                        return false;
                    }
                    ShootProjectile(character,velocity);
                }
            }
            return true;
        }
    }
}