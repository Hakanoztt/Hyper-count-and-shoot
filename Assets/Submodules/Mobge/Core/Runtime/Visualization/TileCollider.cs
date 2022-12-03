using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core
{
    public static class TileCollider
    {
        // Grid collider
        public static void PrepareCollidersFromGridInfo(GridInfo gi, GameObject go, Vector2 tileSize)
        {
            foreach (GridInfo.Int2 keys in gi.Data.Keys)
            {
                var box = go.AddComponent<BoxCollider2D>();
                box.usedByComposite = true;
                box.offset = new Vector2(keys.x, keys.y);
                box.size = tileSize;
            }
            go.AddComponent<CompositeCollider2D>();
        }

        // Bitmask representation of the available collider types.
        public enum ColliderTypes 
        {
            Collider1 = 1,   // 00000001
            Collider2 = 2,   // 00000010
            Collider3 = 4,   // 00000100
            Collider4 = 8,   // 00001000
            Collider5 = 16,  // 00010000
            Collider6 = 32,  // 00100000
            Collider7 = 64,  // 01000000
            Collider8 = 128, // 10000000
        }
    }
}