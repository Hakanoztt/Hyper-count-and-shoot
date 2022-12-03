using ElephantSDK;
using Mobge.HyperCasualSetup;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Mobge.Telemetry {

    [CreateAssetMenu(fileName = "AnalyticsReferences", menuName = "Hyper Casual/AnalyticsEventCollection", order = 0)]

    [System.Serializable]
    public class AnalyticsReferences : ScriptableObject {

        private byte _version;

        public Byte Version => _version;

        public AGameContext Context { get; set; }
        public RemoteConfig RemoteConfig { get; private set; }

        protected void OnEnable() {
            RemoteConfig = ElephantSDK.RemoteConfig.GetInstance();
            _version++;
        }

        public AnalyticsEvent levelStarted;
        public AnalyticsEvent levelFailed;
        public AnalyticsEvent levelCompleted;
        public IntRemoveVariable InterstitialFirst;
        public IntRemoveVariable InterstitialFrequency;
        public IntRemoveVariable InterstitialCooldownAfterSuccessfulRewarded;
        public IntRemoveVariable RateUsFirst;
        public IntRemoveVariable RateUsFrequency;

        [Serializable]
        public struct RemoteVariable<T> {
            [SerializeField] private T defaultValue;
            private byte _version;
            public bool IsFetched(byte version) {
                return _version == version;
            }
            public T Value { get; private set; }
            public T DefaultValue => defaultValue;
            public void SetValue(T value, byte version) {
                _version = version;
                Value = value;
            }


        }
    }

    [System.Serializable]
    public struct AnalyticsEvent {

        public byte Version => analytics.Version;
        public static string CollectionFieldName => nameof(analytics);
        public static string NameFieldName => nameof(name);
        public AnalyticsReferences Analytics => analytics;

        [SerializeField] private string name;
        [SerializeField] private AnalyticsReferences analytics;
        public void FireEvent(Dictionary<string,string> extraProperties = null, int levelIndex = -1) {
            if (analytics == null) {
                return;
            }
            analytics.Context.FireAnalyticsEvent(name, extraProperties, levelIndex);
        }
        public void FireWithSuffix(string suffix, Dictionary<string, string> extraProperties = null, int levelIndex = -1) {
            if (analytics == null) {
                return;
            }
            var name = this.name;
            if (!string.IsNullOrEmpty(suffix)) {
                name += suffix;
            }
            analytics.Context.FireAnalyticsEvent(name, extraProperties, levelIndex);
        }
        public void ShowRewardedAdd(Action<AGameContext.ClaimResult> handleResult) {
            if (analytics == null) {
                return;
            }
            analytics.Context.ClaimReward(name, handleResult);
        }
        public void ShowRewardedAddWithSuffix(string suffix, Action<AGameContext.ClaimResult> handleResult) {
            if (analytics == null) {
                return;
            }
            var name = this.name;
            if (!string.IsNullOrEmpty(suffix)) {
                name += suffix;
            }
            analytics.Context.ClaimReward(name, handleResult);
        }

        public override string ToString() {
            return name;
        }

    }

    [System.Serializable]
    public struct IntRemoveVariable {

        [SerializeField] private AnalyticsEvent name;
        [SerializeField] private AnalyticsReferences.RemoteVariable<int> variable;
        public int Value {
            get {
                if (name.Analytics == null) {
                    return variable.DefaultValue;
                }
                if (!variable.IsFetched(name.Version)) {
                    variable.SetValue(name.Analytics.RemoteConfig.GetInt(name.ToString(), variable.DefaultValue),name.Version);
                }
                return variable.Value;
            }
        }
    }
    [Serializable]
    public struct FloatRemoteVariable {

        [SerializeField] private AnalyticsEvent name;
        [SerializeField] private AnalyticsReferences.RemoteVariable<float> variable;
        public float Value {
            get {
                return GetValue(variable.DefaultValue);
            }
        }
        public float GetValue(float overrideDefault) {

            if (name.Analytics == null) {
                return overrideDefault;
            }
            if (!variable.IsFetched(name.Version)) {
                variable.SetValue(name.Analytics.RemoteConfig.GetFloat(name.ToString(), overrideDefault), name.Version);
            }
            return variable.Value;
        }
    }
    [Serializable]
    public struct StringRemoteVariable {

        [SerializeField] private AnalyticsEvent name;
        [SerializeField] private AnalyticsReferences.RemoteVariable<string> variable;
        public string Value {
            get {
                if (name.Analytics == null) {
                    return variable.DefaultValue;
                }
                if (!variable.IsFetched(name.Version)) {
                    variable.SetValue(name.Analytics.RemoteConfig.Get(name.ToString(), variable.DefaultValue), name.Version);
                }
                return variable.Value;
            }
        }
    }

    [Serializable]
    public struct IntArrayRemoteVariable {
        [SerializeField] private AnalyticsEvent name;
        [SerializeField] private AnalyticsReferences.RemoteVariable<int[]> variable;
        public int[] Value {
            get {
                if (name.Analytics == null) {
                    return variable.DefaultValue;
                }
                if (!variable.IsFetched(name.Version)) {
                    FetchVariable();
                }
                return variable.Value;
            }
        }

        private void FetchVariable() {
            string remoteValue = name.Analytics.RemoteConfig.Get(name.ToString(), null);
            if (string.IsNullOrEmpty(remoteValue)) {
                variable.SetValue(variable.DefaultValue, name.Version);
            }
            else {
                var splitted = remoteValue.Split(',');
                int[] remoteArray = new int[splitted.Length];
                for (int i = 0; i < splitted.Length; i++) {
                    if (int.TryParse(splitted[i], out int result)) {
                        remoteArray[i] = result;
                    }
                    else {
                        remoteArray[i] = variable.DefaultValue.Length > i ? variable.DefaultValue[i] : 0;
                    }
                }
                variable.SetValue(remoteArray, name.Version);
            }
        }
    }
    [Serializable]
    public struct FloatArrayRemoteVariable {
        [SerializeField] private AnalyticsEvent name;
        [SerializeField] private AnalyticsReferences.RemoteVariable<float[]> variable;
        public float[] Value {
            get {
                if (name.Analytics == null) {
                    return variable.DefaultValue;
                }
                if (!variable.IsFetched(name.Version)) {
                    FetchVariable();
                }
                return variable.Value;
            }
        }

        private void FetchVariable() {
            string remoteValue = name.Analytics.RemoteConfig.Get(name.ToString(), null);
            if (string.IsNullOrEmpty(remoteValue)) {
                variable.SetValue(variable.DefaultValue, name.Version);
            }
            else {
                var splitted = remoteValue.Split(',');
                float[] remoteArray = new float[splitted.Length];
                for (int i = 0; i < splitted.Length; i++) {
                    if (float.TryParse(splitted[i], out float result)) {
                        remoteArray[i] = result;
                    }
                    else {
                        remoteArray[i] = variable.DefaultValue.Length > i ? variable.DefaultValue[i] : 0;
                    }
                }
                variable.SetValue(remoteArray, name.Version);
            }
        }
    }

}