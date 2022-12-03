using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ElephantSDK;
using Mobge.HyperCasualSetup;
using UnityEngine;

namespace Mobge.Telemetry {
    public class GameContextWithAnalytics : GameContext {

        [SerializeField] protected AnalyticsReferences analyticsReferences;
        public IAdManager adManager;

        protected new void Awake() {
            base.Awake();
            adManager = GetComponent<IAdManager>();
        }


        public T GetAnalytics<T>() where T : AnalyticsReferences {
            var t= analyticsReferences as T;
            t.Context = this;
            return t;
        }

        public override void ShowInterstitial(string eventName) {
            if (adManager != null) {
                adManager.ShowInterstitial(null, eventName);
            }
        }

        public override void ClaimReward(string eventName, Action<ClaimResult> onStateChanged) {
            if (adManager != null) {
                FireAnalyticsEvent(eventName);
                if(!adManager.ShowRewarded(null,
                    (RewardedResult) => {
                        switch (RewardedResult) {
                            case RewardedResult.Finished:
                                onStateChanged(ClaimResult.Claimed);
                                break;
                            case RewardedResult.Skipped:

                                onStateChanged(ClaimResult.Canceled);
                                break;
                            case RewardedResult.Failed:
                                onStateChanged(ClaimResult.Failed);
                                break;
                            default:
                                break;
                        }
                    }
                )) {
                    base.ClaimReward(eventName, onStateChanged);
                }
            }
            else {
                base.ClaimReward(eventName, onStateChanged);
            }
        }

        public override void FireAnalyticsEvent(string eventName, Dictionary<string, string> extraParams = null, int levelIndex = -1) {
            if (Application.isEditor) {
                Debug.Log("FireAnalyticsEvent: " + eventName);
                return;
            }
            if (string.IsNullOrEmpty(eventName)) return;
            var parameters = Params.New();
            int levelNumber = 0;

            if (levelIndex != -1) {
                levelNumber = levelIndex + 1;
            }
            else {
                var hasLi = MenuManager.TryGetLastOpenedLinearIndex(out int index);
                if (hasLi) levelNumber = index + 1;
            }

            var levelName = MenuManager.Context.LevelData[MenuManager.Context.MenuManager.LastOpenedId].Asset.name;
            parameters.Set("levelName", levelName);
            if (extraParams != null) {
                var e = extraParams.GetEnumerator();
                while (e.MoveNext()) {
                    parameters.Set(SanitizeWhitespace(e.Current.Key), SanitizeWhitespace(e.Current.Value));
                }
                e.Dispose();
            }
            Elephant.Event(SanitizeWhitespace(eventName), levelNumber, parameters);
        }

        private string SanitizeWhitespace(string s) => Regex.Replace(s, @"[ ]+", "_");
    }
}
