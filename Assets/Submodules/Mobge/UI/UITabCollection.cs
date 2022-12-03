using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.UI {
    public class UITabCollection : MonoBehaviour {
        //public bool useResourcesDirectly;
        [SerializeField] private TabCollection _tabs;
        [SerializeField] private ContentCollection _contents;
        private int _selectedTab = int.MaxValue;
        public event Action<UITabCollection, int> OnSelectionChange;
        [SerializeField, OwnComponent] protected ScrollRect _tabScroll;
        public T GetPage<T>(int index) where T : MonoBehaviour {
            return _contents[index].GetComponent<T>();
        }
        protected void Awake() {

        }

        public Collection<UIItem> Tabs => new Collection<UIItem>(_tabs);
        public Collection<RectTransform> Contents => new Collection<RectTransform>(_contents);

        public int Count {
            get => _tabs.Count;
            set {
                int oldCount = Count;
                _tabs.Count = value;
                _contents.Count = value;
                for(int i = 0; i < value; i++) {
                    _contents[i].gameObject.SetActive(false);
                    var tab = _tabs[i];
                    tab.tag = i;
                    tab.buttons[0].OnClick -= TabClicked;
                    tab.buttons[0].OnClick += TabClicked;
                    tab.SetState(0);
                }
                if (_tabScroll) {
                    _tabScroll.normalizedPosition= Vector2.zero;
                }
                SetSelectedTab(0);
            }
        }

        private void TabClicked(UIItem arg1, int arg2) {
            if (_selectedTab != arg1.tag) {
                SelectedTab = arg1.tag;
            }
        }

        public int SelectedTab {
            get => _selectedTab;
            set {
                if (_selectedTab != value) {
                    SetSelectedTab(value);
                }
            }
        }

        private void SetSelectedTab(int value) {
            if (_selectedTab < _tabs.Count) {
                _contents[_selectedTab].gameObject.SetActive(false);
                _tabs[_selectedTab].SetState(0);
            }
            _selectedTab = value;
            if (_selectedTab < _tabs.Count) {
                _contents[_selectedTab].gameObject.SetActive(true);
                var tab = _tabs[_selectedTab];
                //tab.transform.SetAsLastSibling();
                tab.SetState(1);
            }
        }

        [Serializable] public class TabCollection : UICollection<UIItem> { }
        [Serializable] public class ContentCollection : UICollection<RectTransform> { }
        public struct Collection<T> where T : Component {
            private UICollection<T> _collection;

            internal Collection(UICollection<T> collection) {
                _collection = collection;
            }
            public int PredefinedItemCount => _collection.predefinedItems == null ? 0 : _collection.predefinedItems.Length;
            public T this[int index] {
                get => _collection[index];
            }
            public K Get<K>(int index) where K : Component {
                return _collection[index].GetComponent<K>();
            }
        }
    }
}