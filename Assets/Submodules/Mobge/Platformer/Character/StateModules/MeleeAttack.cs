using System;
using UnityEngine;
using Mobge.Animation;

namespace Mobge.Platformer.Character{
    public class MeleeAttack : AAttackModule
    {
        public float damage = 4;
        public AttackSetup[] attacks;
        public bool enableWalkAnimation;
        public float moveInputDecay = 0.1f;
        public float gravityScale = 1f;
        public float startSpeedMultiplayer = 1;
        public float restoreSpeedRate = 0;
        public float comboResetTime = 0.3f;
        private Vector3 _startSpeed;
        private int _currentAttack;
        private Character2D _character;
        private Vector2 _moveInput;
        private AnimationSpliter.Updater _updater;
        private int _pressCount;
        private float _lastAttackEnd;

        protected void Awake() {
            for(int i = 0; i < attacks.Length; i++) {
                attacks[i].trigger.enabled = false;
            }
        }
        public override float DPS {
            get {
                float totalTime = 0;
                float totalDamage = 0;
                for(int i = 0; i < attacks.Length; i++){
                    var a = attacks[i];
                    totalTime += a.times.TotalTime;
                    totalDamage += this.damage;
                }
                return totalDamage / totalTime;
            }
        }

        public override float GetDamage(int index) => damage;
        public override void SetDamage(int index, float value) => damage = value;
        public override int DamageCount => 1;

        public override void Interrupted(Character2D character)
        {
            Finish();
        }
        private bool TryContinue(Character2D character) {
            _lastAttackEnd = Time.fixedTime;
            _currentAttack++;
            if(_currentAttack >= attacks.Length){
                _currentAttack = 0;
                return false;
            }
            if(_pressCount > 0){
                _pressCount--;
                StartNextAttack(character);
                return true;
            }
            return false;
        }
        private void StartNextAttack(Character2D character) {
            attacks[_currentAttack].times.Start(enableWalkAnimation ? 1 : 0, character.Animation, out _updater);
            _startSpeed = character.CurrentVelocity;
            if(startSpeedMultiplayer != 1f) {
                character.CurrentVelocity = character.CurrentVelocity * startSpeedMultiplayer;
            }
            _moveInput = character.Input.MoveInput * startSpeedMultiplayer;
            _character = character;
        }
        public override bool TryEnable(Character2D character, in CharacterInput.Button button, int actionIndex)
        {
            if(_lastAttackEnd + comboResetTime < Time.fixedTime) {
                _currentAttack = 0;
            }
            _pressCount = -1;
            StartNextAttack(character);
            return true;
        }

        public override bool UpdateModule(Character2D character, in CharacterInput.Button button, ref Character2D.State state)
        {
            if(button.Value && !button.PreviousValue) {
                _pressCount++;
            }
            bool indexChanged;
            if(!enableWalkAnimation) {
                state.walkMode =  WalkMode.NoAnimation;
            }
            else {
                state.walkMode =  WalkMode.Normal;
            }
            if(moveInputDecay != 0) {
                _moveInput *= (1 - moveInputDecay);
                character.Input.MoveInput = _moveInput;
                character.Input.Jump.Value = false;
            }
            //character.Input.MoveInput= new Vector2(1,0);
            if(!attacks[_currentAttack].times.Update(character.Animation, ref _updater, out indexChanged)) {
                if(restoreSpeedRate != 0) {
                    character.CurrentVelocity += _startSpeed * restoreSpeedRate;
                }
                Finish();
                return TryContinue(character);
            }
            if(gravityScale != 1) {
                character.AddForce(((gravityScale - character.GravityScale) * character.Mass) * Physics2D.gravity);
            }
            if(indexChanged) {
                character.WakeRigidbody();
                attacks[_currentAttack].trigger.enabled = _updater.currentIndex == 1;
            }
            return true;
        }
        protected void OnTriggerEnter2D(Collider2D col) {
            var chr = Character2D.FromCollider(col);
            if(chr) {
                if(_character.IsEnemy(chr)) {
                    DamageData dd = new DamageData(damage, _character.PhysicalCenter);
                    var a = attacks[_currentAttack];
                    a.characterEffect.ApplyEffect(_character, chr, ref dd);
                    _character.Attack(chr, ref dd);
                }
            }
        }
        private void Finish() {
            attacks[_currentAttack].trigger.enabled = false;
        }

        [Serializable]
        public class AttackSetup {
            public Collider2D trigger;
            [AnimationSplitterAttribute(constantIndexNames = new string[] {"start damage", "end damage", "end attack"})]
            public AnimationSpliter times;
            public CharacterEffectGroup characterEffect;
        }
    }
}