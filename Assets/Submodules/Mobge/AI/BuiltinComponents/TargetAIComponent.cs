using Mobge.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.StateMachineAI {
    public class TargetAIComponent : MonoBehaviour, IAITarget {
        private Transform _target;
        private Vector3 _targetPos;

        public Vector3 WorldTarget {
            get {
                if (_target != null) {
                    return _target.TransformPoint(_targetPos);
                }
                return _targetPos;
            }
        }


        protected void Awake() {
            Reset();
        }

        public void SetTarget(Transform target, Vector3 relativePos) {
            this._target = target;
            _targetPos = relativePos;
        }

        public void SetTarget(Transform target) {
            this._target = target;
            _targetPos = Vector3.zero;
        }

        public void SetTarget(Vector3 worldPos) {
            this._target = null;
            _targetPos = worldPos;
        }

        public void Reset() {
            _target = transform;
            _targetPos = Vector3.zero;
        }
    }

}