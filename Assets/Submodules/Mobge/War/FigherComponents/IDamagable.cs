using Mobge.Platformer.Character;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.War {
    public interface IDamagable {
        Collider Collider { get; }
        int Team { get; }
        bool IsAlive { get; }
        bool TakeDamage(in DamageData data);
    }
    public static class DamagableExtensions {
        public static float GetDistanceSquared(this IDamagable @this, IDamagable damagable) {
            var c1 = @this.Collider;
            var c2 = damagable.Collider;
            var posOn1 = c1.ClosestPoint(c2.transform.position);
            var posOn2 = c2.ClosestPoint(posOn1);
            return (posOn1 - posOn2).sqrMagnitude;
        }
        public static Vector3 GetPosition(this IDamagable @this) {
            return @this.Collider.transform.position;
        }
    }
    public struct DamageData {
        public float damage;
        public bool forced;
        public DamageData(float damage, bool forced = false) {
            this.damage = damage;
            this.forced = forced;
        }
    }
}