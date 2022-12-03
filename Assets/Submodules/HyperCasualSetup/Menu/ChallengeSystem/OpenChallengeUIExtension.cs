using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI.ChallengeSystem {
    public class OpenChallengeUIExtension : MonoBehaviour, BaseMenu.IExtension {
        public ChallengeListUI challengeMenuRes;
        [OwnComponent] public Button openMenuButton;


        private ChallengeListUI _challengeMenu;
        private uint _lastSession;
        private HyperCasualSetup.UI.MenuManager _menuManager;
        private BaseMenu _menu;

        public MenuManager MenuManager => _menuManager;
        public BaseMenu Menu =>_menu;

        void BaseMenu.IExtension.Prepare(BaseMenu menu) {
            _menu = menu;
            _menuManager = (HyperCasualSetup.UI.MenuManager)menu.MenuManager;
            var session = _menuManager.CurrentPlayer.Session;
            if (_lastSession != session) {
                _lastSession = session;
                NewSessionStarted();
            }
        }
        protected virtual void NewSessionStarted() {
            openMenuButton.onClick.RemoveListener(OpenMenu);
            openMenuButton.onClick.AddListener(OpenMenu);
        }

        protected void OpenMenu() {
            var m = ChallengeMenuSafe;
            _menuManager.PushMenu(m);
        }

        protected ChallengeListUI ChallengeMenuSafe {
            get {
                if (_challengeMenu == null) {
                    _challengeMenu = Instantiate(challengeMenuRes);
                    _challengeMenu.backButton.onClick.AddListener(ChallengeMenuBack);
                }
                return _challengeMenu;
            }
        }

        private void ChallengeMenuBack() {
            _menuManager.PopMenuControlled(_challengeMenu);
        }

        private void OnDestroy() {
            if (_challengeMenu) {
                _challengeMenu.gameObject.DestroySelf();
                _challengeMenu = null;
            }
        }
    }

}