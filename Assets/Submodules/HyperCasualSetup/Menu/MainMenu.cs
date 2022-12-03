using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup
{
    public class MainMenu : Mobge.UI.BaseMenu
    {
        [OwnComponent] public Button playButton;
        [OwnComponent] public Button levelsButton;
        [OwnComponent] public Button marketButton;
        public Action<MainMenu> onPlayGame;
        public Action<MainMenu> openLevelsMenu;
        public Action<MainMenu> openMarket;

        protected new void Awake() {
            base.Awake();
            playButton.onClick.AddListener(PlayButtonAction);
            if(marketButton != null) {
                marketButton.onClick.AddListener(MarketButtonAction);
            }
            if (levelsButton != null) {
                levelsButton.onClick.AddListener(LevelsButtonAction);
            }
        }

        private void MarketButtonAction() {
            openMarket?.Invoke(this);
        }

        private void LevelsButtonAction() {
            openLevelsMenu?.Invoke(this);
        }

        private void PlayButtonAction() {
            onPlayGame?.Invoke(this);
        }
    }
}