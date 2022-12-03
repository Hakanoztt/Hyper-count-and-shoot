using Mobge.Core;
using Mobge.HyperCasualSetup;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame {
    /// <summary>
    /// Base class for stacking instances of <see cref="Item"/>>s.
    /// This class should only be extended for visualization purposes, for other purposes it should only be referenced from other classes.
    /// </summary>
    public class BaseItemStack : MonoBehaviour {
        public const string c_tag = "ItemStack";

        public static bool TryGet(Collider c, out BaseItemStack stack) {
            if (c.CompareTag(c_tag)) {
                return c.TryGetComponent(out stack);
            }
            stack = null;
            return false;
        }

        private AutoIndexedMap<Item> _items;
        [SerializeField, InterfaceConstraint(typeof(IFilter))] private List<UnityEngine.Object> predefinedFilters;
        public List<IFilter> filters;
        public bool deactivateItems = true;
        public int maxCount = 100;
        public bool detectable = false;

        [Tooltip("List of acceptable item classes. Empty or null means this stack accepts any item.")]
        [SerializeField] private List<ClassRef> acceptableItems;

        private Transform _defaultItemParent;


        public BaseLevelPlayer LevelPlayer { get; private set; }

        public bool Initialized => LevelPlayer != null;

        public virtual int ItemCount => _items.Count;

        protected void Awake() {
            EnsureAwake();
        }

        protected void Start() {
            EnsureInit((BaseLevelPlayer)this.GetLevelPlayer());
        }
        private void EnsureAwake() {

            if (_items == null) {
                if (detectable) {
                    gameObject.tag = c_tag;
                }
                _items = new AutoIndexedMap<Item>();
                if (filters == null) {
                    filters = new List<IFilter>();
                }
                if (predefinedFilters != null) {
                    for (int i = 0; i < predefinedFilters.Count; i++) {
                        filters.Add((IFilter)predefinedFilters[i]);
                    }
                }
            }
        }
        public BaseItemStack EnsureInit(BaseLevelPlayer player) {
            EnsureAwake();
            this.LevelPlayer = player;
            return this;
        }
        public bool AddAcceptableClass(ItemClass @class) {
            if (IndexOf(@class)>=0) {
                return false;
            }
            if (acceptableItems == null) {
                acceptableItems = new List<ClassRef>();
            }
            ClassRef r;
            r.count = 0;
            r.@class = @class;
            acceptableItems.Add(r);
            return true;
        }

        public AutoIndexedMap<Item>.PairEnumerator GetEnumerator() {
            return _items.GetPairEnumerator();
        }


        public int CalculateAcceptableItemCount(BaseItemStack source) {
            if(!HasClassFilter) {
                return source.ItemCount;
            }
            int totalCount = 0;
            for (int i = 0; i < acceptableItems.Count; i++) {
                int cIndex = source.IndexOf(acceptableItems[i].@class);
                if(cIndex >= 0) {
                    totalCount += source.acceptableItems[cIndex].count;
                }
            }
            return totalCount;
        }


        private int IndexOf(ItemClass @class) {
            if(acceptableItems == null) {
                return -1;
            }
            for(int i = 0; i < acceptableItems.Count; i++) {
                if(acceptableItems[i].@class == @class) {
                    return i;
                }
            }
            return -1;
        }
        public bool HasClassFilter {
            get => acceptableItems != null && acceptableItems.Count > 0;
        }
        public bool CanAdd(Item item) {
            if(maxCount == this.ItemCount) {
                return false;
            }
            if (HasClassFilter) {
                if(IndexOf(item.Class) < 0) {
                    return false;
                }
            }
            for (int i = 0; i < filters.Count; i++) {
                if (!filters[i].CanContain(item)) {
                    return false;
                }
            }
            return true;
        }

        public bool AddItem(Item item) {
            return AddItem(item, out _);
        }

        public virtual bool AddItem(Item item, out int id) {
            if (CanAdd(item)) {
                if (item.Container == null) {

                    if (_defaultItemParent == null) {
                        _defaultItemParent = item.transform.parent;
                    }
                    if (deactivateItems) {
                        item.gameObject.SetActive(false);
                    }
                    id = _items.AddElement(item);
                    Item.SetOwner(item, this, id);

                    if (HasClassFilter)
                    {
                        int index = IndexOf(item.Class);
                        var r = acceptableItems[index];
                        r.count += 1;
                        acceptableItems[index] = r; 
                    }

                    return true;
                }
            }
            id = -1;
            return false;
        }
        public virtual bool RemoveItem(Item item) {
            if(item.Container == (object)this) {
                if (deactivateItems) {
                    item.gameObject.SetActive(true);
                }
                _items.RemoveElement(item.IdInContainer);
                // item.transform.position = transform.position;
                Item.ReleaseOwner(item, this);
                if (item.transform.parent != _defaultItemParent) {
                    item.transform.SetParent(_defaultItemParent);
                }

                if (HasClassFilter)
                {
                    int index = IndexOf(item.Class);
                    var r = acceptableItems[index];
                    r.count -= 1;
                    acceptableItems[index] = r;
                }

                return true;
            }
            return false;
        }

        public interface IFilter {
            bool CanContain(Item item);
        }

        [Serializable]
        public struct ClassRef {
            public ItemClass @class;
            [NonSerialized] public int count;
        }
    }

}