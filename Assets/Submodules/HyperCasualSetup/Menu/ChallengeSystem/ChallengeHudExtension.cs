using Mobge.HyperCasualSetup;
using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI.ChallengeSystem {
    public class ChallengeHudExtension : MonoBehaviour, BaseMenu.IExtension {
        public CanvasGroup root;
        public Image progressImage;
        
        public TextReference leftTime;
        public string leftTimeFormat = "{0}";
        [OwnComponent] public RectTransform timerRoot;

        public TextReference score;
        public string scoreFormat = "{0}";
        public AReusableItem challengeChangedEffect;
        public AReusableItem timerEndEffect;

        private HyperCasualSetup.UI.MenuManager _menuManager;
        private uint _lastSession;
        private bool TimerEnabled {
            get => timerRoot.gameObject.activeSelf;
            set {
                if (value != TimerEnabled) {
                    timerRoot.gameObject.SetActive(value);
                }
            }
        }
        public void Prepare(BaseMenu menu) {
            _menuManager = (HyperCasualSetup.UI.MenuManager)menu.MenuManager;
            var pl = _menuManager.CurrentPlayer;
            if (_lastSession != pl.Session) {
                _lastSession = pl.Session;
                var data = ChallengeGameComponent.GetFromPlayer(pl);
                if (data != null) {
                    root.gameObject.SetActive(true);
                    pl.OnLevelChallengeChange += ChallengeChanged;
                    if (data.TimerEnabled) {
                        UpdateTimerVisual(data, 0);
                        data.OnTimerUpdated += UpdateTimerVisual;
                        data.OnTimerEnded += TimerEnded;
                    }
                    UpdateChallengeVisual(pl);
                }
                else {
                    root.gameObject.SetActive(false);
                }
            }
        }

        private void TimerEnded(ChallengeGameComponent.Data data) {
            if (timerEndEffect != null) {
                timerEndEffect.Play();
            }
        }

        private void ChallengeChanged(BaseLevelPlayer arg1, int arg2) {
            UpdateChallengeVisual(arg1);
            if (challengeChangedEffect) {
                challengeChangedEffect.Play();
            }
        }

        private void UpdateChallengeVisual(BaseLevelPlayer pl) {
            score.Text = string.Format(scoreFormat, pl.LevelChallenge, pl.TotalChallenge);
            if (progressImage != null) {
                progressImage.fillAmount = Mathf.Min(1f, pl.LevelChallenge / (float)pl.TotalChallenge);
            }
        }
        private void UpdateTimerVisual(ChallengeGameComponent.Data data, float leftTime) {
            this.leftTime.Text = string.Format(this.leftTimeFormat, data.timeLimit - leftTime);
        }

    }
}