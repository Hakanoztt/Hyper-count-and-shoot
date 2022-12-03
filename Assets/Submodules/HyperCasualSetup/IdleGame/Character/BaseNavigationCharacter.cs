using Mobge.Animation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mobge.IdleGame {
    public partial class BaseNavigationCharacter : MonoBehaviour, IAnimatorOwner {

        [OwnComponent] public NavMeshMoveModule moveModule;
        public AnimationModule animationModule;


        private ActionCache _actionCache;
        private StateManager _stateManager;


        public float Time => UnityEngine.Time.time;
        public float DeltaTime => UnityEngine.Time.deltaTime;


        public bool NavigationEnabled {
            get {
                return moveModule.NavigationEnabled;
            }
            set {
                moveModule.NavigationEnabled = value;
            }
        }

        public bool IsDoingAction {
            get => _stateManager.HasActiveState;
        }

        protected void Awake() {
            _actionCache.Init();
            _stateManager.Init(this);

        }
        protected void OnEnable() {
            animationModule.Init();
        }
        public State GetAnimationState(int animationState) {
            return animationModule.GetState(animationState);
        }

        public T AddAction<T>() where T : IAction, new() {
            var t = _actionCache.New<T>();
            _stateManager.AddState(this, t);
            return t;
        }

        public void Update() {
            _stateManager.Update(this);
            animationModule.Update(this);
            //Graph.GraphDataManager.Instance.AddData("character speed", moveModule.CurrentVelocity.magnitude);
        }
        public void ClearActions() {
            _stateManager.Clear(this);
        }

        Animator IAnimatorOwner.GetAnimator() {
            return animationModule != null ? animationModule.animator : null;
        }

        private struct ActionCache {
            private Dictionary<Type, List<IAction>> _cache;
            public void Init() {
                if (_cache == null) {
                    _cache = new Dictionary<Type, List<IAction>>();
                }
            }

            public T New<T>() where T : IAction, new() {
                if(!_cache.TryGetValue(typeof(T), out var l) || l.Count == 0) {
                    return new T();
                }
                int index = l.Count - 1;
                var t = l[index];
                l.RemoveAt(index);
                return (T)t;

            }
            public void Recycle(IAction action) {
                var t = action.GetType();
                if (!_cache.TryGetValue(t, out var l)) {
                    l = new List<IAction>();
                    _cache.Add(t, l);
                }
                l.Add(action);
            }
        }

        public interface IAction {
            void Activated(BaseNavigationCharacter character);
            bool Update(BaseNavigationCharacter character);
            void Finished(BaseNavigationCharacter character, FinishReason reason);
        }
        public enum FinishReason {
            Finished,
            Interrupted,
            Cancelled,
        }
        private struct StateManager {
            private ExposedQueue<ActionState> _queue;
            private bool _needsStart;
            public bool HasActiveState => _queue.Count > 0;
            public void Init(BaseNavigationCharacter character) {
                if (_queue == null) {
                    _queue = new ExposedQueue<ActionState>();
                }
                else {
                    this.Clear(character);
                }
            }

            public void Clear(BaseNavigationCharacter character) {
                var e = _queue.GetIndexEnumerator();
                while (e.MoveNext()) {
                    var a = _queue.array[e.Current];
                    a.action.Finished(character, a.started ? FinishReason.Interrupted : FinishReason.Cancelled);
                    character._actionCache.Recycle(a.action);
                }
                e.Dispose();
                _queue.Clear();
            }
            public void AddState(BaseNavigationCharacter character, IAction action) {
                _queue.Enqueue(new ActionState(action));
                if(_queue.Count == 1) {
                    _needsStart = true;
                }
            }

            private void StartState(BaseNavigationCharacter character) {
                ref var a = ref _queue.Peek();
                a.started = true;
                a.action.Activated(character);
            }
            public void Update(BaseNavigationCharacter character) {

                if (_needsStart) {
                    _needsStart = false;
                    StartState(character);
                }
                if(_queue.Count == 0) {
                    return;
                }
                ref var a = ref _queue.Peek();
                if (!a.action.Update(character)) {
                    a.action.Finished(character, FinishReason.Finished);
                    character._actionCache.Recycle(a.action);
                    _queue.Dequeue();
                    if(_queue.Count > 0) {
                        StartState(character);
                    }
                }
            }
            private struct ActionState {
                public bool started;
                public IAction action;

                public ActionState(IAction action) {
                    this.action = action;
                    started = false;
                }
            }
        }
        [Serializable]
        public class AnimationModule {
            [OwnComponent] public Animator animator;
            public OverridableAnimation idleAnimation;
            public OverridableAnimation walkAnimation;
            [AnimatorFloatParameter] public int walkAnimationSpeed;
            public float walkSpeedRate = 1;
            private float _autoAnimationDisabledTill;

            public void Init() {
                AutoAnimationEnabled = true;
            }
            public bool AutoAnimationEnabled {
                get {
                    return _autoAnimationDisabledTill < UnityEngine.Time.time;
                }
                set {
                    if (value) {
                        _autoAnimationDisabledTill = 0;
                    }
                    else {
                        _autoAnimationDisabledTill = float.PositiveInfinity;
                    }
                }
            }


            public void Update(BaseNavigationCharacter character) {
                if (animator == null || !AutoAnimationEnabled) {
                    return;
                }
                if (character.moveModule.CurrentVelocity.sqrMagnitude > 0.0001f) {
                    PlayDirect(walkAnimation.CurrentAnimation);
                    if (walkAnimationSpeed != 0) {
                        animator.SetFloat(walkAnimationSpeed, walkSpeedRate * character.moveModule.CurrentVelocity.magnitude);
                    }

                }
                else {
                    PlayDirect(idleAnimation.CurrentAnimation);
                }
            }
            private void PlayDirect(int state, int layer = 0) {
                var si = animator.GetActiveStateInfo(0);
                if(si.shortNameHash != state) {
                    animator.CrossFadeInFixedTime(state, 0.07f, layer);
                }
            }
            public void Play(int state, int layer = 0) {
                if (animator == null) {
                    return;
                }
                var si = animator.GetActiveStateInfo(0);
                if (si.shortNameHash != state) {
                    animator.CrossFadeInFixedTime(state, 0.07f, layer);
                    _autoAnimationDisabledTill = UnityEngine.Time.time + si.length;
                }
            }

            public void Play(int state, int layer, float normalizedTime, out float duration) {
                if (animator == null) {
                    duration = 0f;
                    return;
                }
                if (normalizedTime == 0) {
                    animator.CrossFade(state, 0.1f, layer, 0);
                }
                else {
                    animator.Play(state, layer, normalizedTime);
                }
                animator.Update(0f);
                duration = animator.GetActiveStateInfo(layer).length;
                duration *= (1 - normalizedTime);
                UpdateDisableDuration(duration);
            }
            public void UpdateDisableDuration(float duration) {
                if(!float.IsPositiveInfinity(_autoAnimationDisabledTill)) {
                    _autoAnimationDisabledTill = UnityEngine.Time.time + duration;
                }
            }

            public void CustomAnimationFinished() {
                _autoAnimationDisabledTill = 0;
            }

            public State GetState(int animation) {
                return new State(this, animation);
            }

        }
        [Serializable]
        public struct OverridableAnimation {
            [SerializeField, AnimatorState] private int animation;
            public int AnimationOverride { get; set; }



            public void ClearOverride() {
                AnimationOverride = 0;
            }
            public int CurrentAnimation => AnimationOverride != 0 ? AnimationOverride : animation;
        }
        public struct State {
            private AnimationModule _module;

            private int _animation;

            public State(AnimationModule module, int animation) {
                _module = module;
                _animation = animation;
            }

            public void Play() {
                _module.Play(_animation);
            }
        }
    }
}