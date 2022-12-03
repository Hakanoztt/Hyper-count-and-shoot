using Mobge.Animation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.UI
{
    public class BaseMenu : MonoBehaviour
    {

        [OwnComponent(true)] public Canvas canvas;
        [OwnComponent(true)] public GraphicRaycaster raycaster;
        [OwnComponent] public Animator animator;
        [AnimatorState] public int enterAnim;
        [AnimatorState] public int exitAnim;
        // public Action<int, object> fireOutputEvent;

        [SerializeField] private ExtensionReference[] _extensions;
        
        
        //todo get output slots
        public delegate void OnOpenDelegate(BaseMenu openedMenu);
        public event OnOpenDelegate onOpen;
        
        public delegate void OnCloseDelegate(BaseMenu closedMenu);
        public event OnCloseDelegate onClose;

        public delegate void OnFocusChangeDelegate(bool isFocused);
        public event OnFocusChangeDelegate onFocusChange;
        
        public MenuManager MenuManager { get; internal set; }
        [Serializable]
        public struct ExtensionReference {
            [SerializeField, OwnComponent(type = typeof(IExtension))] private UnityEngine.Component _object;
            public IExtension Extension => (IExtension)_object;
        }
        public int ExtensionCount => _extensions == null ? 0 : _extensions.Length;
        public T GetExtension<T>(int index) where T : class {
            return _extensions[index].Extension as T;
        }
        public T GetExtension<T>() where T : IExtension {
            for(int i = 0; i < _extensions.Length; i++) {
                if(_extensions[i].Extension is T t) {
                    return t;
                }
            }
            return default(T);
        }
        public interface IExtension
        {
            void Prepare(BaseMenu menu);
        }

        private State _state;
        private RoutineManager _actionManager;
        private RoutineManager.Routine _lastAnim;
        private RoutineManager.RoutineFinish _onFinish;
        private bool _interactable = true;
        private bool _internalInteractable = true;

        public RoutineManager ActionManager => _actionManager;

        public State CurrentState {
            get => _state;
            private set {
                if(_state != value) {
                    _state = value;
                    switch (_state) {
                        case State.Closed:
                            Active = false;
                            InternalInteractable = false;
                            break;
                        case State.Opening:
                            Active = true;
                            InternalInteractable = false;
                            break;
                        case State.Open:
                            Active = true;
                            InternalInteractable = true;
                            
                            break;
                        case State.Closing:

                            OnClose();
                            Active = true;
                            InternalInteractable = false;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        public virtual void Prepare() {
            if (_extensions != null) {
                for (int i = 0; i < _extensions.Length; i++) {
                    _extensions[i].Extension.Prepare(this);
                }
            }
        }

        protected void Awake() {
            _actionManager = new RoutineManager();
            _state = State.Open;
            CurrentState = State.Closed;
            canvas.worldCamera = Camera.main;
            
        }

        private bool Active {
            get => gameObject.activeSelf;
            set {
                if (value != Active) {
                    gameObject.SetActive(value);
                }
            }
        }

        private bool InternalInteractable {
            set {
                _internalInteractable = value;
                if (raycaster != null) {
                    raycaster.enabled = _internalInteractable && _interactable;
                }
            }
        }
        public bool Interactable {
            get => _interactable;
            set {
                if (_interactable != value) {
                    _interactable = value;
                    InternalInteractable = _internalInteractable;
                }
            }
        }

        public void SetEnabled(bool enabled) {
            CurrentState = enabled ? State.Open : State.Closed;
            if (enabled && enterAnim != 0) {
                animator.Play(enterAnim, 0, 1);
            }
            // todo: call open immediately after menumanager que execution ends
            MenuManager.StartCoroutine(OnOpenDelayed());
        }
        IEnumerator OnOpenDelayed() {
            yield return new WaitForEndOfFrame();
            OnOpen();
        }

        public bool SetEnabledWithAnimation(bool enabled, RoutineManager.RoutineFinish onFinish) {
            if (enabled) {
                if(_state != State.Closed) {
                    onFinish?.Invoke(this, false);
                    return false;
                }
                if(enterAnim == 0) {
                    SetEnabled(enabled);
                    onFinish?.Invoke(this, true);
                }
                else {
                    _onFinish = onFinish;
                    CurrentState = State.Opening;
                    PlayAnimation(enterAnim, EnableAnimEnd);
                }
                return true;
            }
            else {
                if (_state != State.Open) {
                    onFinish?.Invoke(this, false);
                    return false;
                }
                if(exitAnim == 0) {
                    SetEnabled(enabled);
                    onFinish?.Invoke(this, true);
                }
                else {
                    _onFinish = onFinish;
                    CurrentState = State.Closing;
                    PlayAnimation(exitAnim, DisableAnimEnd);
                }
                return true;
            }
        }
        protected virtual void OnOpen() {
            onOpen?.Invoke(this);
            OnFocusChange(true);
        }
        protected virtual void OnClose() {
            onClose?.Invoke(this);
            OnFocusChange(false);
        }
        public virtual void OnFocusChange(bool isFocused) {
            onFocusChange?.Invoke(isFocused);
        }
        private void DisableAnimEnd(bool completed, object data) {
            if (completed) {
                CurrentState = State.Closed;
            }
            var of = _onFinish;
            _onFinish = null;
            of?.Invoke(this, completed);
        }

        private void EnableAnimEnd(bool completed, object data) {
            if (completed) {
                CurrentState = State.Open;
            }
            var of = _onFinish;
            _onFinish = null;
            of?.Invoke(this, completed);
            OnOpen();
        }

        private void PlayAnimation(int animId, RoutineManager.RoutineFinish onFinish) {
            animator.Play(animId);
            animator.Update(0);
            var stt = animator.GetCurrentAnimatorStateInfo(0);
            
            _lastAnim.Stop();
            _lastAnim = _actionManager.DoAction(onFinish, stt.length);
            enabled = true;
        }
        protected void Update() {
            _actionManager.Update(Time.unscaledDeltaTime);
        }
        public enum State
        {
            Closed = 0,
            Opening,
            Open,
            Closing,
        }
#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (canvas) {
                if(canvas.worldCamera == null) {
                    canvas.worldCamera = Camera.main;
                }
            }
        }
#endif
    }
}