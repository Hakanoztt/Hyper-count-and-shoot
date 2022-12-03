using ElephantSDK;
using Mobge;
using Mobge.HyperCasualSetup;
using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI.CategorizedMarket {
    public class MarketItem3DSimulator : MonoBehaviour, BaseMenu.IExtension, IDragHandler {
        private IMarketVisual _marketItem;
        [InterfaceConstraint(typeof(IMarketVisual))] public Component marketItem;


        private CategorizedMarketMenu _menu;
        private RawImage _image;
        private RectTransform _tr;
        private Canvas _canvas;
        private RenderTexture _texture;
        private float _rotateSpeed = 1;
        private Quaternion _baseRotation = Quaternion.identity;
        private bool _previewEnabled;
        private Component _instance;

        public RectTransform RectTransform {
            get {
                if (_tr == null) {
                    _tr = (RectTransform)transform;
                }
                return _tr;
            }
        }

        protected Canvas Canvas {
            get {
                if (_canvas == null) {
                    _canvas = GetComponentInParent<Canvas>();
                }
                return _canvas;
            }
        }
        public string remote_RotateSpeed = "market_item_rotate_speed";
        public string remote_marketRotateEnabled = "market_rotate_enabled";

        void Update() {
            if (_image != null)
                CapturePreview();
        }

        public void Prepare(BaseMenu menu) {
            _image = GetComponent<RawImage>();
            _menu = (CategorizedMarketMenu)menu;
            if (_marketItem == null) {
                _instance = Instantiate(marketItem, new Vector3(100000, 0, 0), Quaternion.identity);
                _marketItem = _instance.GetComponent<IMarketVisual>();
                _marketItem.Init((BaseLevelPlayer)menu.MenuManager.CurrentLevel);
            }

            var remoteConfig = RemoteConfig.GetInstance();

            _previewEnabled = remoteConfig.GetBool(remote_marketRotateEnabled, true);
            if (_previewEnabled) {
                if (_baseRotation != Quaternion.identity) {
                    _marketItem.Visual.rotation = _baseRotation;
                }
                else {
                    _baseRotation = _marketItem.Visual.rotation;
                }
                _rotateSpeed = remoteConfig.GetFloat(remote_RotateSpeed, _rotateSpeed);
            }

            _menu.onItemBought -= OnItemBought;
            _menu.onItemBought += OnItemBought;
            _menu.onItemEquipped -= OnItemEquipped;
            _menu.onItemEquipped += OnItemEquipped;
            _menu.onOpen -= OnOpen;
            _menu.onOpen += OnOpen;
        }

        private void OnOpen(BaseMenu openedMenu) {
            UpdateVisual();
        }

        private void UpdateVisual() {
            _marketItem.OnVisualUpdated();
        }

        private void OnItemEquipped(ItemSet set, int id) {
            _marketItem.OnVisualUpdated(set, id);
        }

        private void OnItemBought(ItemSet set, int id) {
            _marketItem.OnVisualUpdated(set, id);
        }

        private void CapturePreview() {
            Rect rect;
            if (Canvas.renderMode == RenderMode.ScreenSpaceOverlay) {
                rect = RectTransform.rect;
            }
            else {
                rect = RectTransformToScreenSpace(RectTransform, Canvas);
            }

            Vector2Int size = Vector2Int.CeilToInt(rect.size);
            if (_texture == null || Mathf.Abs(_texture.width - size.x) > size.x * 0.05f || Mathf.Abs(_texture.height - size.y) > size.y * 0.05f) {
                if (_texture != null) {
                    _texture.DestroySelf();
                    _texture = null;
                }
                _texture = new RenderTexture(size.x, size.y, 16);
                _image.texture = _texture;
                _marketItem.Camera.targetTexture = _texture;
            }
        }

        private void OnEnable() {
            _instance.gameObject.SetActive(true);
        }

        private void OnDisable() {
            _instance.gameObject.SetActive(false);
        }

        private static Rect RectTransformToScreenSpace(RectTransform transform, Canvas canvas) {
            Rect rect = transform.rect;

            Camera cam = canvas.worldCamera;
            Vector3 min = transform.TransformPoint(rect.min);
            Vector3 max = transform.TransformPoint(rect.max);

            if (cam != null) {
                min = cam.WorldToScreenPoint(min);
                max = cam.WorldToScreenPoint(max);
            }
            else {
                Vector2 size = new Vector2(Screen.width, Screen.height);
                min.Scale(size);
                max.Scale(size);
            }

            rect.min = min;
            rect.max = max;

            return rect;
        }

        private void OnDestroy() {
            if (_texture != null) {
                _texture.DestroySelf();
                _texture = null;

            }
        }

        public void OnDrag(PointerEventData eventData) {
            if (_previewEnabled) {

                if (eventData.delta.x != 0) {
                    _marketItem.Visual.Rotate(Vector3.up * -eventData.delta.x * _rotateSpeed, Space.World);
                }
            }
        }

    }

    public interface IMarketVisual {
        public Camera Camera { get; }
        public Transform Visual { get; }
        public void Init(BaseLevelPlayer player);
        public void OnVisualUpdated(ItemSet set = null, int id = -1);
    }
}