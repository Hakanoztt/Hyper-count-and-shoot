using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mobge.Platformer.Character {
    public abstract class CollectableData : ScriptableObject
    {
        public CharacterMappingScheme mappingScheme;
        public void ApplyEffect(Character2D character) {
            this.ApplyEffect(character, character.GetComponent<CharacterMappings>());
        }
        protected abstract void ApplyEffect(Character2D character, CharacterMappings mappings);
    }
    public abstract class StateModuleData : CollectableData {
        private static Dictionary<Type, Stack<IStateModule>> _moduleCache = new Dictionary<Type, Stack<IStateModule>>();
        protected static T NewModule<T>() where T : UnityEngine.ScriptableObject, IStateModule{
            var t = typeof(T);
            if(_moduleCache.TryGetValue(t, out Stack<IStateModule> modules)) {
                if(modules.Count > 0) {
                    return (T)modules.Pop();
                }
            }
            return CreateInstance<T>();
        }
        protected static void CacheModule(IStateModule module) {
            var t = module.GetType();
            if(!_moduleCache.TryGetValue(t, out Stack<IStateModule> modules)) {
                modules = new Stack<IStateModule>();
                _moduleCache.Add(t, modules);
            }
            modules.Push(module);
        }
        protected abstract IStateModule GetModule(Character2D character, CharacterMappings mappings);
        protected override void ApplyEffect(Character2D character, CharacterMappings mappings) {
        }
        public IStateModule GetModule(Character2D character) {
            return GetModule(character, character.GetComponent<CharacterMappings>());
        }
    }
}
