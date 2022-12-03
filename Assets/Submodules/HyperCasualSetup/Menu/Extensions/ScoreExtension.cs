using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI {
    public class ScoreExtension : MonoBehaviour, BaseMenu.IExtension {
        [Header("{3}: total player's score including level score")]
        [Header("{2}: total player's score")]
        [Header("{1}: level total available score")]
        [Header("{0}: level score")]
        public string formatString = "{0}/{1}";
        public bool subscribeLevelScoreChanges;

        public TextReference scoreText;
        private RoutineManager.Routine _currentAnimation;
        private Action<ScoreExtension, float> _updateCallback;
        private Action<ScoreExtension, bool> _onEndCallback;
        private float _lastProgress;
        private float[] _startScores;
        private float[] _targetScores;

        private RoutineManager _routineManager;
        private bool _initialized = false;

        private MenuManager _menuManager;

        private void Update() {
            if (_routineManager != null) {
                _routineManager.Update(Time.unscaledDeltaTime);
            }
        }
        void BaseMenu.IExtension.Prepare(BaseMenu menu) {
            if (!_initialized) {
                _initialized = true;
                _startScores = new float[3];
                _targetScores = new float[3];
                _routineManager = new RoutineManager();
            }
            _menuManager = (MenuManager)menu.MenuManager;
            UpdateScores();
            if (subscribeLevelScoreChanges) {
                var level = _menuManager.CurrentPlayer;
                if (level != null) {
                    level.OnScoreChange -= UpdateFromPlayer;
                    level.OnScoreChange += UpdateFromPlayer;
                }
            }
        }

        private void UpdateFromPlayer(BaseLevelPlayer arg1, float arg2) {
            UpdateScores();
        }

        private void UpdateTargets() {
            _targetScores[0] = _menuManager.CurrentPlayer ? _menuManager.CurrentPlayer.Score : 0f;
            _targetScores[1] = _menuManager.CurrentPlayer ? _menuManager.CurrentPlayer.TotalScore : 0f;
            _targetScores[2] = _menuManager.Context.GameProgressValue.TotalScore;
        }
        public void UpdateScoresWithAnimation(float time, Action<ScoreExtension, float> update, Action<ScoreExtension, bool> onFinish) {
            _currentAnimation.Stop();
            UpdateTargets();
            _currentAnimation = _routineManager.DoAction(OnAnimEnd, time, UpdateAnimProgress);
            _updateCallback = update;
            _onEndCallback = onFinish;
        }
        private float CalculateCurrent(int index) {
            return Mathf.LerpUnclamped(_startScores[index], _targetScores[index], _lastProgress);
        }
        private void OnAnimEnd(bool completed, object data) {
            if (!completed) {
                for (int i = 0; i < _startScores.Length; i++) {
                    _startScores[i] = CalculateCurrent(i);
                }
            }
            else {

                CopyToStart();
            }
            _updateCallback = null;
            if (_onEndCallback != null) {
                _onEndCallback(this, completed);
                _onEndCallback = null;
            }
        }

        private void CopyToStart() {
            for (int i = 0; i < _startScores.Length; i++) {
                _startScores[i] = _targetScores[i];
            }
        }

        private void UpdateAnimProgress(float progress, object data) {
            _lastProgress = progress;
            UpdateScores(CalculateCurrent(0), CalculateCurrent(1), CalculateCurrent(2));
            if (_updateCallback != null) {
                _updateCallback(this, _lastProgress);
            }
        }

        private void UpdateScores(float levelScore, float levelTotalAvailable, float userTotal) {
            scoreText.Text = string.Format(formatString, Mathf.RoundToInt(levelScore), Mathf.RoundToInt(levelTotalAvailable), Mathf.RoundToInt(userTotal), Mathf.RoundToInt(userTotal + levelScore));
        }
        public void SetToCustomValue(int parameterIndex, float value) {
            _targetScores[parameterIndex] = value;
            UpdateScores(_targetScores[0], _targetScores[1], _targetScores[2]);

        }
        public void UpdateScores() {
            UpdateTargets();
            CopyToStart();
            UpdateScores(_targetScores[0], _targetScores[1], _targetScores[2]);
        }
    }
}