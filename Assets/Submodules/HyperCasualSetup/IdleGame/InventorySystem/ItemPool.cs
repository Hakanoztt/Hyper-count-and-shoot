using Mobge.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame {
    public class ItemPool : MonoBehaviour {

        private const string c_key = "gnr_itm_pll";
        
        public static ItemPool Get(LevelPlayer player) {
            if(!player.TryGetExtra(c_key, out ItemPool pool)) {
                pool = new GameObject("item pool").AddComponent<ItemPool>();
                pool.transform.SetParent(player.transform, false);
                player.SetExtra(c_key, pool);
            }
            return pool;
        }



        private PrefabCache<Item> _pool;

        private ItemPool() {
            _pool = new PrefabCache<Item>(true, false);
        }

        public Item SpawnItem(Item prefab, Transform parent, Vector3 localPosition) {
            return SpawnItem(prefab, parent, localPosition, Quaternion.identity);
        }
        public Item SpawnItem(Item prefab, Transform parent, Vector3 localPosition, Quaternion localRotation) {
            var it = _pool.Pop(prefab, parent);
            it.PrefabReference = prefab;
            var ittr = it.transform;
            ittr.localPosition = localPosition;
            ittr.localRotation = localRotation;
            return it;
        }
        public void Recycle(Item instance) {
            _pool.Push(instance.PrefabReference, instance);
        }
    }
}