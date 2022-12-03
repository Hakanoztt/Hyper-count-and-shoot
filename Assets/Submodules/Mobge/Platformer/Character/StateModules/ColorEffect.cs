using System.Collections;
using System.Collections.Generic;
using Mobge.Platformer.PropertyModifier;
using UnityEngine;

namespace Mobge.Platformer.Character {
    [CreateAssetMenu(menuName = "Mobge/Platformer/ColorEffect", order = 21)]
    public class ColorEffect : ACharacterEffect, IModifierDescription
    {
#if UNITY_EDITOR
        public override string[] EditorValueNames => new string[] {
            "r",
            "g",
            "b",
            "a",
        };
#endif

        public override void ApplyEffect(Character2D thisCharacter, CharacterEffect effect, Character2D target, ref DamageData damageData)
        {
            // Color a;
            //a.
        }

        public Property[] FindMethods(Component target)
        {
            throw new System.NotImplementedException();
        }
    }
}