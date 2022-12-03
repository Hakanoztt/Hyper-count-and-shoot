using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Platformer.Character {
    public abstract class ACharacterEffect : ScriptableObject 
    {
        public abstract void ApplyEffect(Character2D thisCharacter, CharacterEffect effect, Character2D target, ref DamageData damageData);
        #if UNITY_EDITOR
        public abstract string[] EditorValueNames { get; }
        #endif
    }
}