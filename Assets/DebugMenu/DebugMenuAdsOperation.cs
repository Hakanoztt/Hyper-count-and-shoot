using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.DebugMenu {

    public class DebugMenuAdsOperation : MonoBehaviour, IDebugMenuExtension {
        public Button rewardedButton, interstitialButton;
        public void Init(DebugMenu debugMenu) {
            debugMenu.OnMenuOpened += OnMenuOpen;
        }

        private void OnMenuOpen() {
            rewardedButton.onClick.RemoveAllListeners();
            rewardedButton.onClick.AddListener(RewardedButtonOnClick);
            interstitialButton.onClick.RemoveAllListeners();
            interstitialButton.onClick.AddListener(InterButtonOnClick);
        }

        private void InterButtonOnClick() {

        }

        private void RewardedButtonOnClick() {

        }
    }
}