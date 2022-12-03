using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mobge.UI {
    public class SwipeNavigationControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler {



        [OwnComponent, SerializeField] private CanvasGroup group;

        private const int c_noPointerId = -109210;

        private bool _previousPressed;

        private PointerEventData _lastEvent;
        private Canvas _canvas;
        private RectTransform _tr;
        private RectTransform _canvasTr;
        private int _pressedId = c_noPointerId;


        public float edgeInput = 5f;
        public float sensitivity = 0.1f;

        public RectTransform swipeArea;

        private Vector2 _lastPointer;
        private Vector2 _input;

        [OwnComponent] public RectTransform visual;

        /// <summary>
        /// Returns whether a pointer is pressed.
        /// </summary>
        public bool Pressed {
            get => _pressedId != c_noPointerId;
        }
        /// <summary>
        /// Returns wheter the pointer is pressed in this Update. Can be called from Update function (not LateUpdate).
        /// </summary>
        public bool IsDown {
            get {
                return !_previousPressed && Pressed;
            }
        }
        /// <summary>
        /// Returns wheter the pointer is released in this Update. Can be called from Update function (not LateUpdate).
        /// </summary>
        public bool IsUp {
            get {
                return _previousPressed && !Pressed;
            }
        }

        public Vector2 PointerPosition {
            get {
                return ToLocal(_canvasTr, _tr, _lastEvent.position);
            }
        }

        private Camera Camera {
            get {
                var c = _canvas.worldCamera;
                if (c == null) {
                    c = Camera.main;
                }
                return c;
            }
        }
        public Vector2 Input => _input;
        public virtual bool Enabled {
            get => group.interactable;
            set {
                group.interactable = value;
                group.blocksRaycasts = value;
                if (!value) {
                    Release();
                }
            }
        }
        private bool VisualEnabled {
            get => visual ? visual.gameObject.activeSelf : false;
            set {
                if (visual) {
                    visual.gameObject.SetActive(value);
                }
            }
        }
        protected void Awake() {
            _canvas = GetComponentInParent<Canvas>();
            _tr = (RectTransform)transform;
            _canvasTr = (RectTransform)_canvas.transform;
            VisualEnabled = false;
            if (_canvas.worldCamera == null) {
                _canvas.worldCamera = Camera.main;
            }
        }

        private void Release() {
            _pressedId = c_noPointerId;
        }
        protected void Update() {


            if (Pressed) {
                var pp = PointerPosition;


                if (IsDown) {
                    _lastPointer = pp;
                }
                var sRect = swipeArea.rect;
                var sPos = ToLocal(_canvasTr, swipeArea, _lastEvent.position);

                Vector2 diff = (pp - _lastPointer) * sensitivity;
                _lastPointer = pp;

                if (sPos.x < sRect.xMin) {
                    diff.x = -this.edgeInput;
                }
                else if (sPos.x > sRect.xMax) {
                    diff.x = this.edgeInput;
                }
                if (sPos.y < sRect.yMin) {
                    diff.y = -this.edgeInput;
                }
                else if (sPos.y > sRect.yMax) {
                    diff.y = this.edgeInput;
                }
                _input = Vector2.LerpUnclamped(_input, diff, MathExtensions.CalculateLerpAmount(0.2f, Time.deltaTime));

            }
            else {
                _input = Vector2.zero;
            }
        }
        protected void LateUpdate() {

            _previousPressed = Pressed;
        }

        private void UpdateVisual(Vector3 localPosition) {
            if (visual) {
                visual.localPosition = localPosition;
            }
        }
        public Ray PointerRay {
            get {
                return Camera.ScreenPointToRay(_lastEvent.position);
            }
        }
        public Ray PressPointerRay {
            get {
                return Camera.ScreenPointToRay(_lastEvent.pressPosition);
            }
        }
        private static Vector2 ToLocal(RectTransform canvasTransform, RectTransform tr, Vector2 screenPosition) {
            return Joystick.ToLocal(canvasTransform, tr, screenPosition);
        }

        void IDragHandler.OnDrag(PointerEventData eventData) {
            if (eventData.pointerId == _pressedId) {
                UpdateVisual(ToLocal(_canvasTr,_tr, eventData.position));
            }
        }
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData) {
            if (!Pressed) {
                eventData.useDragThreshold = false;
                _lastEvent = eventData;
                _pressedId = eventData.pointerId;
                VisualEnabled = true;
                UpdateVisual(ToLocal(_canvasTr, _tr, eventData.position));

            }
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
            if (eventData.pointerId == _pressedId) {
                _lastEvent = null;
                Release();
                VisualEnabled = false;
            }
        }

    }
}