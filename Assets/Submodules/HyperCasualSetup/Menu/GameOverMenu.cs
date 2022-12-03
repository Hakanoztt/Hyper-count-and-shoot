using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mobge.UI;

namespace Mobge.HyperCasualSetup.UI {
    public class GameOverMenu : Mobge.UI.BaseMenu {
        public Action<GameOverMenu> onMenuClicked;
        public Action<Mobge.UI.BaseMenu> openNextLevel;
        public Func<GameOverMenu, bool> onRestartClicked;
        public string scoreFormat = "x {0}";
        public string challengeFormat = "x {0}";
        public MenuManager.LevelEndResults Results { get; private set; }

        [SerializeField] private TextReference _scoreText;
        [SerializeField] private AReusableItem _scoreEffect;
        [SerializeField] private TextReference _challengeText;
        [SerializeField] private AReusableItem _challengeEffect;

        public ref TextReference ScoreText => ref _scoreText;

        [SerializeField, OwnComponent] private UnityEngine.UI.Button goToMenuButton;
        [SerializeField, OwnComponent] private UnityEngine.UI.Button goToNextLevel;
        [SerializeField, OwnComponent] private UnityEngine.UI.Button restartLevelButton;

        public Button MenuButton => goToMenuButton;
        protected new void Awake() {
            base.Awake();
            goToMenuButton?.onClick.AddListener(GoToMenuAction);
            goToNextLevel?.onClick.AddListener(NextLevelAction);
            restartLevelButton?.onClick.AddListener(RestartLevelAction);
        }

        public void GoToMenuAction() {
            onMenuClicked?.Invoke(this);
        }

        private void NextLevelAction() {
            NextLevelAction(this);
        }
        public void NextLevelAction(Mobge.UI.BaseMenu topMenu) {
            openNextLevel?.Invoke(topMenu);
        }

        public void RestartLevelAction() {
            if(MenuManager is HyperCasualSetup.UI.MenuManager mm) {

                mm.CurrentPlayer.FinishGame(false, 0);
            }
            else {
                onRestartClicked?.Invoke(this);
            }
        }
        public void Prepare(AGameContext gameContext, MenuManager.LevelEndResults currentLevelInfo) {
            Results = currentLevelInfo;
            var result = Results.MergedResult;
            _scoreText.Text = string.Format(scoreFormat, result.score.ToString());
            _challengeText.Text = string.Format(challengeFormat, result.levelChallenge.ToString());
            if (_scoreEffect != null) {
                _scoreEffect.Play();
            }
            if (_challengeEffect != null) {
                _challengeEffect.Play();
            }
        }
    }
}