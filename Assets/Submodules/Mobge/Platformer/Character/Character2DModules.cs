using System;
using System.Collections.Generic;
using UnityEngine;
using static Mobge.Platformer.Character.CharacterInput;

namespace Mobge.Platformer.Character {
    public partial class Character2D : MonoBehaviour {
        [Serializable]
        public struct InputMapper {
            public InputMapping[] mappings;
        }
        [Serializable]
        public struct InputMapping {
            [OwnComponent(typeof(IStateModule))] [SerializeField]
            private UnityEngine.Object module;
            [SerializeField]
            private StateModuleData _moduleData;
            public int actionIndex;

            public IStateModule Module { 
                get => module as IStateModule;
            }

            public StateModuleData ModuleData { 
                get => _moduleData;
            }
            public void Initialize(Character2D character) {
                if(_moduleData != null) {
                    module = null;
                    SetModuleData(character, _moduleData);
                }
                else if(module != null) {
                    Module.Attached(character);
                }
            }
            public void Destyoy() {
                if(module != null) {
                    Module.Deattached();
                }
            }
            public void SetModuleData(Character2D character, StateModuleData value, int actionIndex = 0) {
                module = value;
                SetModule(character, value.GetModule(character), actionIndex);
            }
            public void SetModule(Character2D character, IStateModule value, int actionIndex = 0) {
                var m = Module;
                if(m != null) {
                    m.Deattached();
                }
                module = value as UnityEngine.Object;
                this.actionIndex = actionIndex;
                if(value != null) {
                    value.Attached(character);
                }
            }
            public override string ToString(){
                return "" + module;
            }
        }
        private struct ActiveState
        {
            public IStateModule stt{get; private set;}
            public int inputIndex{get; private set;}
            private bool ownComponent;
            public void ReleaseModule(Character2D c) {
                if(!ownComponent) {
                    stt.Deattached();
                }
                c._state.Reset();
                stt = null;
            }
            public void InterruptModule(Character2D c) {
                if(stt != null) {
                    stt.Interrupted(c);
                    ReleaseModule(c);
                }
            }
            public void SetModule(Character2D c, IStateModule module, int inputIndex, bool ownComponent) {
                InterruptModule(c);
                stt = module;
                this.inputIndex = inputIndex;
                this.ownComponent = ownComponent;
                if(!ownComponent && stt != null) {
                    stt.Attached(c);
                }
            }
        }
        [Serializable]
        public struct State {
            public WalkMode walkMode;
            public void Reset() {
                walkMode = WalkMode.Normal;
            }
        }
    }
    public interface BaseMoveModule : BaseModule {
        bool UpdateModule(Character2D character, ref GroundContact groundContact, WalkMode walkMode);
    }
    public interface BaseJumpModule : BaseModule {
        bool UpdateModule(Character2D character, ref int airJumpCount, WalkMode walkMode);
    }
    public interface BaseModule {
        void SetEnabled(Character2D character, bool enabled);
    }
    public struct ModuleList<T> where T : class{
        private UnityEngine.Object[] _objects;
        public ModuleList(UnityEngine.Object[] modules) {
            this._objects = modules;
        }
        public int Count => _objects.Length;
        public bool IsNull => _objects == null;
        public T this[int index]
        {
            get => _objects[index] as T;
        }
    }
    public interface IControlModule
    {
        bool Enabled { get; set; }

        void UpdateModule(Character2D character);
        void Initialize(Character2D character);
    }
    public abstract class AControlModule : MonoBehaviour, IControlModule {
        public virtual bool Enabled { get => enabled; set => enabled = value; }

        public abstract void UpdateModule(Character2D character);
        public abstract void Initialize(Character2D character);
    }
    public interface IStateModule {
        bool UpdateModule(Character2D character, in Button button, ref Character2D.State state);
        bool TryEnable(Character2D character, in Button button, int actionIndex);
        void Interrupted(Character2D character);
        int ActionCount {get;}
        void Attached(Character2D character);
        void Deattached();
    }
    public abstract class DamageHandlerState : MonoBehaviour, IStateModule {
        public abstract bool HandleDamage(Character2D character, ref Health health, ref Poise poise, in DamageData damage);

        public abstract void Interrupted(Character2D character);

        public bool TryEnable(Character2D character, in Button button, int actionIndex) {
            throw new InvalidOperationException("For " + nameof(DamageHandlerState) + " " + nameof(HandleDamage) + " have to be called instead of this method.");
        }
        public abstract bool UpdateModule(Character2D character, in Button button, ref Character2D.State state);

        public virtual void Attached(Character2D character)
        {
        }

        public virtual void Deattached()
        {
        }

        public int ActionCount => 0;
    }
    [Serializable]
    public struct DamageData {
        public float damage;
        public float poiseMultiplayer;
        public float knockbackMultiplayer;
        [NonSerialized] public Vector3 damagePosition;
        public DamageData(float damage, Vector3 damagePosition) {
            this.damage = damage;
            this.poiseMultiplayer = 1;
            this.knockbackMultiplayer = 1;
            this.damagePosition = damagePosition;
        } 
    }
    
}
