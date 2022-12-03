using Mobge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup {

    [Serializable]
    public class ItemCluster {

        public static string PropertyName_items => nameof(items);


        [SerializeField] public ItemSet set;
        [SerializeField] private List<ItemContent> items;


        public ItemSet Set => set;
        public ListIndexer<ItemContent> Items => new ListIndexer<ItemContent>(items);

        public ItemCluster() {

        }
        public ItemCluster(ItemSet set) {
            this.set = set;
            items = new List<ItemContent>();
        }


        public bool Contains(ItemCluster cluster, out bool typeMismatch) {
            if(cluster.set != set) {
                typeMismatch = true;
                return false;
            }
            typeMismatch = false;
            for (int i = 0; i < cluster.items.Count; i++) {
                var it = cluster.items[i];
                int count = GetCount(it.id, out _);
                if (count < it.count) {
                    return false;
                }
            }
            return true;
        }
        public bool Contains(in ItemCluster.ItemContent item) {
            for (int i = 0; i < items.Count; i++) {
                int count = GetCount(item.id, out _);
                if (count < item.count) {
                    return false;
                }
            }
            return true;
        }

        public void UpdateFromItemSet(AGameContext context) {
            Clear();
            var setData = context.GameProgressValue.GetQuantityItemSet(set);
            var en = setData.GetQuantityEnumerator();
            while (en.MoveNext()) {
                var c = en.Current;
                items.Add(new ItemCluster.ItemContent(c.key, c.quantity));
            }
            en.Dispose();
        }

        public void Add(ItemCluster cluster, out bool typeMismatch) {
            if (cluster.set != set) {
                typeMismatch = true;
                return;
            }
            typeMismatch = false;
            for (int i = 0; i < cluster.items.Count; i++) {
                var it = cluster.items[i];
                int count = GetCount(it.id, out int index);
                if(index < 0) {
                    this.items.Add(it);
                }
                else {
                    this.items[index] = new ItemContent(it.id, count + it.count);
                }
            }
        }
        public void Add(int itemId, int amount) {
            int count = GetCount(itemId, out int index);
            if (index < 0) {
                this.items.Add(new ItemContent(itemId, amount));
            }
            else {
                this.items[index] = new ItemContent(itemId, count + amount);
            }
        }
        public bool TryRemove(ItemCluster cluster, out bool typeMismatch) {
            if (!Contains(cluster, out typeMismatch)) {
                return false;
            }
            for (int i = 0; i < cluster.items.Count; i++) {
                var it = cluster.items[i];
                if (it.count > 0) {
                    int count = GetCount(it.id, out int index);
                    count -= it.count;
                    if (count == 0) {
                        this.items.RemoveAt(index);
                    }
                    else {
                        this.items[index] = new ItemContent(it.id, count); 
                    }
                }
            }
            return true;
        }
        public bool TryRemove(int itemId, int amount) {
            int count = GetCount(itemId, out int index);
            if (index < 0) {
                return false;
            } else {
                count -= amount;
                if (count < 0) {
                    return false;
                }
                if (count == 0) {
                    items.RemoveAt(index);
                } else {
                    items[index] = new ItemContent(itemId, count);
                }
                return true;
            }
        }
        public bool TryRemove(in ItemCluster.ItemContent item) {
            return TryRemove(item.id, item.count);
        }
        public int GetCount(int id, out int index) {
            for(int i = 0; i < items.Count; i++) {
                var it = items[i];
                if (it.id == id) {
                    index = i;
                    return it.count;
                }
            }
            index = -1;
            return 0;
        }

        public void Clear() {
            if (items != null) {
                items.Clear();
            }
            else {
                items = new List<ItemContent>();
            }
        }

        [Serializable]
        public struct ItemContent {
            public int id;
            public int count;

            public ItemContent(int id, int count) {
                this.id = id;
                this.count = count;
            }

            //public static implicit operator int(ItemContent id) {
            //    return id.id;
            //}
        }

    }

}