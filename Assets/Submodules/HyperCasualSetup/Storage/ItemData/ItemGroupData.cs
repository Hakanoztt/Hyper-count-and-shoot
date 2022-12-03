using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mobge.HyperCasualSetup {

    [CreateAssetMenu(menuName = "Hyper Casual/Random Item Selector")]
    public class ItemGroupData : ScriptableObject {

        public static ItemGroupData Create(RuleSet[] sets) {

            ItemGroupData gd = ScriptableObject.CreateInstance<ItemGroupData>();
            gd.sets = sets;
            gd.Reinitialize();
            return gd;
        }


        [SerializeField] private RuleSet[] sets;
        protected readonly Dictionary<string, ItemSet> _itemSets = new Dictionary<string, ItemSet>();
        private static HashSet<ItemSet.ItemPath> s_availableItemCache = new HashSet<ItemSet.ItemPath>();
        private static List<int> s_tempItems = new List<int>();
        public struct RandomSelector {
            PathList[] _paths;
            private ItemGroupData _selector;
            private int _setIndex;

            public PathList[] Paths => _paths;

            internal RandomSelector(AGameContext context, ItemGroupData selector, int startIndex) {
                _paths = new PathList[selector.sets.Length];
                _selector = selector;
                if (startIndex < 0) {
                    _setIndex = UnityEngine.Random.Range(0, selector.sets.Length);
                }
                else {
                    _setIndex = startIndex - 1;
                }
                for (int i = 0; i < selector.sets.Length; i++) {
                    s_availableItemCache.Clear();
                    selector.sets[i].UpdateAvailableItems(context, s_availableItemCache);
                    _paths[i] = new PathList(new List<ItemSet.ItemPath>(s_availableItemCache));
                }
            }
            public int AvailableCount {
                get {
                    int total = 0;
                    for (int i = 0; i < _paths.Length; i++) {
                        total += _paths[i].Count;
                    }
                    return total;
                }
            }
            public ItemSet.ItemPath Select(int displayCount = -1) {
                PathList l;
                int tryCount = 0;
                do {
                    if (tryCount == _paths.Length) {
                        throw new Exception("Not enough element left.");
                    }
                    tryCount++;
                    _setIndex++;
                    if (_setIndex == _paths.Length) {
                        _setIndex = 0;
                    }
                }
                while (((l = _paths[_setIndex]).Count - l.selectedCount) == 0);
                ItemSet.ItemPath item;
                var mode = _selector.sets[_setIndex].selectMode;
                bool hasOrder = displayCount >= 0 && l.order.order != null;
                bool removed = false;
                if (mode == SelectMode.Random) {
                    if (hasOrder) {


                        int index = l.order.FindItemToDisplay(displayCount + l.selectedCount);
                        item = l[index];

                    }
                    else {
                        int index = UnityEngine.Random.Range(0, l.Count);
                        //randomlySelected = true;
                        item = l[index];

                        l[index] = l[l.Count - 1];
                        l.RemoveAt(l.Count - 1);
                        removed = true;

                    }
                }
                else {
                    if (hasOrder) {
                        s_tempItems.Clear();
                        s_tempItems.AddRange(l.order.order);
                        s_tempItems.Sort();
                        int order = s_tempItems[(l.selectedCount % s_tempItems.Count)];
                        int index = Array.IndexOf(l.order.order, order);
                        item = l[index];
                    }
                    else {
                        item = l[0];
                        l.RemoveAt(0);
                        removed = true;
                    }
                }
                if (!removed) {
                    _paths[_setIndex].selectedCount++;
                }

                return item;
            }
        }

        public RuleSet[] Groups => sets;
        public virtual  RandomSelector NewRandomSelector(AGameContext context, int startIndex = -1) {
            RandomSelector sel = new RandomSelector(context, this, startIndex);
            return sel;
        }
        private void Reinitialize() {
            _itemSets.Clear();
            if (sets != null) {
                for (int i = 0; i < sets.Length; i++) {
                    var rules = sets[i].rules;
                    for (int j = 0; j < rules.Length; j++) {
                        var itemSet = rules[j].set;
                        if (!_itemSets.ContainsKey(itemSet.name)) _itemSets.Add(itemSet.name, itemSet);
                    }
                }
            }
        }
        public void OnEnable() {
            Reinitialize();
        }
        public bool TryGetItemSet(string name, out ItemSet itemSet) {
            return _itemSets.TryGetValue(name, out itemSet);
        }
        [Serializable]
        public struct RuleSet {
            public string name;
            public SelectMode selectMode;
            public Rule[] rules;

            public bool UpdateAvailableItems(AGameContext context, HashSet<ItemSet.ItemPath> items) {
                if (rules == null) {
                    return false;
                }
                for(int i = 0; i < rules.Length; i++) {
                    if (!rules[i].UpdateAvailableItems(context, items)) {
                        return false;
                    }
                }
                return true;
            }
            public void GetAllItems(HashSet<ItemSet.ItemPath> items) {
                InternalGetAllItems(items);
            }
            public void GetAllItems(List<ItemSet.ItemPath> items) {
                InternalGetAllItems(items);
            }
            internal bool InternalGetAllItems(Container<ItemSet.ItemPath> items) {
                if (rules == null) {
                    return false;
                }
                for(int i = 0; i < rules.Length; i++) {
                    if (!rules[i].InternalGetAllItems(items)) {
                        return false;
                    }
                }
                return true;
            }
        }
        [Serializable]
        public struct Rule {
            public ItemSet set;
            public int label;
            public float value;
            public bool UpdateAvailableItems(AGameContext context, HashSet<ItemSet.ItemPath> items) {
                if(set == null) {
                    return false;
                }
                if (set.stackable) {
                    GetAllItemsDirect(items);
                }
                else {
                    var e = set.items.GetKeyEnumerator();
                    var data = context.GameProgressValue.GetItemSet(set);
                    if (label < 0) {
                        while (e.MoveNext()) {
                            var id = e.Current;
                            if (!data.HasItem(id)) {
                                items.Add(new ItemSet.ItemPath(set, id, false));
                            }
                        }
                    }
                    else {
                        while (e.MoveNext()) {
                            var id = e.Current;
                            var item = set.items[id];
                            float value;
                            if (item.values == null || item.values.Length <= label) {
                                value = 0;
                            }
                            else {
                                value = item.values[label];
                            }
                            if (value == this.value && !data.HasItem(id)) {
                                items.Add(new ItemSet.ItemPath(set, id, false));
                            }
                        }
                    }
                }
                return true;
            }
            internal bool InternalGetAllItems(Container<ItemSet.ItemPath> items) {
                if (set == null) {
                    return false;
                }
                GetAllItemsDirect(items);
                return true;
            }

            private void GetAllItemsDirect(Container<ItemSet.ItemPath> items) {
                var e = set.items.GetKeyEnumerator();
                if (label < 0) {
                    while (e.MoveNext()) {
                        items.Add(new ItemSet.ItemPath(set, e.Current, true));
                    }
                }
                else {
                    while (e.MoveNext()) {
                        var id = e.Current;
                        var item = set.items[id];
                        float value = item.GetValueSafe(label, 0f);
                        if (value == this.value) {
                            items.Add(new ItemSet.ItemPath(set, id, true));
                        }
                    }
                }
            }
            
        }
        internal struct Container<T> {
            private HashSet<T> _hasSet;
            private List<T> _list;
            public Container(HashSet<T> set) {
                _hasSet = set;
                _list = null;
            }
            public Container(List<T> list) {
                _hasSet = null;
                _list = list;
            }

            public void Add(in T element) {
                if (_hasSet != null) {
                    _hasSet.Add(element);
                }
                else {
                    _list.Add(element);
                }
            }

            public static implicit operator Container<T>(List<T> list) {
                return new Container<T>(list);
            }
            public static implicit operator Container<T>(HashSet<T> list) {
                return new Container<T>(list);
            }
        }
        public struct PathList {
            public Order order;
            private List<ItemSet.ItemPath> paths;
            public int selectedCount;
            public int Count => paths.Count;
            public PathList(List<ItemSet.ItemPath> paths) {
                this.paths = paths;
                this.order = default;
                selectedCount = 0;
            }
            public ItemSet.ItemPath this[int index] {
                get => paths[index];
                set => paths[index] = value;
            }

            public void RemoveAt(int index) {
                paths.RemoveAt(index);
            }
        }
        public static int Random(int range, int input) {

            uint state = (uint)input;
            state ^= 2747636419;
            state ^= 2554435769;
            state ^= state >> 16;
            state ^= 2554435769;
            state ^= state >> 16;
            state ^= 2554435769;
            return (int)(state % range);
        }
        public struct Order {
            public bool randomAfterAllRequested;
            public int[] order;
            private int _maxOrder;
            public int FindItemToDisplay(int displayCount) {
                if (_maxOrder == 0) {
                    for (int i = 0; i < order.Length; i++) {
                        int next = order[i];
                        _maxOrder = Mathf.Max(_maxOrder, next);
                    }
                    _maxOrder += 1;
                }
                if (!randomAfterAllRequested) {
                    displayCount = displayCount % _maxOrder;
                }
                for (int i = 0; i < order.Length; i++) {
                    int next = order[i];
                    if(displayCount == next) {
                        return i;
                    }
                }
                if (randomAfterAllRequested) {
                    return UnityEngine.Random.Range(0, order.Length);
                }
                return Random(order.Length, displayCount);
            }

            
        }

        public enum SelectMode {
            Random = 0,
            FirstAvailable = 1,
        }
    }
}