using Mobge.Core;
using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.UI {

    public class WorldPopupCanvas : MonoBehaviour {
        public const string c_manager_key = "wrldPppCnvs";


        [OwnComponent(true)] public UIItem mainItem;
        [OwnComponent(true)] public Canvas canvas;
        

        private Transform _followTarget;
        private Vector3 _offset;
        private float cameraOffset = 1f;

        private GraphicRaycaster _raycaster;

        public int openState;
        public int closeState;

        public Vector3 Offset { get => _offset; set => _offset = value; }

        private float _closeEndTime;
        private Action<WorldPopupCanvas> _onCloseEnd;
        private WorldPopupCanvas _prefab;

        protected void Awake() {
            _raycaster = GetComponent<GraphicRaycaster>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
        }

        private void Open(Transform followTarget, Vector3 offset) {
            _followTarget = followTarget;
            _offset = offset;
            gameObject.SetActive(true);
            mainItem.SetState(openState);
            if (_raycaster != null) {
                _raycaster.enabled = true;
            }
        }

        private void Close(Action<WorldPopupCanvas> onCloseEnd) {
            _onCloseEnd = onCloseEnd;
            mainItem.SetState(closeState);
            _closeEndTime = Time.time;
            if (_raycaster != null) {
                _raycaster.enabled = false;
            }
            float _closeDuration;
            if (mainItem.animator == null || closeState == -1) {
                _closeDuration = 0;
            }
            else {
                mainItem.animator.Update(0);
                _closeDuration = mainItem.animator.GetCurrentAnimatorStateInfo(0).length;
            }
            _closeEndTime = Time.time + _closeDuration;
        }


        protected void LateUpdate() {
            var camera = canvas.worldCamera;
            var camTr = camera.transform;
            var worldPosition = _followTarget.transform.TransformPoint(_offset);
            var camPos = camTr.position;
            var camForward = camTr.forward;
            transform.rotation = Quaternion.LookRotation(camForward);
            Vector3 pos;
            if (camera.orthographic) {
                float verticalDistance = Vector3.Dot(camForward, worldPosition - camPos);
                float extraDistance = verticalDistance - this.cameraOffset;
                pos = worldPosition - camForward * extraDistance;
            }
            else {
                var dir = worldPosition - camPos;
                dir.Normalize();
                pos = camPos + dir * this.cameraOffset;
            }
            this.transform.position = pos;

            if(_closeEndTime <= Time.time) {
                if (_onCloseEnd != null) {
                    var a = _onCloseEnd;
                    _onCloseEnd = null;
                    a(this);
                }
            }
        }

        [Serializable]
        public struct Referance<T> where T : WorldPopupCanvas {
            public T res;
            public Vector3 followOffset;


            private T _instance;
            public T Instance {
                get => _instance;
            }
            public void Open(LevelPlayer player, Transform target) {
                if (_instance == null) {
                    _instance = WorldPopupCanvas.Open(player, res, target, followOffset);
                }
            }
            public void Close(LevelPlayer player) {
                if (_instance != null) {
                    WorldPopupCanvas.Close(player, _instance);
                    _instance = null;
                }
            }

        }

        public static T Open<T>(LevelPlayer player, T prefab, Transform target, Vector3 offset) where T : WorldPopupCanvas {
            var canvas = (T)CanvasManager.Get(player).Spawn(prefab);
            canvas.Open(target, offset);
            return canvas;
        }
        public static void Close(LevelPlayer player, WorldPopupCanvas canvas) {
            canvas.Close(CanvasManager.Get(player).Recycle);
        }
        private class CanvasManager {
            private LevelPlayer _player;

            PrefabCache<WorldPopupCanvas> _cache;
            private CanvasManager(LevelPlayer player) {
                _player = player;
                _cache = new PrefabCache<WorldPopupCanvas>(true, false);
            }
            public static CanvasManager Get(LevelPlayer player) {
                if(!player.TryGetExtra(c_manager_key, out CanvasManager cm)) {
                    cm = new CanvasManager(player);
                    player.SetExtra(c_manager_key, cm);
                }
                return cm;
            }

            public WorldPopupCanvas Spawn(WorldPopupCanvas prefab) {
                var ins = _cache.Pop(prefab, _player.transform);
                ins._prefab = prefab;
                return ins;
            }
            public void Recycle(WorldPopupCanvas instance) {
                _cache.Push(instance._prefab, instance);
                instance._prefab = null;
            }
        }
    }
}