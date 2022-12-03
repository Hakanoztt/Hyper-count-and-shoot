using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI
{
    public class GameHud : Mobge.UI.BaseMenu
    {

        [OwnComponent, SerializeField] private Button pauseButton;
        [OwnComponent, SerializeField] private Button restartButton;
        [SerializeField] private TextReference _scoreText;
        [SerializeField] private AReusableItem _scoreEffect;
        [SerializeField] private TextReference _challengeText;
        [SerializeField] private AReusableItem _challengeEffect;

        public Action<GameHud> onRestart;
        public Action<GameHud> onPause;

        private uint _currentSession;

        protected new void Awake() {
            base.Awake();
            if (pauseButton != null) {
                pauseButton.onClick.AddListener(PauseAction);
            }
            if (restartButton != null) {
                restartButton.onClick.AddListener(RestartAction);
            }
        }


        protected virtual void Prepare(BaseLevelPlayer player) {
            player.OnScoreChange -= ScoreChanged;
            player.OnScoreChange += ScoreChanged;
            player.OnLevelChallengeChange -= ChallengeChanged;
            player.OnLevelChallengeChange += ChallengeChanged;
            _challengeText.Text = player.LevelChallenge.ToString();
            _scoreText.Text = player.Score.ToString();
        }

        public override void Prepare() {
            var pl = ((MenuManager)MenuManager).CurrentPlayer;
            this.Prepare(pl);
            base.Prepare();
            _currentSession = pl.Session;
        }

        private void ChallengeChanged(BaseLevelPlayer arg1, int arg2) {

            _challengeText.Text = arg2.ToString();
            if (_scoreEffect != null) {
                _scoreEffect.Play();
            }
        }
        private void ScoreChanged(BaseLevelPlayer arg1, float arg2) {
            _scoreText.Text = Mathf.FloorToInt(arg2).ToString();
            if (_challengeEffect != null) {
                _challengeEffect.Play();
            }
        }

        public void PauseAction() {
            if (((MenuManager)MenuManager).PauseGame(this)) {
                if (onPause != null) {
                    onPause(this);
                }
            }
        }

        private void RestartAction() {
            if (((MenuManager)MenuManager).LevelRestart(this)) {
                if (onRestart != null) {
                    onRestart(this);
                }
            }
        }
    }
}