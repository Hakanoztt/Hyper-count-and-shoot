using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Mobge.HyperCasualSetup {

    [CreateAssetMenu(menuName = "Mobge/Audio Mixer Control")]
    public class AudioMixerControl : ScriptableObject {
        private const float c_minValue = 0.0001f;
        private const string c_dataPrefix = "audio:";

        public AudioMixer[] mixers;
        public AssetReference[] addressableMixers;
        public string music = "Music";
        public string fx = "FX";
        public string master = "Master";

        [NonSerialized] private bool _isLoaded = false;
        public bool IsLoaded => _isLoaded;

        private Dictionary<string, float> _cachedValues = new Dictionary<string, float>();

        private AGameContext _context;
        private bool _autoSave = true;

        public void Initialize(AGameContext context) {
            _context = context;
            for (int i = 0; i < addressableMixers.Length; i++) {
                var a = addressableMixers[i];
                a.LoadAssetAsync<AudioMixer>().Completed += MixerLoaded;
            }
            UpdateAll();
        }

        private void MixerLoaded(AsyncOperationHandle<AudioMixer> obj) {
            var e = _cachedValues.GetEnumerator();
            while (e.MoveNext()) {
                var p = e.Current;
                obj.Result.SetFloat(p.Key, p.Value);
            }
            _isLoaded = true;
        }

        public void UpdateAll() {
            var progress = _context.GameProgressValue;
            AutoSave = false;
            Music = progress.GetFloat(c_dataPrefix + music, 1);
            FX = progress.GetFloat(c_dataPrefix + fx, 1);
            Master = progress.GetFloat(c_dataPrefix + master, 1);
            AutoSave = true;
        }
        private void SaveAll() {

            var val = _context.GameProgressValue;
            val.SetFloat(c_dataPrefix + music, Music);
            val.SetFloat(c_dataPrefix + fx, FX);
            val.SetFloat(c_dataPrefix + master, Master);
            _context.GameProgress.SaveValue(val);
        }
        private float GetMixerValue(string key) {
            _cachedValues.TryGetValue(key, out float value);
            return value;
        }
        private void SetMixerValue(string key, float value) {
            _cachedValues[key] = value;
            for(int i = 0; i   < mixers.Length; i++) {
                mixers[i].SetFloat(key, value);
            }
            for(int i = 0; i < addressableMixers.Length; i++) {
                var a = addressableMixers[i];
                if (a.IsDone) {
                    var m = (AudioMixer)a.Asset;
                    if (m) {
                        m.SetFloat(key, value);
                    }
                }
            }
        }
        public float Music {
            get => GetValue(music);
            set => SetValue(music, value);
        }
        public float FX {
            get => GetValue(fx);
            set => SetValue(fx, value);
        }
        public float Master {
            get => GetValue(master);
            set => SetValue(master, value);
        }
        public float MusicRaw {
            get => GetMixerValue(music);
            set => SetMixerValue(music, value);
        }
        public float FXRaw {
            get => GetMixerValue(fx);
            set => SetMixerValue(fx, value);
        }
        public float MasterRaw {
            get => GetMixerValue(master);
            set => SetMixerValue(master, value);
        }
        private float GetValue(string key) {
            float val = GetMixerValue(key);
            return (Mathf.Pow(10, val * (1f / 20f)) - c_minValue) * (1f / (1f - c_minValue));
        }
        public bool AutoSave {
            get => _autoSave;
            set {
                if(_autoSave != value) {
                    _autoSave = value;
                    if (_autoSave) {
                        SaveAll();
                    }
                }
            }
        }
        private void SetValue(string key, float value) {
            SetMixerValue(key, Mathf.Log10(value * (1f - c_minValue) + c_minValue) * 20f);
            if (_autoSave) {
                SaveAll();
            }
        }
    }
}