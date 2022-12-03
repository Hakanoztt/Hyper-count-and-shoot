using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame {
    public partial class BaseNavigationCharacter {

        public class MoveAction : IAction {
            public Vector3 target;
            public Transform parent;

            public Vector3 WorldTarget {
                get {
                    if (parent != null) {
                        return parent.TransformPoint(target);
                    }
                    return target;
                }
            }
            public void Init(Vector3 target, Transform parent = null) {
                this.target = target;
                this.parent = parent;
            }
            public void Activated(BaseNavigationCharacter character) {
                character.moveModule.Navigate(WorldTarget);
            }

            public bool Update(BaseNavigationCharacter character) {
                if (!character.moveModule.IsNavigating) {
                    return false;
                }
                return true;
            }
            public void Finished(BaseNavigationCharacter character, FinishReason reason) {

            }
        }
        public class PlayAnimation : IAction {
            public int animationState;
            public int layer;
            private float _endTime;
            private float _customDuration;
            public void Init(int animationState, int layer) {
                InitWithCustomDuration(animationState, layer);
            }
            public void InitWithCustomDuration(int animationState, int layer, float customDuration = 0) {
                this.animationState = animationState;
                this.layer = layer;
                _customDuration = customDuration;
            }

            public void Activated(BaseNavigationCharacter character) {
                character.animationModule.Play(this.animationState, layer, 0f, out float duration);
                if (_customDuration > 0) {
                    duration = _customDuration;
                    character.animationModule.UpdateDisableDuration(duration);
                }
                _endTime = character.Time + duration;
            }


            public bool Update(BaseNavigationCharacter character) {
                bool value = _endTime > character.Time;
                
                return value;
            }
            public void Finished(BaseNavigationCharacter character, FinishReason reason) {
                if (reason == FinishReason.Interrupted) {
                    character.animationModule.UpdateDisableDuration(0);
                }
                _customDuration = 0;
            }
        }
        public class SetDirection : IAction {
            public Transform parent;
            public Vector3 direction;

            public void Init(Vector3 direction, Transform parent = null) {
                this.direction = direction;
                this.parent = parent;
            }
            public Vector3 WorldDirection {
                get {
                    if (parent != null) {
                        return parent.TransformDirection(direction);
                    }
                    return direction;
                }
            }

            public void Activated(BaseNavigationCharacter character) {

            }

            public bool Update(BaseNavigationCharacter character) {
                var dir = WorldDirection;
                dir.y = 0;
                if (Vector3.Dot(character.transform.forward, dir) > 0.995f) {
                    character.moveModule.Input = Vector2.zero;
                    return false;
                }
                else {
                    character.moveModule.Turn(dir);
                }
                return true;
            }
            public void Finished(BaseNavigationCharacter character, FinishReason reason) {

            }
        }
        public class CallAction : IAction {
            private Action<BaseNavigationCharacter> _action;

            public void Init(Action<BaseNavigationCharacter> action) {
                _action = action;
            }
            public void Activated(BaseNavigationCharacter character) {
                var a = _action;
                _action = null;
                if (a != null) {
                    a(character);
                }
            }

            public bool Update(BaseNavigationCharacter character) {

                return false;
            }
            public void Finished(BaseNavigationCharacter character, FinishReason reason) {
                _action = null;
            }
        }


        public class CoroutineAction : IAction {
            private IEnumerator _routine;

            public void Init(IEnumerator routine) {
                _routine = routine;
            }
            public void Activated(BaseNavigationCharacter character) {

            }

            public bool Update(BaseNavigationCharacter character) {
                if (_routine.MoveNext()) {
                    return true;
                }
                return false;
            }

            public void Finished(BaseNavigationCharacter character, FinishReason reason) {

            }
        }

        public class WaitAction : IAction {
            private int _version;
            private bool _isRunning;
            public Handle InitWithHandle() {
                return new Handle(this, _version);
            }

            void IAction.Activated(BaseNavigationCharacter character) {
                _isRunning = true;
            }

            void IAction.Finished(BaseNavigationCharacter character, FinishReason reason) {
                _isRunning = false;
                _version++;
            }

            bool IAction.Update(BaseNavigationCharacter character) {
                return _isRunning;
            }

            public struct Handle {
                private WaitAction _waitAction;
                private int _version;

                internal Handle(WaitAction waitAction, int version) {
                    _waitAction = waitAction;
                    _version = version;
                }

                public bool IsRunning {
                    get {
                        return _waitAction != null && _waitAction._version == _version && _waitAction._isRunning;
                    }
                }
                public bool Stop() {
                    if (IsRunning) {
                        _waitAction._isRunning = false;
                        return true;
                    }
                    return false;
                }

            }
        }

    }
}
