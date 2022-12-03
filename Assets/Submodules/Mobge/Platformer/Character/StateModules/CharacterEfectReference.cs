using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Platformer.Character {
    [Serializable]
    public struct CharacterEffectGroup
    {
        public CharacterEffect[] effects;
        public void ApplyEffect(Character2D thisCharacter, Character2D target, ref DamageData damageData) {
            for(int i = 0; i < effects.Length; i++) {
                var e = effects[i];
                if(e.probability >= UnityEngine.Random.value) {
                    e.effect.ApplyEffect(thisCharacter, e, target, ref damageData);
                }
            }
        }
        
    }
    [Serializable]
    public struct CharacterEffect {
        public float[] values;
        public float probability;
        public ACharacterEffect effect;
        public ReusableReference visualEffect;
    }
}