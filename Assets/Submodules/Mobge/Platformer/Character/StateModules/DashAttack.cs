using UnityEngine;
using Mobge.Animation;

namespace Mobge.Platformer.Character{
    public class DashAttack : AAttackModule
    {
        [AnimationSplitterAttribute(constantIndexNames = new string[] {"start dash", "end dash", "end attack"})]
        public AnimationSpliter sequence; 
        public float damage;
        public bool stopOnDamage;
        public float distance;
        public Collider2D trigger;
        public CharacterEffectGroup hitEffect;
        private AnimationSpliter.Updater _updater;
        private Character2D _character;

        public override float DPS => damage / sequence.TotalTime;

        public override float GetDamage(int index) => damage;
        public override void SetDamage(int index, float value) => damage = value;
        public override int DamageCount => 1;
        protected void Awake() {
            trigger.enabled = false;
        }
        void Finish() {
            trigger.enabled = false;
        }
        public override void Interrupted(Character2D character)
        {
            Finish();
        }
        public override bool TryEnable(Character2D character, in CharacterInput.Button button, int actionIndex)
        {
            sequence.Start(0, character.Animation, out _updater);
            _character = character;
            return true;
        }

        public override bool UpdateModule(Character2D character, in CharacterInput.Button button, ref Character2D.State state)
        {
            bool indexChanged;
            state.walkMode = WalkMode.NoAnimation;
            character.Input.MoveInput = new Vector2(0,0);
            if(!sequence.Update(character.Animation, ref _updater, out indexChanged)) {
                Finish();
                return false;
            }
            if(indexChanged) {
                trigger.enabled = _updater.currentIndex == 1;
                if(_updater.currentIndex == 2) {
                    var vel = _character.CurrentVelocity;
                    _character.AddForce(new Vector2(-vel.x, 0) * _character.Mass, ForceMode2D.Impulse);
                }
            }
            if(_updater.currentIndex == 1) {
                var deltaPos = character.Direction > 0 ? this.distance : -this.distance;
                var speed = deltaPos / sequence.divisions[1].duration;
                var cVel = character.CurrentVelocity;
                character.AddForce(new Vector2(speed - cVel.x, 0) * _character.Mass, ForceMode2D.Impulse);
            }
            return true;
        }
        
        protected void OnTriggerEnter2D(Collider2D col) {
            var chr = Character2D.FromCollider(col);
            if(chr) {
                if(_character.IsEnemy(chr)) {
                    DamageData dd = new DamageData(damage, _character.PhysicalCenter);
                    this.hitEffect.ApplyEffect(_character, chr, ref dd);
                    _character.Attack(chr, ref dd);
                    if(stopOnDamage){
                        _updater.FinishCurrentState();
                    }
                }
            }
        }
        #if UNITY_EDITOR
        protected void OnDrawGizmosSelected() {
            var chr = transform.GetComponentInParent<Character2D>();
            var pos = chr.Position;
            var p2 = pos;
            p2.x += distance;
            Gizmos.DrawLine(pos, p2);

        }
        #endif
    }
}