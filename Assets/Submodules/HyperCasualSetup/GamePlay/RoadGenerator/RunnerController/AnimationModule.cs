using Mobge.Animation;
using Mobge.Graph;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {

    public partial class RunnerController {

        [Serializable]
        public class AnimationModule {

            public float minOnGrounTime = 0.07f;

            public OverridableAnimation runAnim;
            public OverridableAnimation idleAnim;
            public JumpModule jumpModule  = new JumpModule() {
                minAcceptedJumpSpeed = 5f,
                enabled = true,
            };

            //private float _lastGroundStartTime;

            private RunnerController _controller;

            [OwnComponent] public Animator animator;

            ExposedList<StateInfo> _currentStates;
            private bool _waitingToInitialize;
            private bool _onGround;
            private float _disabledUntil;

            public State RunState => GetState(runAnim.CurrentAnimation);
            public State IdleState => GetState(idleAnim.CurrentAnimation);

            public void SetDisabledUntil(float time) {
                _disabledUntil = time;
            }

            public void Init(RunnerController cont) {
                _controller = cont;
                _currentStates = new ExposedList<StateInfo>();
            }

            private State GetState(int anim) {
                return new State(_controller, anim);
            }
            public void Update() {
                if(animator == null) {
                    return;
                }
                if(_controller.Time < _disabledUntil) {
                    return;
                }
                bool onGround = _controller.OnGround;
                //bool groundStart = !_onGround && onGround;
                //if (groundStart) {
                //    _lastGroundStartTime = _controller.Time;
                //}
                
                if(onGround || 
                    //(_controller.Time - _lastGroundStartTime) < minOnGrounTime || 
                    !jumpModule.enabled) {

                    bool isRunning = Mathf.Abs(_controller.input.MoveInput.y) > 0.05f;

                    if (isRunning) {
                        RunState.Play();
                    }
                    else {
                        IdleState.Play();
                    }
                }
                else {
                    jumpModule.Update(this, _onGround);
                }
                _onGround = onGround;
                if (_waitingToInitialize) {
                    _waitingToInitialize = false;
                    for (int i = 0; i < _currentStates.Count; i++) {
                        ref var stt = ref _currentStates.array[i];
                        if (stt.Current != 0) {
                            Play(stt.Current, i);
                        }
                    }
                }
            }

            private void PlayFrom(int state, float normalizedTime, int layer = 0) {
                if (_currentStates.Count <= layer) {
                    _currentStates.SetCount(layer + 1);
                }
                ref var stt = ref _currentStates.array[layer];
                stt.Current = state;
                float passed = stt.PassedTime;
                if (animator.isInitialized) {
                    animator.CrossFade(state, 0.07f, layer, normalizedTime, Mathf.Min(passed, 1f));
                }
            }

            private void Play(int state, int layer = 0) {
                if (_currentStates.Count <= layer) {
                    _currentStates.SetCount(layer + 1);
                }
                ref var stt = ref _currentStates.array[layer];
                if (stt.Current != state) {
                    stt.Current = state;
                    if (animator.isInitialized) {
                        // animator.Play(state, layer, 0f);
                        animator.CrossFadeInFixedTime(state, 0.07f, layer, 0f);
                    }
                    else {
                        _waitingToInitialize = true;
                    }
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
                private RunnerController _controller;

                private int _animation;

                public State(RunnerController controller, int animation) {
                    _controller = controller;
                    _animation = animation;
                }

                public void Play() {
                    _controller.animationModule.Play(_animation);
                }
            }
            private struct StateInfo {
                private int _current;
                private float _startTime;
                public float StartTime => _startTime;
                public int Current {
                    get => _current; 
                    set {
                        if (_current != value) {
                            _current = value;
                            _startTime = UnityEngine.Time.time;
                        }
                    }
                }
                public float PassedTime => UnityEngine.Time.time - _startTime;
            }

            [Serializable]
            public struct JumpModule {
                public OverridableAnimation anim;
                public float minAcceptedJumpSpeed;
                public bool enabled;
                private bool _onGround;
                private float _jumpSpeed;
                private float _jumpStartTime;
                public void Update(in AnimationModule animModule, bool firstJumpFrame) {
                    if (!enabled) {
                        return;
                    }
                    float time = UnityEngine.Time.time;
                    if (firstJumpFrame) {
                        _jumpStartTime = time;
                        _jumpSpeed = animModule._controller.Velocity.y;
                        minAcceptedJumpSpeed = Mathf.Max(minAcceptedJumpSpeed, 0.2f);
                        _jumpSpeed = Mathf.Max(minAcceptedJumpSpeed, _jumpSpeed);
                    }
                    float velocity = animModule._controller.Velocity.y;



                    float start = _jumpSpeed; 
                    float end = -_jumpSpeed;
                    float progress = (velocity - start) / (end - start);
                    progress = Mathf.Clamp(progress, 0, 0.99f);

                    animModule.PlayFrom(anim.CurrentAnimation, progress, 0);
                }
            }

        }
    }
}