using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Platformer.Character {
    [CreateAssetMenu(menuName = "Mobge/Platformer/PushEffect", order = 21)]
    public class PushEffect : ACharacterEffect
    {
        public Mode mode;
        #if UNITY_EDITOR
        private static string[] s_editorValueNames = new string[] {
            "power"
        };
        public override string[] EditorValueNames {
            get {
                return s_editorValueNames;
            }
        }
        #endif

        public override void ApplyEffect(Character2D thisCharacter, CharacterEffect effect, Character2D target, ref DamageData damageData)
        {
            damageData.knockbackMultiplayer = 0;
            damageData.poiseMultiplayer = float.PositiveInfinity;
            var dif = target.PhysicalCenter - damageData.damagePosition;
            dif.Normalize();
            float power = effect.values[0];
            switch(mode) {
                default:
                case Mode.PowerAsMomentum:
                power /= target.Mass;
                break;
                case Mode.PowerAsSpeed:
                break;
            }
            target.CurrentVelocity = power * dif;
            target.JumpStart();
        }
        public enum Mode {
            PowerAsMomentum = 0,
            PowerAsSpeed = 1,
        }
    }
}