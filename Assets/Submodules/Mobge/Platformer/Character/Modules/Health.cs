using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Platformer.Character {

    [Serializable]
    public struct Health
    {
        [SerializeField]
        private float _max;
        private float _current;
        public float Max {
            get => _max;
            set{
                _max = value;
                Current = _current;
            }
        }
        public Health(float max) {
            _max = max;
            _current = max;
        }
        public float Current {
            get => _current;
            set{
                _current = Mathf.Clamp(value, 0, _max);
            }
        }
        public bool Alive{
            get{ return _current != 0; }
        }
        public bool Full {
            get => Max == Current;
        }
        public float Percentage {
            get => _current / _max;
            set {
                Current = _max * value;
            }
        }
    }
}
