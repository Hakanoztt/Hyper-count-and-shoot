using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mobge.UI {
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class UIAspectScaler : UIBehaviour {
        [SerializeField, HideInInspector] private AspectMode _aspectMode;

        private DrivenRectTransformTracker _tracker;

        public AspectMode aspectMode {
            get => _aspectMode;
            set {
                if(_aspectMode != value) {
                    _aspectMode = value;
                    UpdateScale();
                }
            }
        }
        protected override void OnDisable() {
            this._tracker.Clear();
            base.OnDisable();
        }
        protected override void OnEnable() {
            base.OnEnable();
            UpdateScale();
        }
        protected override void OnRectTransformDimensionsChange() {
            base.OnRectTransformDimensionsChange();
            UpdateScale();
        }
        private void UpdateScale() {
            _tracker.Clear();
            switch (aspectMode) {
                default:
                case AspectMode.None:
                    break;
                case AspectMode.FitInParent:
                    FitTo(true);
                    break;
                case AspectMode.EnvelopeParent:
                    FitTo(false);
                    break;
            }
        }
        private void FitTo(bool minRatio) {
            var tr = (RectTransform)this.transform;
            _tracker.Add(this, tr, DrivenTransformProperties.AnchoredPositionX | DrivenTransformProperties.AnchoredPositionY | DrivenTransformProperties.AnchorMin | DrivenTransformProperties.AnchorMax | DrivenTransformProperties.Scale);
            tr.anchorMin = new Vector2(0.5f, 0.5f);
            tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.anchoredPosition = Vector2.zero;

            var size = tr.rect.size;
            var parent = tr.parent as RectTransform;
            Vector2 parentSize = parent ? parent.rect.size : Vector2.zero;
            Vector2 scales = parentSize / size;
            float scale = scales.x;
            if((scales.x > scales.y) == minRatio) {
                scale = scales.y;
            }
            tr.localScale = new Vector3(scale, scale, scale);
        }

        public enum AspectMode {
            None = 0,
            FitInParent = 1,
            EnvelopeParent = 2,
        }
    }
}