using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Platformer.Character {
    [CreateAssetMenu(menuName = "Mobge/Platformer/Collectable/Range Weapon")]
    public class RangeModuleData : StateModuleData
    {
        [CharacterMapping(CharacterMappingAttribute.Mapping.Module)]
        public int rangeModule;
        [InterfaceConstraint(typeof(Projectile), ReusableReference.ReferenceFieldName)]
        public ReusableReference projectile;
        public float damage;
        public ProjectileShootData shootData;
        public float gravity = -10;
        public CharacterEffectGroup attackEffect;

        protected override IStateModule GetModule(Character2D character, CharacterMappings mappings)
        {
            return null;
        }
    }
}
