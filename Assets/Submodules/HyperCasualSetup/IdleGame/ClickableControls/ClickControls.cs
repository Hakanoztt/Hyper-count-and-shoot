using Mobge.Core;
using Mobge.Core.Components;
using Mobge.HyperCasualSetup;
using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mobge.IdleGame {
    [HiddenOnEditor]
    public class ClickControls : MonoBehaviour, IComponentExtension, ICanvasRaycastFilter, IPointerDownHandler, IPointerUpHandler {
        private const string c_key = "clckCntrl";
        public const int c_nullPointerId = -(1 << 8);
        private static RaycastHit[] s_raycastBuffer = new RaycastHit[32];

        public static ClickControls Get(LevelPlayer player) {
            if(player.TryGetExtra<ClickControls>(c_key, out var c)) {
                return c;
            }
            return null;
        }


        public int layerMask = -1;
        public float mouseRadius = 0f;

        public IClickFilter clickFilter;

        [OwnComponent(true)] public Canvas canvas;

        private RectTransform _transform;

        private BaseLevelPlayer _player;

        private HashSet<AClickable> _clickables;

        private Raycaster _raycaster;

        private AClickable _pressedClickable;
        private int _pressedId = c_nullPointerId;

        void IComponentExtension.Start(in BaseComponent.InitArgs initData) {
            _player = (BaseLevelPlayer)initData.player;
            _player.SetExtra(c_key, this);
            _transform = (RectTransform)transform;
            _clickables = new HashSet<AClickable>();
            _raycaster.Init(this);
            canvas.worldCamera = Camera.main;
        }

        public void Register(AClickable clickable) {
            _clickables.Add(clickable);
        }
        public void UnRegister(AClickable clickable) {
            _clickables.Remove(clickable);
        }

        bool ICanvasRaycastFilter.IsRaycastLocationValid(Vector2 sp, Camera eventCamera) {
            var c = _raycaster.Raycast(this, sp);
            return c != null;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData) {
            if (_pressedId != c_nullPointerId) {
                return;
            }
            var c = _raycaster.Raycast(this, eventData.position);
            _pressedClickable = null;
            if (c != null) {
                if (clickFilter == null || clickFilter.ShouldClick(c)) {
                    _pressedClickable = c;
                }
            }
            if (_pressedClickable != null) {

                _pressedClickable.HandlePointerDown();
                _pressedId = eventData.pointerId;
            }
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
            if (_pressedClickable == null) {
                return;
            }
            if (_pressedId != eventData.pointerId) {
                return;
            }
            _pressedId = c_nullPointerId;
            _pressedClickable.HandlePointerUp();
            var c = _raycaster.Raycast(this, eventData.position);
            if(c == _pressedClickable) {
                _pressedClickable.HandleClick();
            }
            _pressedClickable = null;
        }

        public interface IClickFilter {
            bool ShouldClick(AClickable clickable);
        }
        struct Raycaster {
            private float _lastTime;
            private Vector2 _lastLocation;

            private AClickable _clickable;

            public void Init(ClickControls clickControls) {
                _lastTime = -1f;
            }

            public AClickable Raycast(ClickControls controls, Vector2 screenPoint) {
                float time = Time.time;
                if (time == _lastTime && _lastLocation == screenPoint) {
                    return _clickable;
                }
                _lastTime = time;
                _lastLocation = screenPoint;
                _clickable = null;

                var cam = controls.canvas.worldCamera;
                var ray = cam.ScreenPointToRay(screenPoint);
                int count = Physics.RaycastNonAlloc(ray, s_raycastBuffer, cam.farClipPlane - cam.nearClipPlane, controls.layerMask, QueryTriggerInteraction.Ignore);
                for(int i = 0; i  < count; i++) {
                    if (AClickable.TryGet<AClickable>(s_raycastBuffer[i].collider, controls._player, out var c)) {
                        _clickable = c;
                        return _clickable;
                    }
                }
                if (controls.mouseRadius > 0) {

                    Vector3 localPoint = Joystick.ToLocal(controls._transform, controls._transform, screenPoint);

                    var en = controls._clickables.GetEnumerator();
                    var camPos = cam.transform.position;
                    var relativeCamPos = controls._transform.InverseTransformPoint(camPos);
                    float minDistanceSqr = float.PositiveInfinity;
                    float thresholdSqr = controls.mouseRadius * controls.mouseRadius;
                    while (en.MoveNext()) {
                        var c = en.Current;

                        var relativeCPos = controls._transform.InverseTransformPoint(c.transform.position);
                        var r = new Ray(relativeCamPos, relativeCPos - relativeCamPos);
                        var point = r.PositionOnZ(0);
                        float distanceSqr = (localPoint - point).sqrMagnitude;
                        if (distanceSqr < thresholdSqr && distanceSqr < minDistanceSqr) {
                            minDistanceSqr = distanceSqr;
                            _clickable = c;

                        }
                    }
                    en.Dispose();
                }

                return _clickable;
            }

        }
    }
}