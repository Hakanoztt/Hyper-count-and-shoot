using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.UI {
    public class UIImageTransition : MonoBehaviour {
        private Stack<Pointer> _parameterPool;
        private ActionManager _actionManager;

        protected void Awake() {
            _parameterPool = new Stack<Pointer>();
            _actionManager = new ActionManager();
        }

        protected void Update() {
            _actionManager.Update(Time.unscaledDeltaTime);
        }
        public ActionManager.Action TranslateImage(in TransitionParameters @params) {
            Pointer p;
            if (_parameterPool.Count > 0) {
                p = _parameterPool.Pop();
            }
            else {
                p = new Pointer(transform);
            }
            p.value = @params;
            return _actionManager.DoTimedAction(@params.time, UpdateAnim, OnAnimFinish, p);

        }

        private void UpdateAnim(in ActionManager.UpdateParams @params) {
            var p = (Pointer)@params.data;
            var target = p.value.target.GetBounds(p.value.simulationParent);
            Bounds b = Lerp(p.value.source, target, @params.progress);
            p.ApplyBounds(b);

        }
        private Bounds Lerp(Bounds b1, Bounds b2, float progress) {
            return new Bounds(Vector3.LerpUnclamped(b1.center, b2.center, progress), Vector3.LerpUnclamped(b1.size, b2.size, progress));
        }

        private void OnAnimFinish(object data, bool completed) {
            var p = (Pointer)data;
            p.Reset();
            _parameterPool.Push(p);
        }


        public class Pointer {
            public TransitionParameters value;
            public SpriteRenderer instance;
            public Pointer(TransitionParameters value,Transform parent) {
                this.value = value;
                InitInstance(parent);
            }
            public Pointer(Transform parent) {
                InitInstance(parent);
            }
            private void InitInstance(Transform parent) {
                instance = new GameObject("-").AddComponent<SpriteRenderer>();
                instance.transform.SetParent(parent, false);
                instance.gameObject.SetActive(false);
            }
            public void Reset() {
                value = default;
                instance.sprite = null;
            }
            public void ApplyBounds(in Bounds b) {
                var tr = instance.transform;
                tr.localPosition = b.center;
                var size = instance.sprite.rect.size / instance.sprite.pixelsPerUnit;
                var scl = b.size / size;
                if (value.fitInFrame) {
                    var min = Mathf.Min(scl.x, scl.y);
                    scl = new Vector2(min, min);
                }
                tr.localScale = new Vector3(scl.x, scl.y, scl.x);
            }
        }
        public struct TransitionParameters {
            public Bounds source;
            public Endpoint target;
            public Transform simulationParent;
            public Sprite sprite;
            public bool fitInFrame;
            public object data;
            public Action<object> onComplete;
            public float time;
        }
        public struct Endpoint {
            private RectTransform _transform;
            private Bounds _lastBounds;
            public Endpoint(Transform parent, RectTransform transform) {
                _transform = transform;
                _lastBounds = default;
                _lastBounds = CalculateBounds(parent);
            }

            private Bounds CalculateBounds(Transform parent) {
                var center = _transform.position;
                var size = _transform.TransformVector(_transform.rect.size);
                var pivot = _transform.TransformVector(_transform.rect.position);


                pivot += size * 0.5f;
                return InverseTransformBounds(new Bounds(center + pivot, size), parent);
            }
            private Bounds TransformBounds(Bounds bounds, Transform parent) {
                if (parent) {
                    bounds.center = parent.TransformPoint(bounds.center);
                    bounds.size = parent.TransformVector(bounds.size);
                }
                return bounds;
            }
            private Bounds InverseTransformBounds(Bounds bounds, Transform parent) {
                if (parent) {
                    bounds.center = parent.InverseTransformPoint(bounds.center);
                    bounds.size = parent.InverseTransformVector(bounds.size);
                }
                return bounds;
            }


            public Endpoint(Bounds localBounds) {
                _transform = null;
                _lastBounds = localBounds;
            }
            public Bounds GetBounds(Transform parent) {
                if(_transform != null) {
                    _lastBounds = CalculateBounds(parent);
                }
                return _lastBounds;
            }
        }
    }
}