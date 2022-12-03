using System;
using System.Collections.Generic;
using Facebook.Unity;
using Mobge.HyperCasualSetup;
using UnityEngine;

namespace Mobge.Telemetry {
    public class TelemetryModule {
        protected BaseLevelPlayer player;
        private bool _finished = true;
        private static readonly Dictionary<string, object> FacebookSdkParameterCache = new Dictionary<string, object>();
        protected GameContextWithAnalytics Context => player.Context as GameContextWithAnalytics;
        private int CurrentLevelNumber {
            get {
                if (player.Context.MenuManager.TryGetLastOpenedLinearIndex(out int index)) {
                    return index + 1;
                }
                return player.Context.MenuManager.LastOpenedId.Value;
            }
        }
        private string CurrentLevelName => player.Context.LevelData[player.Context.MenuManager.LastOpenedId].Asset.name;
        private ALevelSet.ID LevelId => player.Context.MenuManager.LastOpenedId;
        private float Score => player.Score;
        public void TrackLevel(BaseLevelPlayer player, bool autoFireEvents = true) {
            this.player = player;
            if (autoFireEvents) {
                LevelStart();
                this.player.OnLevelFinish -= HandleLevelResult;
                this.player.OnLevelFinish += HandleLevelResult;
            }
        }
        private void HandleLevelResult(BaseLevelPlayer arg1, BaseLevelPlayer.LevelFinishParams @params) {
            if (@params.success) {
                LevelSuccess();
            }
            else {
                LevelFail();
            }
        }
        private bool IsTelemetryDisabled() {
            if (Application.isEditor) return true;
            return false;
        }
        public void LevelStart(bool forced = false) {
            if (!_finished && !forced) {
                return;
            }
            _finished = false;
            FireLevelStartEvent();
        }
        protected virtual void FireLevelStartEvent() {
            try {
                Context.GetAnalytics<AnalyticsReferences>().levelStarted.FireEvent();
                if (IsTelemetryDisabled()) return;
                Debug.Log("telemetry start: " + CurrentLevelNumber + " " + CurrentLevelName);
                FacebookSdkParameterCache.Clear();
                FacebookSdkParameterCache["level_index"] = CurrentLevelNumber;
                FacebookSdkParameterCache["level_name"] = CurrentLevelName;
                FB.LogAppEvent("fb_mobile_level_started", null, FacebookSdkParameterCache);
            }
            catch (Exception e) {
                Debug.LogError("Exception at telemetry level start: ");
                Debug.LogError(e);
            }
        }
        public void LevelFail(bool forced = false) {
            if (_finished && !forced) return;
            _finished = true;
            FireLevelFailEvent();
        }
        public void LevelSuccess(bool forced = false, Dictionary<string, string> optParamsForEvent = null) {
            if (_finished && !forced) return;
            _finished = true;
            FireLevelSuccessEvent(optParamsForEvent);
        }
        protected virtual void FireLevelSuccessEvent(Dictionary<string, string> optParamsForEvent = null) {
            try {
                Context.GetAnalytics<AnalyticsReferences>().levelCompleted.FireEvent(optParamsForEvent);
                if (IsTelemetryDisabled()) return;
                Debug.Log("telemetry success: " + CurrentLevelNumber + " " + CurrentLevelName);
                FacebookSdkParameterCache.Clear();
                FacebookSdkParameterCache["level_index"] = CurrentLevelNumber;
                FacebookSdkParameterCache["level_name"] = CurrentLevelName;
                FacebookSdkParameterCache["Score"] = Score;
                FB.LogAppEvent("fb_mobile_level_achieved", null, FacebookSdkParameterCache);
            }
            catch (Exception) {
                Debug.LogError("Exception at telemetry level success");
            }
        }
        protected virtual void FireLevelFailEvent() {
            try {
                Context.GetAnalytics<AnalyticsReferences>().levelFailed.FireEvent();
                if (IsTelemetryDisabled()) return;
                Debug.Log("telemetry fail: " + CurrentLevelNumber + " " + CurrentLevelName);
                FacebookSdkParameterCache.Clear();
                FacebookSdkParameterCache["level_index"] = CurrentLevelNumber;
                FacebookSdkParameterCache["level_name"] = CurrentLevelName;
                FacebookSdkParameterCache["Score"] = Score;
                FB.LogAppEvent("fb_mobile_level_failed", null, FacebookSdkParameterCache);
            }
            catch (Exception) {
                Debug.LogError("Exception at telemetry level fail");
            }
        }
    }
}