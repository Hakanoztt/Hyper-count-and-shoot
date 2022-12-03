using Mobge.Animation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mobge.UI {
    public class SliderControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IAnimatorOwner {
        private const int c_noPointerId = -109210;
        [OwnComponent] public RectTransform limits;
        public Animator animator;
        [AnimatorState] public int dragProgressAnimation;
        [OwnComponent, SerializeField] private CanvasGroup group;
        public Direction direction = Direction.Horizontal;
        public bool offsetRelativeToTouchDown;

        private float _offset;

        private int _pressedId = c_noPointerId;

        private float _startRatio;

        public float Value { get; private set; }

        public virtual bool Enabled {
            get => group.interactable;
            set {
                group.interactable = value;
                if (!value) {
                    Release();
                }
            }
        }

        [NonSerialized]
        private Canvas _canvas;
        private Canvas Canvas {
            get {
                if (_canvas == null) {
                    _canvas = GetComponentInParent<Canvas>();
                }
                return _canvas;
            }
        }

        public bool Pressed {
            get => _pressedId != c_noPointerId;
        }


        protected void Awake() {

        }

        void IDragHandler.OnDrag(PointerEventData eventData) {
            if (eventData.pointerId == _pressedId) {
                UpdateProgress(eventData);
            }
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData){
            if (!Pressed) {
                _pressedId = eventData.pointerId;
                if (this.offsetRelativeToTouchDown) {
                    _startRatio = ToRatio(eventData);
                }
                else {
                    UpdateProgress(eventData);
                }
            }
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
            if (eventData.pointerId == _pressedId) {
                Release();
            }
        }

        private void Release() {
            _pressedId = c_noPointerId;
            UpdateRatio(0.5f);
        }
        private void UpdateProgress(PointerEventData eventData) {
            float ratio = ToRatio(eventData, _startRatio);
            UpdateRatio(ratio);
        }
        protected void Update() {
            UpdateAnimation();
        }
        private void UpdateRatio(float ratio) {
            ratio -= 0.5f;
            ratio *= 2f;
            Value = ratio;
            UpdateAnimation();
        }

        private void UpdateAnimation() {

            if (animator) {
                if (animator.isInitialized) {
                    animator.Play(dragProgressAnimation, 0, Value * 0.5f + 0.5f);
                    animator.Update(0);
                    animator.speed = 0;
                }
                else {
                }

            }
        }

        float ToRatio(PointerEventData data, float offsetRatio = 0.5f) {
            
            var local = ToLocal(data);
            var rect = limits.rect;
            float min;
            float pos;
            float size;
            switch (direction) {
                default:
                case Direction.Horizontal:
                    min = rect.x;
                    pos = local.x;
                    size = rect.width;
                    break;
                case Direction.Vertical:
                    min = rect.y;
                    pos = local.y;
                    size = rect.height;
                    break;
            }
            float offset = (pos - min) / size;
            offset = Mathf.Clamp01(offset - (offsetRatio - 0.5f));
            return offset;

        }
        Vector2 ToLocal(PointerEventData data) {

            var cam = Canvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(limits, data.position, cam, out Vector2 localPoint);
            return localPoint;
        }

        Animator IAnimatorOwner.GetAnimator() {
            return animator;
        }

        public enum Direction {
            Horizontal = 0,
            Vertical = 1,
        }
    }
}