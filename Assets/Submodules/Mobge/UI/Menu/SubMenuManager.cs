using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.UI {
    public class SubMenuManager<T> where T : BaseMenu {
        private T _menu;
        private bool _isMenuOpen;
        private bool _animPlaying;
        private BaseMenu _parentMenu;
        private bool _isCurrentlyOpen;
        public bool IsMenuOpen => _isMenuOpen;

        public T Menu => _menu;

        public SubMenuManager(T menuRes, BaseMenu parentMenu) : this(menuRes, parentMenu.MenuManager) {
            _parentMenu = parentMenu;
            _parentMenu.onOpen += ParentOpen;
            _parentMenu.onClose += ParentClose;
        }
        public SubMenuManager(T menuRes, MenuManager manager) {

            _menu = UnityEngine.Object.Instantiate(menuRes);
            manager.EnsureRegistration(_menu);
        }

        private void ParentOpen(BaseMenu openedMenu) {
            UpdateCurrentStateDirect();
        }

        private void ParentClose(BaseMenu closedMenu) {
            UpdateCurrentStateDirect();
        }

        public void SetEnabled(bool enabled) {
            if (enabled == _isMenuOpen) {
                return;
            }
            _isMenuOpen = enabled;

            UpdateCurrentStateDirect();
        }

        private void MenuAnimFinished(bool complete, object data) {
            _animPlaying = false;
            UpdateCurrentStateDirect();
        }


        private void UpdateCurrentStateDirect() {
            if (_animPlaying) {
                return;
            }
            bool targetOpen = this._isMenuOpen;

            if (_parentMenu != null) {
                if(_parentMenu.CurrentState != BaseMenu.State.Open) {
                    targetOpen = false;
                }
            }

            if (targetOpen != _isCurrentlyOpen) {
                _animPlaying = true;
                _isCurrentlyOpen = targetOpen;
                if (_isCurrentlyOpen) {
                    _menu.Prepare();
                    _menu.SetEnabledWithAnimation(true, MenuAnimFinished);
                }
                else {
                    _menu.SetEnabledWithAnimation(false, MenuAnimFinished);
                }
            }
        }

        enum NextAction {
            None = 0,
            Open,
            Close,
        }
    }
}