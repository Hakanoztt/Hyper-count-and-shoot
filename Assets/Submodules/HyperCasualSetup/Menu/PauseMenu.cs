using Mobge.Animation;
using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI
{
    public class PauseMenu : Mobge.UI.BaseMenu
    {
        [OwnComponent, SerializeField] private Button _restartButton;
        [OwnComponent, SerializeField] private Button _mainMenuButton;
        [OwnComponent, SerializeField] private Button _continueButton;

        [SerializeField] private TextReference _scoreText;
        [SerializeField] private TextReference _challengeText;


        public Action<PauseMenu> onContinueClicked;
        public Action<PauseMenu> onMenuClicked;
        public Func<PauseMenu, bool> onRestartClicked;

        protected new void Awake() {
            base.Awake();
            if (_restartButton) {
                _restartButton.onClick.AddListener(RestartAction);
            }
            if (_mainMenuButton) {
                _mainMenuButton.onClick.AddListener(MainMenuAction);
            }
            if (_continueButton) {
                _continueButton.onClick.AddListener(ContinueAction);
            }
        }

        public void Prepare(BaseLevelPlayer player) {
            _challengeText.Text = player.LevelChallenge.ToString();
            _scoreText.Text = player.Score.ToString();
        }
        private void ContinueAction() {
            onContinueClicked?.Invoke(this);
        }

        private void MainMenuAction() {
            onMenuClicked?.Invoke(this);
        }

        private void RestartAction() {
            onRestartClicked?.Invoke(this);
        }
    }
}