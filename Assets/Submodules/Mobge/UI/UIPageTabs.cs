using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.UI {
    public class UIPageTabs : MonoBehaviour {
        [OwnComponent] public UIPageCollection pageCollection;
        [Header("state 1: selected")]
        [Header("state 0: unselected")]
        public UICollection<UIItem> tabs;

        private int _selectedTab = -1;

        protected void Update() {
            var pageCount = pageCollection.PageCount;
            if (!IsSync()) {
                tabs.Count = 0;
                for (int i = 0; i < pageCount; i++) {
                    pageCollection.GetPage<MonoBehaviour>(i, out int resId);
                    var ui = tabs.AddElement(resId);
                    ui.tag = i;
                    ui.buttons[0].OnClick -= ButtonClick;
                    ui.buttons[0].OnClick += ButtonClick;
                }
            }
            int currentPage = pageCollection.CurrentPage;
            if (currentPage != _selectedTab) {
                HideSelectedTab();
                _selectedTab = currentPage;
                tabs[_selectedTab].SetState(1);
            }
        }
        private bool IsSync() {
            if (tabs.Count != pageCollection.PageCount) {
                return false;
            }
            for(int i = 0; i < tabs.Count; i++) {
                pageCollection.GetPage<MonoBehaviour>(i, out int index);
                if (tabs.GetResourceId(i) != index) {
                    return false;
                }
            }
            return true;
        }
        private void ButtonClick(UIItem arg1, int arg2) {
            pageCollection.SetPorgressAnimating(arg1.tag);
        }

        private void HideSelectedTab() {
            if (_selectedTab >= 0) {
                if (tabs.Count > _selectedTab) {
                    tabs[_selectedTab].SetState(0);
                }
            }
        }
        private void OnEnable() {
            HideSelectedTab();
            _selectedTab = -1;
        }
    }
}