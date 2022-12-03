using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mobge.UI
{
    public class Joystick : UIBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private Vector2 _axis;
        public Vector2 Axis {
            get => _axis; 
            private set {
                _axis = value;
            }
        }
        public bool Pressed { get; private set; }
        public bool IsDragging { get; private set; }
        public float radius;
        public float followFingerRadius;
        public Vector2 visualOffset;
        public float thumbMovementRate = 1f;
        [OwnComponent]
        public RectTransform backgroundVisual, thumbVisual;
        private bool _visualsEnabled = true;
        private RectTransform _rtr;
        [NonSerialized]
        private Canvas _canvas;
        private Vector2 _origin;
        public bool visibleWhileResting;
        public Vector2 visualsRestPosition;

        public RectTransform Transform => _rtr;
        public Vector2 LocalOrigin => _origin;
        private Canvas Canvas {
            get {
                if (_canvas == null) {
                    _canvas = GetComponentInParent<Canvas>();
                    if(_canvas!=null && _canvas.gameObject == gameObject && _canvas.worldCamera == null) {
                        _canvas.worldCamera = Camera.main;
                    }
                }
                return _canvas;
            }
        }
        public Ray WorldOriginRay {
            get {
                var cam = Canvas.worldCamera;
                if (cam == null) {
                    cam = Camera.main;
                }
                var wO = transform.TransformPoint(_origin);
                if (cam) {
                    var o = cam.transform.position;
                    return new Ray(o, wO - o);
                }
                return new Ray(wO, Vector3.back);
            }
        }

        protected override void Awake() {
            base.Awake();
            _rtr = (RectTransform)transform;
            UpdateVisibility();
        }
        public bool VisualsEnabled {
            get => _visualsEnabled;
            set {
                if (value != _visualsEnabled) {
                    _visualsEnabled = value;
                    UpdateVisibility();
                }
            }
        }
        public event ReleaseHandler OnRelease;
        //protected void Update() {
        //    Matrix4x4 m = _rtr.localToWorldMatrix;
        //    //Debug.DrawLine(m.MultiplyPoint(Vector3.zero), m.MultiplyPoint(Vector3.zero) + Vector3.up, Color.red);
        //}
        public static Vector2 ToLocal(RectTransform canvasTransform, Transform tr, Vector2 screenPosition) {
            Vector3 relative = new Vector3(screenPosition.x / Screen.width, screenPosition.y / Screen.height);
            var rect = canvasTransform.rect;
            relative = new Vector3(rect.x + relative.x * rect.width, rect.y + relative.y * rect.height);
            if (canvasTransform != tr) {
                Vector3 world = canvasTransform.TransformPoint(relative);
                relative = tr.InverseTransformPoint(world);
            }
            return relative;
        }
        Vector2 ToLocal(PointerEventData data) {
            return ToLocal(((RectTransform)Canvas.transform), transform, data.position);
        }
        void IDragHandler.OnDrag(PointerEventData eventData) {
            IsDragging = true;
            var newPos = ToLocal(eventData);
            var dif = newPos - _origin;
            var difMag = dif.magnitude;
            if(difMag == 0) {
                return;
            }
            var dir = dif / difMag;
            if (difMag > followFingerRadius) {
                _origin = newPos - dir * followFingerRadius;
            }
            RefreshVisuals();
            if (difMag > radius) {
                Axis = dir;
            }
            else {
                Axis = dir * (difMag / radius);
            }
        }
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData) {
            if (Pressed) {
                return;
            }
            Pressed = true;
            UpdateVisibility();
            _origin = ToLocal(eventData);
            Axis = Vector2.zero;

            RefreshVisuals();
            eventData.useDragThreshold = false;
        }

        public Vector3 VisualCenter {
            get => _origin + visualOffset;
        }
        void RefreshVisuals() {
            if (backgroundVisual) {
                backgroundVisual.localPosition = VisualCenter;
            }
            if (thumbVisual) {
                thumbVisual.localPosition = VisualCenter + (Vector3)((radius * thumbMovementRate) * Axis);
            }
        }


        void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
            Axis = Vector2.zero;
            Pressed = false;
            UpdateVisibility();
            bool drag = IsDragging;
            IsDragging = false;
            OnRelease?.Invoke(drag);
        }
        private void UpdateVisibility() {
            bool isActive;
            bool goToRestingPosition = false;
            if (this.visibleWhileResting) {
                isActive = _visualsEnabled;
                goToRestingPosition = !(Pressed && _visualsEnabled);

            }
            else {
                isActive = Pressed && _visualsEnabled;
            }
            if (backgroundVisual) {
                backgroundVisual.gameObject.SetActive(isActive);
                if (goToRestingPosition) {
                    backgroundVisual.anchoredPosition = visualsRestPosition;
                }
            }
            if (thumbVisual) {
                thumbVisual.gameObject.SetActive(isActive);
                if (goToRestingPosition) {
                    thumbVisual.anchoredPosition = visualsRestPosition;
                }
            }
        }


        public delegate void ReleaseHandler(bool isDragged);
    }
}