using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mobge.UI {

    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class ContentSizeFitterPlus : UIBehaviour, ILayoutSelfController, ILayoutController {
        [SerializeField, HideInInspector] private Vector2 _minSize = Vector2.zero;
        [SerializeField, HideInInspector] private Vector2 _minSizeRatio = Vector2.zero;            
        [SerializeField, HideInInspector] private FitMode _horizontalFitMode = FitMode.Unconstrained;
        [SerializeField, HideInInspector] private FitMode _verticalFitMode = FitMode.Unconstrained;
        private RectTransform _rect;
        private DrivenRectTransformTracker _tracker;

        private RectTransform RectTransform {
            get {
                bool flag = this._rect == null;
                if (flag) {
                    this._rect = base.GetComponent<RectTransform>();
                }
                return this._rect;
            }
        }

        public Vector2 MinSize {
            get => _minSize;
            set {
                if (value != _minSize) {
                    _minSize = value;
                    SetDirty();
                }
            }
        }

        public Vector2 MinSizeRatio {
            get => _minSizeRatio;
            set {
                if(value != _minSizeRatio) {
                    _minSizeRatio = value;
                    SetDirty();
                }
            }
        }

        public FitMode HorizontalFitMode {
            get => _horizontalFitMode;
            set {
                if (_horizontalFitMode != value) {
                    _horizontalFitMode = value;
                    SetDirty();
                }
            }
        }
        public FitMode VerticalFitMode {
            get => _verticalFitMode;
            set {
                if (_verticalFitMode != value) {
                    _verticalFitMode = value;
                    SetDirty();
                }
            }
        }

        protected void SetDirty() {
            if (IsActive()) {
                LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
            }
        }
        protected override void OnRectTransformDimensionsChange() {
            base.OnRectTransformDimensionsChange();
            this.SetDirty();
        }
        protected override void OnEnable() {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnDisable() {
            this._tracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
            base.OnDisable();
        }

        private void HandleSelfFittingAlongAxis(int axis) {
            FitMode fitMode;
            if(axis == 0) {
                fitMode = _horizontalFitMode;
            }
            else {
                fitMode = _verticalFitMode;
            }

            if(fitMode == FitMode.Unconstrained) {
                _tracker.Add(this, RectTransform, DrivenTransformProperties.None);
            }
            else {
                DrivenTransformProperties props;
                float parentSize;
                float minSize;
                float minSizeRatio;
                float preferredSize;
                RectTransform.Axis ax;
                var rt = RectTransform;
                if (axis == 0) {
                    props = DrivenTransformProperties.SizeDeltaX;
                    minSize = _minSize.x;
                    minSizeRatio = _minSizeRatio.x;
                    parentSize = ((RectTransform)rt.parent).rect.width;
                    ax = RectTransform.Axis.Horizontal;
                    preferredSize = LayoutUtility.GetPreferredWidth(rt);
                }
                else {
                    props = DrivenTransformProperties.SizeDeltaY;
                    minSize = _minSize.y;
                    minSizeRatio = _minSizeRatio.y;
                    parentSize = ((RectTransform)rt.parent).rect.height;
                    ax = RectTransform.Axis.Vertical;
                    preferredSize = LayoutUtility.GetPreferredHeight(rt);
                }
                _tracker.Add(this, rt, props);
                float size = Mathf.Max(preferredSize, minSize);
                size = Mathf.Max(size, minSizeRatio * parentSize);
                rt.SetSizeWithCurrentAnchors(ax, size);


            }
        }

        public virtual void SetLayoutHorizontal() {
            _tracker.Clear();
            HandleSelfFittingAlongAxis(0);
        }



        public virtual void SetLayoutVertical() {
            HandleSelfFittingAlongAxis(1);
        }
        public enum FitMode {
            Unconstrained = 0,
            PreferredSize = 2
        }
    }

}