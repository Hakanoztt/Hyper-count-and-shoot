using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Platformer.Character {
    [CreateAssetMenu(menuName = "Mobge/Platformer/CriticEffect", order = 21)]
    public class CriticEffect : ACharacterEffect
    {
        #if UNITY_EDITOR
        private static string[] s_editorValueNames = new string[] {
            "attack multiplayer"
        };
        public override string[] EditorValueNames {
            get {
                return s_editorValueNames;
            }
        }
        #endif
        public override void ApplyEffect(Character2D thisCharacter, CharacterEffect effect, Character2D target, ref DamageData damageData) {
            
            damageData.damage *= effect.values[0];
            var p1 = thisCharacter.PhysicalCenter;
            var p2 = target.PhysicalCenter;
            var pos = p1 + p2;
            pos *= 0.5f;
            var i = effect.visualEffect.SpawnItem(pos);
            if(i != null) {
                i.transform.right = p2 - p1;
            }
            
        }
    }
}