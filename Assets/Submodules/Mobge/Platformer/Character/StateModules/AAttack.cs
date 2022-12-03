using System;
using UnityEngine;

namespace Mobge.Platformer.Character{
    public abstract class AAttackModule : MonoBehaviour, IStateModule{
        public abstract float DPS {
            get;
        }
        public abstract int DamageCount { get; }
        public abstract float GetDamage(int index);
        public abstract void SetDamage(int index, float value);

        public abstract bool UpdateModule(Character2D character, in CharacterInput.Button button, ref Character2D.State state);

        public abstract bool TryEnable(Character2D character, in CharacterInput.Button button, int actionIndex);

        public abstract void Interrupted(Character2D character);
        public virtual int ActionCount => 1;
        public virtual void Attached(Character2D character)
        {
        }
        public virtual void Deattached()
        {
        }

        [NonSerialized]
        public float damageMultiplayer = 1;
    }
}