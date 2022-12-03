using System.Collections;
using System.Collections.Generic;
using Mobge.Platformer.PropertyModifier;
using UnityEngine;

namespace Mobge.Platformer.Character {
    [CreateAssetMenu(menuName = "Mobge/Platformer/SlowEffect", order = 21)]
    public class SlowEffect : ACharacterEffect, IModifierDescription
    {
        private static Property[] sEmptyProperties = new Property[0];
#if UNITY_EDITOR
        private static string[] s_editorValueNames = new string[] {
            "duration",
            "amount"
        };
        public override string[] EditorValueNames {
            get {
                return s_editorValueNames;
            }
        }
#endif

        public override void ApplyEffect(Character2D thisCharacter, CharacterEffect effect, Character2D target, ref DamageData damageData)
        {
            Modifier m;
            m.duration = effect.values[0];
            m.amount = effect.values[1];
            target.AddPropertyModifier(this, m);
        }

        public Property[] FindMethods(Component target)
        {
            Side2DMove move = target.GetComponentInChildren<Side2DMove>();
            if(move == null) {
                return sEmptyProperties;
            }
            Property v = new Property();
            v.target = move;
            v.field = move.GetType().GetField(nameof(move.walkSpeed));
            return new Property[] { v };
        }
    }
}
