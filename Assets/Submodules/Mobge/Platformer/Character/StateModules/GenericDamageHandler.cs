using UnityEngine;
using Mobge.Animation;

namespace Mobge.Platformer.Character {
    public class GenericDamageHandler : DamageHandlerState
    {
        [InterfaceConstraint(typeof(AReusableItem), ReusableReference.ReferenceFieldName)]
        public ReusableReference damageEffect;
        [AnimationAttribute]
        public int takeDamageAnimation;
        public float takeDamageAnimTime = 0.25f;
        [AnimationAttribute]
        public int dieAnimation;
        public float knockbackVelocity;
        private Mobge.Animation.AnimationState _animState;
        public override bool HandleDamage(Character2D character, ref Health health, ref Poise poise, in DamageData damage)
        {
            if(health.Full) {
                poise.Reset();
            }
            if(health.Alive) {
                health.Current -= damage.damage;
                damageEffect.SpawnItem(character.PhysicalCenter);
                if(health.Alive) {
                    if(poise.DecreasePoise(damage.damage * damage.poiseMultiplayer)) {
                        _animState = character.Animation.PlayAnimation(takeDamageAnimation, false);
                        _animState.Speed = _animState.Duration / takeDamageAnimTime;
                        Knockback(character, damage.knockbackMultiplayer, damage.damagePosition);
                        return true;
                    }
                }
                else {
                    _animState = character.Animation.PlayAnimation(dieAnimation, false);
                    return true;
                }
            }
            return false;
        }
        private void Knockback(Character2D owner, float forceMultiplayer, Vector3 damagePosition) {
            if(knockbackVelocity == 0) return;
            owner.JumpStart();
            var f = knockbackVelocity * forceMultiplayer;
            if(damagePosition.x - owner.Position.x < 0){
                f = -f;
            }
            owner.CurrentVelocity = new UnityEngine.Vector3(f, 0, 0);
        }

        public override void Interrupted(Character2D character)
        {
            
        }

        public override bool UpdateModule(Character2D character, in CharacterInput.Button button, ref Character2D.State state)
        {
            state.walkMode = WalkMode.NoMoveOrAnimate;
            bool b = _animState != null && character.Animation.IsPlaying(_animState);
            if(!b) {
                _animState = null;
                if(!character.Alive) {
                    character.enabled = false;
                }
                else {
                    return false;
                }
            }
            return true;
        }
    }
}