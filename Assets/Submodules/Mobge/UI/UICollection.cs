using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.UI
{
    [Serializable]
    public class UICollection<T> where T : Component
    {
        [OwnComponent] public T itemRes;
        public T[] predefinedItems;
        [OwnComponent] public RectTransform customParent;
        private int _count;
        private List<ElementReference> _items;


        private RectTransform _currentParent;
        public RectTransform CurrentParent {
            get {
                if (_currentParent == null) {
                    _currentParent = customParent ? customParent : (itemRes ? (RectTransform)itemRes.transform.parent : ((predefinedItems != null && predefinedItems.Length > 0) ?(RectTransform)predefinedItems[0].transform.parent : null ));
                }
                return _currentParent;
            }
        }


        public int Count {
            get => _count;
            set {
                if (_items == null) {
                    _items = new List<ElementReference>();
                    if (itemRes) {
                        if (itemRes.gameObject.activeSelf) {
                            itemRes.gameObject.SetActive(false);
                        }
                    }
                    if (predefinedItems != null) {
                        for (int i = 0; i < predefinedItems.Length; i++) {
                            var pi = predefinedItems[i];
                            pi.gameObject.SetActive(false);
                            _items.Add(new ElementReference(pi, i));
                        }
                    }
                }
                InitializeListItems(_items, itemRes, ref _count, value, CurrentParent);
            }
        }

        public void ReverseOrder() {
            for(int i = 1; i < _items.Count; i++) {
                _items[i].transform.SetAsFirstSibling();
            }
        }
        public T this[int index] {
            get => _items[index].item;
        }
        public int GetResourceId(int index) {
            return _items[index].resId;
        }
        public T RemoveElement(int index) {
            if (index >= _count) {
                throw new IndexOutOfRangeException();
            }
            var item = _items[index];
            _items.RemoveAt(index);
            item.item.gameObject.SetActive(false);
            _items.Add(item);
            _count--;


            return item.item;
        }
        public T AddElement(int itemId = -1) {
            var parent = CurrentParent;
            int index = _count;
            int listIndex = -1;
            for(int i = _count; i < _items.Count; i++) {
                if(_items[i].resId == itemId) {
                    listIndex = i;
                    break;
                }
            }
            _count++;

            if (listIndex < 0) {
                T itemRes;
                if(itemId >= 0) {
                    itemRes = predefinedItems[itemId];
                }
                else {
                    itemRes = this.itemRes;
                }
                var er = new ElementReference(UnityEngine.Object.Instantiate(itemRes, parent, false), itemId);
                listIndex = _items.Count;
                _items.Add(er);
            }
            if (listIndex != index) {
                var er = _items[listIndex];
                _items.RemoveAt(listIndex);
                _items.Insert(index, er);
                er.transform.SetSiblingIndex(index);
            }
            var it = _items[index].item;
            it.gameObject.SetActive(true);
            return it;
        }

        public static void InitializeListItems(List<ElementReference> items, T res, ref int lastCount, int targetCount, Transform parent) {

            if (res) {
                
                while (items.Count < targetCount) {
                    var item = UnityEngine.Object.Instantiate(res, parent, false);
                    items.Add(new ElementReference(item, -1));
                }
            }
            while (lastCount < targetCount) {
                items[lastCount].gameObject.SetActive(true);
                lastCount++;
            }
            while (lastCount > targetCount) {
                lastCount--;
                items[lastCount].gameObject.SetActive(false);
            }
        }
        public struct ElementReference {
            public int resId;
            public T item;
            public Transform transform => item.transform;
            public GameObject gameObject => item.gameObject;
            public ElementReference(T item, int id) {
                this.item = item;
                this.resId = id;
            }
        }
    }
}