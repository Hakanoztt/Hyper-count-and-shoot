using Mobge.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.StateMachineAI {
    public abstract class AIObjectDetector<T> : MonoBehaviour, IAITarget, IAnimatorOwner, IAIComponent {

        public StateAI ai;

        private static List<Transform> s_tempRemoveColliders = new List<Transform>();

        public string targetTag;
        public LayerMask layerMask = -1;
        [AnimatorIntParameter] public int countParameter;

        public bool removeInvalidsImmediately;
        public float updateCooldown = 0.2f;

        private float _lastUpdateTime;

        private Dictionary<Transform, Pair> _objects;

        private KeyValuePair<Transform, Pair> _choosenObject;

        public Vector3 WorldTarget {
            get {
                var tr = ChooseTarget(out _);
                if(tr == null) {
                    return ai.transform.position;
                }
                return tr.position;
            }
        }
        Animator IAnimatorOwner.GetAnimator() {
            return ai != null ? ai.animator : null;
        }
        public abstract bool TryGetObject(Collider tr, out T t);
        public abstract bool IsValid(T t);

        public virtual void OnAIEnable(bool enabled) {
            if (_objects == null) {
                _objects = new Dictionary<Transform, Pair>();
            }
            _objects.Clear();
            _choosenObject = default;
            _lastUpdateTime = 0;
        }


        public virtual Transform ChooseTarget(out T t) {
            if (_choosenObject.Key != null && !IsValid(_choosenObject.Value.value)) {
                DoUpdate();
            }
            t = _choosenObject.Value.value;
            return _choosenObject.Key;
        }

        protected void OnTriggerEnter(Collider other) {
            if (other.isTrigger) {
                return;
            }
            if ((layerMask.value & (0x1 << other.gameObject.layer)) == 0x0) {
                return;
            }
            if (_objects.ContainsKey(other.transform)) {
                return;
            }
            if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag)) {
                return;
            }
            if (TryGetObject(other, out var t)) {
                if (_objects.Count == 0) {
                    enabled = true;
                }
                _objects.Add(other.transform, new Pair(other, t));
            }
        }
        protected void FixedUpdate() {
            if(_objects.Count == 0) {
                enabled = false;
                ai.animator.SetInteger(this.countParameter, 0);
                return;
            }
            if (_lastUpdateTime + updateCooldown <= Time.fixedTime) {
                DoUpdate();
            }
        }

        private void DoUpdate() {
            float fTime = Time.fixedTime;
            if(fTime == _lastUpdateTime) {
                return;
            }
            _lastUpdateTime = Time.fixedTime;
            CleanUpAndChoose(out int count);
            ai.animator.SetInteger(this.countParameter, count);

        }

        protected void OnTriggerExit(Collider other) {
            _objects.Remove(other.transform);
        }

        protected void CleanUpAndChoose(out int count) {
            _choosenObject = default;
            float minDis = float.PositiveInfinity;
            Vector3 thisPos = this.ai.transform.position;
            var en = _objects.GetEnumerator();
            count = 0;
            while (en.MoveNext()) {
                var c = en.Current;
                if (c.Key == null || !c.Value.collider.gameObject.activeInHierarchy) {
                    s_tempRemoveColliders.Add(c.Key);
                }
                else {
                    if (IsValid(c.Value.value)) {
                        count++;
                        var dis = thisPos - c.Key.position;
                        float sqrMag = dis.sqrMagnitude;
                        if (sqrMag < minDis) {
                            minDis = sqrMag;
                            _choosenObject = c;
                        }
                    }
                    else if (removeInvalidsImmediately) {
                        s_tempRemoveColliders.Add(c.Key);
                    }
                }
            }
            en.Dispose();
            for(int i = 0; i < s_tempRemoveColliders.Count; i++) {
                var c = s_tempRemoveColliders[i];
                _objects.Remove(c);
            }
            s_tempRemoveColliders.Clear();
        }

        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }


        private struct Pair {
            public Collider collider;
            public T value;

            public Pair(Collider collider, T value) {
                this.collider = collider;
                this.value = value;
            }
        }

        public struct Enumerator {
            private AIObjectDetector<T> _detector;
            private Dictionary<Transform, Pair>.Enumerator _en;
            private T _value;
            private Transform _tr;
            public Enumerator(AIObjectDetector<T> detector) {
                _detector = detector;
                _en = _detector._objects.GetEnumerator();
                _value = default;
                _tr = null;
            }

            public bool MoveNext() {
                while(_en.MoveNext()) {
                    _value = _en.Current.Value.value;
                    _tr = _en.Current.Key;
                    if (_detector.IsValid(_value)) {
                        return true;
                    }
                }
                return false;
            }
            public T Current => _value;
            public Transform CurrentKey =>_tr;
            public void Dispose() {
                _en.Dispose();
            }
        }


    }
}