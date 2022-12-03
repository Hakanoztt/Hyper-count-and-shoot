using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mobge.UI { 
    public class SinglePointerControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler{

        private const int c_noPointerId = -109210;

        [OwnComponent, SerializeField] private CanvasGroup group;
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

        [OwnComponent] public RectTransform visual;

        private bool _previousPressed;
        private PointerEventData _lastEvent;
        private int _pressedId = c_noPointerId;
        private Canvas _canvas;
        private RectTransform _canvasTr;

        private RectTransform _tr;

        private Camera Camera {
            get {
                var c = _canvas.worldCamera;
                if (c == null) {
                    c = Camera.main;
                }
                return c;
            }
        }

        public Ray PointerRay {
            get {
                //var rc = _lastEvent.pointerCurrentRaycast;
                //return new Ray(rc.worldPosition, rc.worldNormal);
                
                return Camera.ScreenPointToRay(_lastEvent.position);
            }
        }
        public Ray PressPointerRay {
            get {
                //var rc = _lastEvent.pointerPressRaycast;
                //return new Ray(rc.worldPosition, rc.worldNormal);
                return Camera.ScreenPointToRay(_lastEvent.pressPosition);
            }
        }
        private Vector2 ToLocal(Vector2 screenPosition) {
            Vector3 relative = new Vector3(screenPosition.x / Screen.width, screenPosition.y / Screen.height);
            var rect = _canvasTr.rect;
            relative = new Vector3(rect.x + relative.x * rect.width, rect.y + relative.y * rect.height);
            Vector3 world = _canvas.transform.TransformPoint(relative);
            Vector3 local = _tr.InverseTransformPoint(world);
            //Debug.Log(relative + "\n" + world + "\n" + local);
            return local;
            //RectTransformUtility.ScreenPointToLocalPointInRectangle(_tr, screenPosition, Camera, out Vector2 localPoint);
            //return localPoint;
        }
        private void UpdateVisual(Vector3 localPosition) {
            if (visual) {
                visual.localPosition = localPosition;
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
        public Vector2 PointerPosition {
            get {
                return ToLocal(_lastEvent.position);
            }
        }
        public Vector2 PressPosition {
            get {
                return ToLocal(_lastEvent.pressPosition);
            }
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
        /// <summary>
        /// Returns whether a pointer is pressed.
        /// </summary>
        public bool Pressed {
            get => _pressedId != c_noPointerId;
        }


        protected void Awake() {
            _canvas = GetComponentInParent<Canvas>();
            _tr = (RectTransform)transform;
            _canvasTr = (RectTransform)_canvas.transform;
            VisualEnabled = false;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData) {
            if (!Pressed) {
                eventData.useDragThreshold = false;
                _lastEvent = eventData;
                //Debug.Log(PointerRay);
                _pressedId = eventData.pointerId;
                VisualEnabled = true;
                UpdateVisual(ToLocal(eventData.position));
            }
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
            if (eventData.pointerId == _pressedId) {
                _lastEvent = null;
                Release();
                VisualEnabled = false;
            }
        }
        void IDragHandler.OnDrag(PointerEventData eventData) {
            if (eventData.pointerId == _pressedId) {
                UpdateVisual(ToLocal(eventData.position));
            }
        }

        private void Release() {
            _pressedId = c_noPointerId;
        }

        protected void LateUpdate() {
            _previousPressed = Pressed;
        }

    }
}