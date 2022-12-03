using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mobge.UI {
    public class UIPageCollection : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler {

        public delegate void PageChangeAction(UIPageCollection pages, int oldPage, int newPage);

        public Button leftButton;
        public Button rightButton;

        public RectTransform[] pageResources;
        public float snapTime = 0.3f;

        public PageChangeAction currentPageChanged; 

        private List<PageReference> _pages;
        private float _targetProgress;
        private bool _isDragging;
        private float _progressVelocity;
        private float _acceleration;

        private ActionManager _actionManager;
        private ActionManager.Action _fitAction;

        private float _progress;
        private float _lastProgress;
        private Canvas _canvas;

        private RectTransform _tr => (RectTransform)transform;
        public T GetPage<T>(int index) where T : MonoBehaviour {
            return GetPage<T>(index, out _);
        }
        public T GetPage<T>(int index, out int pageResourceId) where T : MonoBehaviour {
            var p = _pages[index];
            pageResourceId = p.resId;
            return p.page.GetComponent<T>();
        }
        public int CurrentPage {
            get { return Mathf.Clamp(Mathf.RoundToInt(_progress), 0, PageCount - 1); }
            set {
                SetProgress(Mathf.Clamp(value, 0, PageCount - 1));
            }
        }
        public float CurrentProgress => _progress;

        protected void Awake() {
            if (leftButton) {
                leftButton.onClick.AddListener(ScrollLeft);
            }
            if (rightButton) {
                rightButton.onClick.AddListener(ScroolRight);
            }
            _actionManager = new ActionManager();
            for (int i = 0; i < pageResources.Length; i++) {
                pageResources[i].gameObject.SetActive(false);
            }
            _canvas = GetComponentInParent<Canvas>();
            //_tr = (RectTransform)transform;
        }
        protected void Update() {
            _actionManager.Update(Time.unscaledDeltaTime);
            if (_isDragging) {
                float dt = Time.unscaledDeltaTime;
                var vel = (_progress - _lastProgress) / dt;
                _progressVelocity = Mathf.Lerp(_progressVelocity, vel, Mathf.Min(1, dt * 4f));
                _progressVelocity = vel;
                _lastProgress = _progress;
                //Debug.Log("v: " + _progress  + "," + _lastProgress);
            }
        }
        private void ScrollLeft() {
            if (!_isDragging) {
                var cp = CurrentPage;
                if (cp > 0) {
                    _progressVelocity = 0;
                    SetPorgressAnimating(cp - 1);
                }
            }
        }

        private void ScroolRight() {
            if (!_isDragging) {
                int worldCount = _pages.Count;
                var cp = CurrentPage;
                if (cp < worldCount - 1) {
                    _progressVelocity = 0;
                    SetPorgressAnimating(cp + 1);
                }
            }
        }
        public void SetPorgressAnimating(float targetProgress) {
            _targetProgress = targetProgress;
            float distance = _targetProgress - _progress;
            float avarageVel = distance / snapTime;
            float endVel = 2 * avarageVel - _progressVelocity;
            _acceleration = (endVel - _progressVelocity) / snapTime;
            _fitAction.Stop();
            _fitAction = _actionManager.DoTimedAction(snapTime, UpdateSnap, EndSnap);
        }

        public int PageCount {
            get => _pages == null ? 0 : _pages.Count;
            set {
                if (_pages == null) {
                    _pages = new List<PageReference>();
                }
                if (_pages.Count != value) {

                    while (_pages.Count > value) {
                        var p = _pages[_pages.Count - 1];
                        _pages.RemoveAt(_pages.Count - 1);
                        p.page.gameObject.DestroySelf();
                    }
                    while (_pages.Count < value) {
                        int count = _pages.Count;
                        AddPage(count % pageResources.Length);
                    }
                    UpdateProgress();
                }
            }
        }

        public void AddPage(int resourceIndex) {
            var res = pageResources[resourceIndex];
            var instance = Instantiate(res);
            instance.gameObject.SetActive(true);
            instance.transform.SetParent(transform, false);
            _pages.Add(new PageReference(instance, resourceIndex));
        }

        private void EndSnap(object data, bool completed) {
            if (completed) {
                _progressVelocity = 0;
                SetProgress(_targetProgress);
            }
        }

        private void UpdateSnap(in ActionManager.UpdateParams @params) {
            var dt = Time.unscaledDeltaTime;
            _progressVelocity += dt * _acceleration;
            SetProgress(_progress + _progressVelocity * dt);
        }
        void SetProgress(float progress) {
            var cp = CurrentPage;
            _progress = progress;
            //_progress = Mathf.Clamp(progress, -0.4f, _menu._gameContext.levelData.worlds.Count + (-1 + 0.4f));
            UpdateProgress();
            int ncp = CurrentPage;
            if (cp != ncp) {
                currentPageChanged?.Invoke(this, cp, ncp);
            }
        }
        private void OnEnable() {

            UpdateProgress();
        }
        private Vector3 GetPosition(float progress, RectTransform targetPage) {
            float width = (_tr.rect.width + targetPage.rect.width) * 0.5f;
            return new Vector3(width * progress, 0, 0);
        }

        private void UpdateProgress() {
            var currentPage = CurrentPage;
            //bool showRight = currentPage < _progress;
            int worldCount = PageCount;
            if (leftButton) {
                leftButton.interactable = currentPage > 0;
            }
            if (rightButton) {
                rightButton.interactable = currentPage < worldCount - 1;
            }
            // todo: Update only visible ones. Track visibility and activate/deactivate when objects change their visibility.
            // 
            float progress = -_progress;
            for(int i = 0; i < PageCount; i++, progress += 1f) {
                _pages[i].page.anchoredPosition = GetPosition(progress, _pages[i].page);
            }
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) {
            _isDragging = true;
            _lastProgress = _progress;
            _fitAction.Stop();
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData) {
            _isDragging = false;
            //Debug.Log("end vel: " + _progressVelocity);
            var targetProgress = Mathf.Round(_progress + _progressVelocity * snapTime * 0.5f);
            //float nearesProgress = Mathf.Round(_progress);
            //targetProgress = Mathf.Clamp(targetProgress, nearesProgress - 1, nearesProgress + 1);
            targetProgress = Mathf.Clamp(targetProgress, 0, PageCount - 1);
            SetPorgressAnimating(targetProgress);
        }

        void IDragHandler.OnDrag(PointerEventData eventData) {
            var ctr = (RectTransform)_canvas.transform;

            var localPos = Joystick.ToLocal(ctr, _tr, eventData.position);
            var prevLocalPos = Joystick.ToLocal(ctr, _tr, eventData.position - eventData.delta);
            SetProgress(_progress + -(localPos.x - prevLocalPos.x)/ ctr.rect.width);
        }
        public struct PageReference {
            public RectTransform page;
            public int resId;

            public PageReference(RectTransform page, int resId) {
                this.page = page;
                this.resId = resId;
            }
        }
    }
}