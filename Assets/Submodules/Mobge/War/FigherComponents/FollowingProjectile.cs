using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mobge.War {
    public class FollowingProjectile : MonoBehaviour {
        public float speed;
        public Rigidbody rb;
        public float maxLifeTime = 100;
        public ReusableReference onHitEffect;

        private Action<FollowingProjectile, IDamagable> _onFinish;
        private IDamagable _target;

        private float _fireTime;
        private Vector3 _relativeOffset;


        private Vector3 WorldTarget {
            get => _target.Collider.transform.TransformPoint(_relativeOffset);
        }

        public void Fire(IDamagable damagable, Action<FollowingProjectile, IDamagable> onFinish) {
            if (_target != null) {
                throw new Exception("Cannot fire a " + GetType() + " which has already been fired before it finishes.");
            }
            _onFinish = onFinish;
            _target = damagable;
            _relativeOffset = _target.Collider.GetLocalCenter();
            enabled = true;
            _fireTime = Time.fixedTime;
        }


        protected void FixedUpdate() {
            if (_target == null) {
                enabled = false;
                return;
            }
            if(_fireTime + maxLifeTime <= Time.fixedTime) {
                _target = null;
                enabled = false;
                FireOnFinish(null);
                return;
            }
            else {
                UpdateFollow();
            }
        }

        private void FireOnFinish(IDamagable damagable) {
            if (_onFinish != null) {
                var a = _onFinish;
                _onFinish = null;
                a(this, damagable);
            }
        }

        private void UpdateFollow() {
            var target = WorldTarget;
            var dt = Time.fixedDeltaTime;
            var pos = rb.position;
            float step = dt * speed;
            Vector3 dif = target - pos;
            float distanceSqr = dif.sqrMagnitude;
            if(distanceSqr < step * step) {
                rb.velocity = Vector3.zero;
                var dam = _target;
                _target = null;
                FireOnFinish(dam);
            }
            else {
                rb.velocity = dif / Mathf.Sqrt(distanceSqr) * step;
            }
        }

    }
}