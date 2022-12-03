using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mobge.Animation;

namespace Mobge.Platformer.Character {
    [CreateAssetMenu(menuName = "Mobge/Platformer/Collectable/Decoy")]
    public class DecoyModuleData : StateModuleData
    {
        public float duration = 5f;
        public float defansMultiplayer = 1f;
        public float attackMultiplayer = 1f;
        public int aiControlModuleIndex;
        [InterfaceConstraint(typeof(ParticleEffect), ReusableReference.ReferenceFieldName)]
        public ReusableReference _spawnEffect;
        public Vector2 spawnVelocity;
        public AnimationSplitDurations castDurations;

        protected override IStateModule GetModule(Character2D character, CharacterMappings mappings)
        {
            var module = CreateInstance<Module>();
            module.mappings = mappings;
            module.data = this;
            return module;
        }

        public class Module : ScriptableObject, IStateModule
        {
            public CharacterMappings mappings;
            public DecoyModuleData data;
            private AnimationSpliter.Updater _updater;
            private Character2D _decoy;
            private float _spawnTime = float.NegativeInfinity;
            public int ActionCount => 1;
            public void Interrupted(Character2D character)
            {
                
            }
            
            private bool Expired {
                get => _spawnTime + data.duration < Time.fixedTime;
            }
            public bool TryEnable(Character2D character, in CharacterInput.Button button, int actionIndex)
            {
                //Debug.Log("neicim decoy enabled : " + !Expired);
                if(!Expired) {
                    return false;
                }
                EnsureInit(character);
                var splitter = mappings.GetAnimation(this, data.castDurations, out int track);
                splitter.Start(track, character.Animation, out _updater);
                return true;
            }
            public bool UpdateModule(Character2D character, in CharacterInput.Button button, ref Character2D.State state)
            {
                bool indexChanged;
                var splitter = mappings.GetAnimation(this, data.castDurations, out int track);
                if(!splitter.Update(character.Animation, ref _updater, out indexChanged)) {
                    return false;
                }
                state.walkMode = WalkMode.NoAnimation;
                character.Input.MoveInput.x = 0;
                if(indexChanged) {
                    if(_updater.currentIndex == 1) {
                        _decoy.Active = true;
                        _decoy.Reset(character.GameSetup, character.Team);
                        _decoy.Position = character.Position;
                        _spawnTime = Time.fixedTime;
                        _decoy.ActionManager.DoTimedAction(data.duration, null, TryDisable);
                        data._spawnEffect.SpawnItem(character.PhysicalCenter);
                        _decoy.Direction = character.Direction;
                        var vel = data.spawnVelocity;
                        if(character.Direction < 0){
                            vel.x = -vel.x;
                        }
                        character.CurrentVelocity = vel;
                        character.JumpStart();
                    }
                }
                return true;
            }
            private void TryDisable(object data, bool completed) {
                if(completed) {   
                    this.data._spawnEffect.SpawnItem(_decoy.PhysicalCenter);
                    _decoy.Active = false;
                }
            }
            void EnsureInit(Character2D character) {
                if(_decoy == null) {
                    _decoy = UnityEngine.Object.Instantiate(character, character.transform.parent);
                    _decoy.Active = false;
                    _decoy.ControlModules[data.aiControlModuleIndex].Enabled = true;
                    _decoy.MaxHealth = character.MaxHealth * data.defansMultiplayer;
                    for(int i = 0; i < _decoy.States.mappings.Length; i++) {
                        var module = _decoy.States.mappings[i].Module as AAttackModule;
                        if(module != null) {
                            for(int j = 0; j < module.DamageCount; j++) {
                                module.SetDamage(j, module.GetDamage(j) * data.attackMultiplayer);
                            }
                        }
                    }
                }
            }
            public void Attached(Character2D character)
            {
                if(_decoy != null) {
                    _decoy.gameObject.DestroySelf();
                }
                
                
            }

            public void Deattached()
            {
                mappings = null;
                if(_decoy != null) {
                    _decoy.gameObject.DestroySelf();
                }
            }
        }
    }
}