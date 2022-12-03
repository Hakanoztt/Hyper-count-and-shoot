using System;
using UnityEngine;

namespace Mobge.Platformer.Character{
    [Serializable]
    public struct Poise {
        [SerializeField]
        private float _max;
        private float _current;
        private float _lastSetTime;
        private float _revocerRate;
        public float Max {
            get => _max;
            set{
                _max = value;
            }
        }
        public void Reset(float recoverRate) {
            this._revocerRate = recoverRate;
            SetCurrent(_max);
        }
        public void Reset() {
            SetCurrent(_max);
        }
        /** <summary>
         * Decrease the poise by specified amount. Returns true if depleted.
         * Sets the poise to max amount if depleted.
         * </summary>
         * <param name="amount">Specified amount</param>
         * <param name="recoverRate">Recover speed of poise</param>
         */
        public bool DecreasePoise(float amount) {
            SetCurrent(GetCurrent() - amount);
            if(_current == 0) {
                _current = _max;
                return true;
            }
            return false;
        }
        private void SetCurrent(float value) {
            
            _current = Mathf.Max(value, 0);
            _lastSetTime = Time.fixedTime;
            
        }
        private float GetCurrent() {
            return Math.Min(_current + _revocerRate * (Time.fixedTime - _lastSetTime), _max);
        }

    }
}