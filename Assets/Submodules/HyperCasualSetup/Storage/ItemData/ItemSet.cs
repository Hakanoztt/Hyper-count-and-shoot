using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup
{
    [CreateAssetMenu(menuName = "Hyper Casual/ItemSet")]
    public class ItemSet : ScriptableObject {

#if UNITY_EDITOR
        [HideInInspector] public int editorEquipped;
#endif
        [HideInInspector] public int defaultItem = -1;

        [HideInInspector] public Map items;
        public string[] valueLabels;
        public bool stackable;

        public string tag;

        public static ItemPathComparer PathComparer { get; } = new ItemPathComparer();
        public static ItemPathEqualityComparer PathEqualityComparer { get; } = new ItemPathEqualityComparer();

        protected virtual void OnEnable() {}

        public Item GetEquippedItem(AGameContext context, out int index)
        {
            var val = context.GameProgressValue;
            var setData = val.GetItemSet(this);
            index = setData.EquippedItem;
#if UNITY_EDITOR
            if (context.TestMode)
            {
                index = editorEquipped;
            }
#endif
            if (items.TryGetElement(index, out Item element)) {
                return element;
            }
            return null;
        }
        public class ItemPathComparer : Comparer<ItemPath> {
            public override int Compare(ItemPath x, ItemPath y) {
                return x.GetHashCode() - y.GetHashCode();
            }
        }
        public class ItemPathEqualityComparer : EqualityComparer<ItemPath> {
            public override bool Equals(ItemPath x, ItemPath y) {
                return x.set.name == y.set.name && x.id == y.id;
            }
            public override int GetHashCode(ItemPath obj) {
                return obj.GetHashCode();
            }
        }
        [Serializable]
        public struct ItemPath {
            public ItemSet set;
            public int id;
            public bool Stackable => set.stackable;

            public ItemPath(ItemSet set, int id, bool stackable) {
                this.set = set;
                this.id = id;
            }

            public Item Item => set.items[id];

            public override int GetHashCode() {
                return set.name.GetHashCode() * 23 + id;
            }
        }
        [Serializable]
        public class Item
        {
            public string name;
            public Sprite sprite;
            public int cost;
            public float costIncreaseForLevel = 0;
            public float geometricCostIncreaseForLevel = 0;
            public UnityEngine.Object[] contents;
            public float[] values;
            public int defaultLevel;
            public float costMax = float.PositiveInfinity;
            public override string ToString()
            {
                if (!string.IsNullOrEmpty(name)) {
                    return name;
                }
                if (sprite != null)
                {
                    return sprite.ToString();
                }
                if (contents != null && contents.Length > 0 && contents[0] != null)
                {
                    return contents[0].name;
                }
                return "null";
            }
            public float GetValueSafe(int index, float @default = 0) {
                if (values == null || values.Length <= index) return @default;
                return values[index];
            }
            public int GetCost(int currentLevel) {
                var result = (cost + costIncreaseForLevel * currentLevel);
                if (geometricCostIncreaseForLevel != 0) {
                    result *= Mathf.Pow(geometricCostIncreaseForLevel, currentLevel);
                }
                return Mathf.RoundToInt(Mathf.Min(result, costMax));

            }
            public bool TryGetContent<T>(int index, out T content) {
                if(contents!=null && contents.Length > index) {
                    if(contents[index] is T t) {
                        content = t;
                        return true;
                    }
                }
                content = default;
                return false;
            }
        }

        [Serializable]
        public class Map : AutoIndexedMap<Item>
        {

        }
    }
}