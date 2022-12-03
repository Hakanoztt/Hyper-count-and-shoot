using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Animation
{
    [Serializable]
    public struct Curve
    {
        public static string c_keyArrayName => nameof(_keys);
        [SerializeField]
        private Keyframe[] _keys;
        public Keyframe[] Keys => _keys;

        public bool IsValid { get => _keys != null; }
        private AnimationCurve _curve;
        public void EnsureInit(bool forced = false) {
            if (_curve == null || forced) {
                _curve = ToAnimationCurve();
            }
        }
        public float TotalTime {
            get {
                return _keys[_keys.Length - 1].time;
            }
        }
        public Curve(Keyframe[] keys) {
            this._keys = keys;
            _curve = null;
        }
        public float Evaluate(float time) {
            return _curve.Evaluate(time);
        }

        public static Curve New() {
            Curve c;
            c._keys = new Keyframe[0];
            c._curve = null;
            return c;
        }
        public AnimationCurve ToAnimationCurve() {
            UnityEngine.Keyframe[] kf = new UnityEngine.Keyframe[_keys.Length];
            for (int i = 0; i < _keys.Length; i++) {
                kf[i] = _keys[i];
            }
            AnimationCurve ac = new AnimationCurve(kf);
            return ac;
        }
        public void UpdateKeys(AnimationCurve curve) {
            var keys = curve.keys;
            _curve = curve;
            // Debug.Log(_keys.Length + " => " + keys.Length);
            Array.Resize(ref _keys, keys.Length);
            for (int i = 0; i < keys.Length; i++) {
                _keys[i] = keys[i];
            }
        }
    }
    [Serializable]
    public struct Keyframe
    {
        [SerializeField] public float time;
        [SerializeField] public float value;
        [SerializeField] public float inTangent;
        [SerializeField] public float outTangent;

        public Keyframe(float time, float value, float inTangent, float outTangent) {
            this.time = time;
            this.value = value;
            this.inTangent = inTangent;
            this.outTangent = outTangent;
        }
        // [SerializeField] private int m_TangentMode;
        // [SerializeField] private int m_WeightedMode;
        // [SerializeField] private float m_InWeight;
        // [SerializeField] private float m_OutWeight;
        public static implicit operator UnityEngine.Keyframe(Keyframe kf) {
            return new UnityEngine.Keyframe(kf.time, kf.value, kf.inTangent, kf.outTangent);
        }
        public static implicit operator Keyframe(UnityEngine.Keyframe kf) {
            return new Keyframe(kf.time, kf.value, kf.inTangent, kf.outTangent);
        }
    }
}