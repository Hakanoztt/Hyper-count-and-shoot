using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame {
    [Serializable]
    public class PopupReference<T> where T : BaseMenu {
        public T menuRes;

        private SubMenuManager<T> _menu;

        public T GetInstance(MenuManager manager) {
            EnsureLoad(manager);
            if (_menu == null) {
                return null;
            }
            else {
                return _menu.Menu;
            }
        }

        public void EnsureLoad(MenuManager manager) {
            if (menuRes == null) {
                return;
            }
            if (_menu == null) {
                _menu = new SubMenuManager<T>(menuRes, manager);
            }
        }

        public void SetVisibility(MenuManager manager, bool visible) {
            EnsureLoad(manager);
            if (_menu != null) {
                _menu.SetEnabled(visible);
            }
        }
        public enum Type {
            Null,
            NotLoaded,
            Ready
        }
    }
}