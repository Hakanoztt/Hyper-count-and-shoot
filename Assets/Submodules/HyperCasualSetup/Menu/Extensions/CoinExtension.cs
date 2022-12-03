using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Globalization;
using Mobge.HyperCasualSetup.Components;
using Mobge.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI {

    [Obsolete("This component is obsolate. Use " + nameof(ScoreExtension) + " instead.", false)]
    public class CoinExtension : MonoBehaviour, BaseMenu.IExtension {
        [OwnComponent(true)] public Text scoreText;
        [OwnComponent] public CanvasGroup group;
        [OwnComponent, Obsolete, Header("Obsolete")] public Image image;
        public AnimationCurve fadeCurve;
        public bool ignoreScoreChanges;
        [Header("{3}: total player's score minus current level score")]
        [Header("{2}: total player's score")]
        [Header("{1}: level total available score")]
        [Header("{0}: level score")]
        public string formatString = "{0}/{1}";

        private ActionManager _actionManager;

        private float _lastCoinCollectedTime = -100f;
        private MenuManager _menuManager;
        private float _lastScore;
        private ActionManager.Action _coinAction;

        public float CurrentAmount { get => _lastScore; set => UpdateScore(value); }
        public float PlayersTotal => _menuManager.Context.GameProgressValue.TotalScore;
        protected void Awake() {
            _actionManager = new ActionManager();
        }
        public void Prepare(BaseMenu menu) {
            _menuManager = menu.MenuManager as MenuManager;
            if (_menuManager == null) return;
            var player = _menuManager.CurrentPlayer;

            if (!ignoreScoreChanges) {
                player.OnScoreChange += ScoreChanged;
            }
            UpdateScore(player.Score);
        }
        private void UpdateScore(float score) {
            _lastScore = score;
            var p = _menuManager.CurrentPlayer;
            var playersTotal = PlayersTotal;
            this.scoreText.text = string.Format(formatString, _lastScore, p.TotalScore, playersTotal, playersTotal - _lastScore);
        }
        public void UpdateScore() {
            if (_menuManager) {
                UpdateScore(_lastScore);
            }
        }
        private void ScoreChanged(BaseLevelPlayer arg1, float arg2) {
            UpdateScore(_menuManager.CurrentPlayer.Score);
            _lastCoinCollectedTime = Time.time;
        }
        public bool ChangeCoinsWithAnimation(float changeAmount, float animationTime, ActionManager.ActionComplete onFinish = null, Action<float> amountChanged = null) {
            if (!_coinAction.IsFinished()) {
                return false;
            }
            float startScore = _lastScore;
            float endScore = _lastScore + changeAmount;
            _coinAction = _actionManager.DoTimedAction(animationTime, (in ActionManager.UpdateParams p) => {
                float score = Mathf.Round(Mathf.LerpUnclamped(startScore, endScore, p.progress));
                UpdateScore(score);
                if (amountChanged != null) {
                    amountChanged(score - startScore);
                }
            }, onFinish);
            return true;
        }

        public bool ChangeValueWithAnimation(float newValue, float animationTime, ActionManager.ActionComplete onFinish = null, Action<float> amountChanged = null) {
            if (!_coinAction.IsFinished()) {
                return false;
            }
            float startScore = _lastScore;
            float endScore = newValue;
            _coinAction = _actionManager.DoTimedAction(animationTime, (in ActionManager.UpdateParams p) => {
                float score = Mathf.Round(Mathf.LerpUnclamped(startScore, endScore, p.progress));
                UpdateScore(score);
                if (amountChanged != null) {
                    amountChanged(score - startScore);
                }
            }, onFinish);
            return true;
        }
        private void Update() {
            _actionManager.Update(Time.unscaledDeltaTime);
            float a = fadeCurve.Evaluate(Time.time - _lastCoinCollectedTime);
            Color color;
            if (group) {
                group.alpha = a;
            }
            else {
                if (image) {
                    color = image.color;
                    color.a = a;
                    image.color = color;

                }
                color = scoreText.color;
                color.a = a;
                scoreText.color = color;
            }

        }
    }
}
