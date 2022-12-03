using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Platformer.Character {
    [CreateAssetMenu(menuName = "Mobge/Platformer/KnockbackEffect", order = 21)]
    public class KnockbackEffect : ACharacterEffect
    {
        #if UNITY_EDITOR
        private static string[] s_editorValueNames = new string[] {
            "force multiplayer"
        };
        public override string[] EditorValueNames {
            get {
                return s_editorValueNames;
            }
        }
        #endif

        public override void ApplyEffect(Character2D thisCharacter, CharacterEffect effect, Character2D target, ref DamageData damageData)
        {
            damageData.knockbackMultiplayer *= effect.values[0];
            damageData.poiseMultiplayer = float.PositiveInfinity;
        }
    }

}