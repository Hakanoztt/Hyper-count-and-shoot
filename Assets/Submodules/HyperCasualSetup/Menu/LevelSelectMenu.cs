using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Mobge.HyperCasualSetup.UI
{
    public class LevelSelectMenu : Mobge.UI.BaseMenu , IBeginDragHandler, IEndDragHandler, IDragHandler{
        [OwnComponent] public Button backButton;
        [SerializeField] private ScrollManager _scrollManager;

        public Func<ALevelSet.ID, bool> onLevelSelected;
        public Action<LevelSelectMenu> onBackPressed;

        private AGameContext _gameContext;

        protected new void Awake() {
            base.Awake();
            if (backButton != null) {
                backButton.onClick.AddListener(BackAction);
            }
        }

        private void BackAction() {
            if (onBackPressed != null) {
                onBackPressed(this);
            }
        }

        public void SetParameters(AGameContext gameContext) {
            _gameContext = gameContext;
            _scrollManager.Initialize(this, 0);
        }
        
        void IDragHandler.OnDrag(PointerEventData eventData) {
            _scrollManager.OnDrag(this, eventData);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData){
            _scrollManager.OnPointerUp(eventData);
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) {
            _scrollManager.OnPointerDown(eventData);
        }
        protected new void Update() {
            base.Update();
            _scrollManager.Update();
            
        }
        [Serializable]
        public class ScrollManager
        {
            public RectTransform parent;
            public Button leftButton;
            public Button rightButton;
            [OwnComponent, SerializeField] private WorldSelectPanel _worldPanelRes;
            private WorldSelectPanel[] _worldPanels;
            private float _progress;
            public int CurrentPage => Mathf.Clamp(Mathf.RoundToInt(_progress), 0, _menu._gameContext.LevelData.WorldCount - 1);
            public float snapTime = 0.3f;
            private ConsistancyCache<WorldSelectPanel, float> _consistancyCache;
            private float _lastProgress;
            private float _targetProgress;
            private float _progressVelocity;
            private bool _isDragging;
            private LevelSelectMenu _menu;
            private RoutineManager.Routine _fitAction;
            private float _acceleration;

            public void Initialize(LevelSelectMenu menu, int currentPage) {
                if(_worldPanels == null) {
                    _worldPanels = new WorldSelectPanel[2];
                    _worldPanelRes.gameObject.SetActive(false);
                    var parent = (RectTransform)_worldPanelRes.transform.parent;
                    for(int i = 0; i < _worldPanels.Length; i++) {
                        var panel = Instantiate(_worldPanelRes, parent, false);
                        panel.gameObject.SetActive(true);

                        var str = (RectTransform)_worldPanelRes.transform;
                        var ptr = (RectTransform)panel.transform;
                        ptr.anchoredPosition = str.anchoredPosition;
                        ptr.sizeDelta = str.sizeDelta;
                        //ptr.SetParent(parent, false);
                        panel.onLevelButtonClicked = LevelButtonClicked;
                        _worldPanels[i] = panel;
                        
                    }

                    _consistancyCache = new ConsistancyCache<WorldSelectPanel, float>(_worldPanels);

                    if (leftButton) {
                        leftButton.onClick.AddListener(ScrollLeft);
                    }
                    if (rightButton) {
                        rightButton.onClick.AddListener(ScroolRight);
                    }
                }
                _menu = menu;
                UpdateProgress();
            }

            private void ScrollLeft() {
                if (!_isDragging) {
                    var cp = CurrentPage;
                    if(cp > 0) {
                        _progressVelocity = 0;
                        SetPorgressAnimating(cp - 1);
                    }
                }
            }

            private void ScroolRight() {
                if (!_isDragging) {
                    int worldCount = _menu._gameContext.LevelData.WorldCount;
                    var cp = CurrentPage;
                    if (cp < worldCount - 1) {
                        _progressVelocity = 0;
                        SetPorgressAnimating(cp + 1);
                    }
                }
            }

            private void LevelButtonClicked(WorldSelectPanel arg1, int arg2) {
                _menu.onLevelSelected?.Invoke(LevelData.ID.FromWorldLevel(arg1.WorldIndex, arg2));
            }

            internal void OnDrag(LevelSelectMenu menu, PointerEventData eventData) {

                SetProgress(_progress + -eventData.delta.x / Screen.width);
            }

            internal void OnPointerDown(PointerEventData eventData) {
                _isDragging = true;
                _lastProgress = _progress;
                _fitAction.Stop();
            }

            internal void OnPointerUp(PointerEventData eventData) {
                _isDragging = false;
                //Debug.Log("end vel: " + _progressVelocity);
                var targetProgress = Mathf.Round(_progress + _progressVelocity * snapTime * 0.5f);
                //float nearesProgress = Mathf.Round(_progress);
                //targetProgress = Mathf.Clamp(targetProgress, nearesProgress - 1, nearesProgress + 1);
                targetProgress = Mathf.Clamp(targetProgress, 0, _menu._gameContext.LevelData.WorldCount - 1);
                SetPorgressAnimating(targetProgress);
            }
            void SetPorgressAnimating(float targetProgress) {
                _targetProgress = targetProgress;
                float distance = _targetProgress - _progress;
                float avarageVel = distance / snapTime;
                float endVel = 2 * avarageVel - _progressVelocity;
                _acceleration = (endVel - _progressVelocity) / snapTime;
                _fitAction.Stop();
                _fitAction = _menu.ActionManager.DoAction(EndSnap, snapTime, UpdateSnap);
            }

            private void EndSnap(bool completed, object data) {
                if (completed) {
                   SetProgress(_targetProgress);
                }
            }

            private void UpdateSnap(float progress, object data) {
                var dt = Time.unscaledDeltaTime;
                _progressVelocity += dt * _acceleration;
                SetProgress(_progress + _progressVelocity * dt);
            }

            void SetProgress(float progress) {
                _progress = progress;
                //_progress = Mathf.Clamp(progress, -0.4f, _menu._gameContext.levelData.worlds.Count + (-1 + 0.4f));
                UpdateProgress();
            }
            internal void Update() {
                if (_isDragging) {
                    float dt = Time.unscaledDeltaTime;
                    var vel = (_progress - _lastProgress) / dt;
                    _progressVelocity = Mathf.Lerp(_progressVelocity, vel, Mathf.Min(1, dt * 4f));
                    _progressVelocity = vel;
                    _lastProgress = _progress;
                    //Debug.Log("v: " + _progress  + "," + _lastProgress);
                }
            }

            private Vector3 GetPosition(float progress) {
                return new Vector3(parent.rect.width * -progress, 0, 0);
            }

            private void UpdateProgress() {
                this._menu.gameObject.SetActive(true);
                var currentPage = CurrentPage;
                bool showRight = currentPage < _progress;
                int worldCount = _menu._gameContext.LevelData.WorldCount;
                if (Mathf.Approximately(currentPage, _progress) ||
                    (!showRight && currentPage == 0) ||
                    (showRight && currentPage == worldCount - 1)) {
                    _consistancyCache.AddRequirement(currentPage, _progress - currentPage);
                }
                else {
                    if (showRight) {
                        _consistancyCache.AddRequirement(currentPage, _progress - currentPage);
                        _consistancyCache.AddRequirement(currentPage + 1, _progress - currentPage - 1);
                    }
                    else {
                        _consistancyCache.AddRequirement(currentPage, _progress - currentPage);
                        _consistancyCache.AddRequirement(currentPage - 1, _progress - currentPage + 1);
                    }
                }
                if (leftButton) {
                    leftButton.interactable = currentPage > 0;
                }
                if (rightButton) {
                    rightButton.interactable = currentPage < worldCount - 1;
                }
                var pairs = _consistancyCache.ConsumePairs();
                while (pairs.MoveNext()) {
                    var c = pairs.Current;
                    c.resource.SetLevels(_menu._gameContext, c.id, GetPosition(c.requirement));
                }
                for (int i = 0; i < _worldPanels.Length; i++) {
                    var p = _worldPanels[i];
                    bool used = _consistancyCache.IsResourceUsed(i);
                    if (p.gameObject.activeSelf != used) {
                        p.gameObject.SetActive(used);
                    }
                }
            }
        }
    }
}