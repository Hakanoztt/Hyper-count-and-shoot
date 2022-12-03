using Mobge.Animation;
using Mobge.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI.ChallengeSystem {

    public class ChallengeGameOverExtension : MonoBehaviour, BaseMenu.IExtension {

        public TextReference currentScore;
        public TextReference bestScore;
        public Collection prizes;
        public ChallengeData data;
        [OwnComponent] public Button replayButton;
        [OwnComponent] public Button noThanksButton;
        [OwnComponent] public Animator animator;
        [AnimatorState] public int openingState;
        public AReusableItem bestScoreBrokeEffect;
        public float takePrizeDelay = 0.4f;

        public string prizeFormat = "{0}";
        public string prizeLimitFormat = "{0}";

        public string bestScoreFormat = "{0}";
        public string currentScoreFormat = "{0}";

        public string replayClaimKey;

        public int freeTicketPerNewLevel = 1;

        public string levelEnsScoreEvent = "challenge_level_score";

        private uint _session;
        private GameOverMenu _menu;
        private AnimatorAction _animatorAction;

        private Queue<Action> _actions;

        private MenuManager MenuManager => (MenuManager)_menu.MenuManager;

        void BaseMenu.IExtension.Prepare(BaseMenu menu) {
            _menu = (GameOverMenu)menu;
            var session = MenuManager.CurrentPlayer.Session;
            if (_session != session) {
                _session = session;
                Initialize();
            }
        }

        public void PlayAnimation(int state, float speed = 1) {
            var aa = new AnimatorAction(state, speed);
            if (animator.isInitialized) {
                aa.Apply(animator);
            } else {
                _animatorAction = aa;
                enabled = true;
            }
        }

        private void Initialize() {
            PlayAnimation(openingState);
            if (replayButton) {
                replayButton.onClick.RemoveListener(ReplayAction);
                replayButton.onClick.AddListener(ReplayAction);
            }
            if (noThanksButton) {
                noThanksButton.onClick.RemoveListener(NoThanksAction);
                noThanksButton.onClick.AddListener(NoThanksAction);
            }
            _menu.onOpen -= OnOpen;
            _menu.onOpen += OnOpen;

            var oldRes = _menu.Results.OldResult;
            int oldScore = oldRes == null ? 0 : oldRes.levelChallenge;

            data.TryGetLevel(MenuManager.LastOpenedId, out var levelInfo);
            prizes.Count = levelInfo.scores.Length;
            prizes.ReverseOrder();
            for (int i = 0; i < levelInfo.scores.Length; i++) {
                var ui = prizes[i];
                var pData = levelInfo.scores[i];
                ui.SetState(oldScore >= pData.value? 1 : 0, 0);
                ui.textsTMPro[0].text = string.Format(this.prizeLimitFormat, pData.value);
                ui.textsTMPro[1].text = string.Format(this.prizeFormat, pData.prize.GetDisplayString(0));
                ui.images[0].sprite = pData.icon;
            }

            var merged = _menu.Results.MergedResult;
            int bestScore = merged == null ? 0 : merged.levelChallenge;
            var newResult = _menu.Results.NewResult;
            int score = newResult == null ? 0 : newResult.levelChallenge;
            this.bestScore.Text = string.Format(bestScoreFormat, bestScore);
            this.currentScore.Text = string.Format(currentScoreFormat, score);

            Dictionary<string, string> extraParams = new Dictionary<string, string>();
            extraParams["limit_score"] = levelInfo.scores[levelInfo.scores.Length - 1].value.ToString();
            extraParams["score"] = _menu.Results.NewResult.levelChallenge.ToString();

            MenuManager.Context.FireAnalyticsEvent(levelEnsScoreEvent, extraParams);
        }

        private void OnOpen(BaseMenu openedMenu) {
            if (_actions == null) {
                _actions = new Queue<Action>();
            } else {
                _actions.Clear();
            }

            _menu.Interactable = false;
            _menu.onOpen -= OnOpen;
            var result = _menu.Results.MergedResult;
            int bestScore = result == null ? 0 : result.levelChallenge;
            data.TryGetLevel(MenuManager.LastOpenedId, out var levelInfo);
            prizes.Count = levelInfo.scores.Length;
            float delay = 0;

            for (int i = 0; i < levelInfo.scores.Length; i++) {
                var ui = prizes[i];
                var pData = levelInfo.scores[i];
                int newState = bestScore >= pData.value ? 1 : 0;
                if (ui.CurrentState != newState) {
                    delay += takePrizeDelay;
                    _actions.Enqueue(() => {
                        ui.SetState(newState);
                        ui.PlayExtraAnim(0);
                        TakePrize(ui, pData.prize);
                    });
                }
            }

            var oldRes = _menu.Results.OldResult;
            int oldScore = oldRes == null ? 0 : oldRes.levelChallenge;
            if (bestScore > oldScore) {
                _actions.Enqueue(() => {
                    if (bestScoreBrokeEffect != null) {
                        bestScoreBrokeEffect.Play();
                    }
                });
            }
            ConsumeActionQueue();
        }

        private void ConsumeActionQueue() {
            if(_actions.Count == 0) {
                _menu.Interactable = true;
            } else {
                _menu.ActionManager.DoAction(DoNextAction, this.takePrizeDelay, null);
            }
        }

        private void DoNextAction(bool completed, object data) {
            if (completed) {
                _actions.Dequeue()();
                ConsumeActionQueue();
            }
        }

        private void TakePrize(UIItem item, PrizeData data) {
            if (item.TryGetEffect(0, out var effect)) {
                effect.Play();
            }
            var gData = MenuManager.Context.GameProgressValue;
            data.ApplyToData(MenuManager.Context, gData, MenuManager.CurrentPlayer.Score, 0);
            MenuManager.Context.GameProgress.SaveValue(gData);
            _menu.Prepare();
        }

        private void NoThanksAction() {
            MenuManager.Context.ShowInterstitial("int_ad_impression_challenge");
            _menu.NextLevelAction(_menu);
        }

        private void DoReplay() {
            MenuManager.LevelRestart(null);
        }

        private void ReplayAction() {
            if (string.IsNullOrEmpty(replayClaimKey)) {
                DoReplay();
            } else {
                _menu.Interactable = false;
                MenuManager.Context.ClaimReward(replayClaimKey, (r) => {
                    _menu.Interactable = true;
                    if(r == AGameContext.ClaimResult.Claimed) {
                        DoReplay();
                    }
                });
            }
        }

        protected void Update() {
            if (_animatorAction.HasAnimation) {
                _animatorAction.Apply(animator);
            }
            enabled = false;
        }

        [Serializable]
        public class Collection : UICollection<UIItem> {}

        private struct AnimatorAction {
            public int state;
            public float speed;

            public AnimatorAction(int state, float speed) {
                this.state = state;
                this.speed = speed;
            }

            public bool HasAnimation => state != 0;

            public void Apply(Animator a) {
                a.Play(state, 0, 0f);
                a.Update(0);
                a.speed = speed;
                state = 0;
            }
        }
    }
}