using Mobge.Animation;
using Mobge.Core;
using Mobge.Core.Components;
using System.Collections.Generic;
using UnityEngine;
using AnimationState = Mobge.Animation.AnimationState;

namespace Mobge.HyperCasualSetup {
    public class CollectableBehaviour : MonoBehaviour, IComponentExtension, ILogicComponent {
        public int type;
        [OwnComponent] public new AAnimation animation;
        [OwnComponent] public new Collider2D collider;
        [OwnComponent] public Collider collider3D;
        [Animation(true)] public int appearAnimation = -1;
        [Animation(true)] public int collectedAnimation = -1;
        [Animation] public int[] cycleAnimations;
        public float[] cycleAnimationWeights;
        
        public LayerMask layerMask = -1;
        public ReusableReference takeEffect;
        public float availableScore = 1f;
        public float randomSpeedDelta = 0.3f;
        [HideInInspector, SerializeField] private LogicConnections _connections;

        private LevelPlayer _player;
        private Dictionary<int, BaseComponent> _components;
        private bool _collected = false;
        private AnimationState _animationState;

        LogicConnections ILogicComponent.Connections { get => _connections; set => _connections = value; }

        Mobge.Animation.AnimationState PlayAnimation(int animationId, bool loop) {
            if (animation == null || animationId < 0) return null;
            return animation.PlayAnimation(animationId, loop);
        }
        private void OnTriggerEnter(Collider other) {
            if (!ColliderEnabled) return;
            var collisionBody = other.attachedRigidbody;
            if (collisionBody == null) return;
            if ((layerMask.value & (1 << collisionBody.gameObject.layer)) == 0) return;
            Collected();
        }
        private void OnTriggerEnter2D(Collider2D collision) {
            if (!ColliderEnabled) return;
            var collisionBody = collision.attachedRigidbody;
            if (collisionBody == null) return;
            if ((layerMask.value & (1 << collisionBody.gameObject.layer)) == 0) return;
            Collected();
        }

        private void Collected() {
            var baseLevelPlayer = _player as BaseLevelPlayer;
            if (baseLevelPlayer != null)
                baseLevelPlayer.Score += 1f;
            var animationState = PlayAnimation(collectedAnimation, false);
            _connections?.InvokeSimple(this, 0, transform, _components);
            if (animationState == null) {
                OnFinish(true, null);
            }
            else {
                ColliderEnabled = false;
                Simulated = false;
                takeEffect.SpawnItem(transform.position);
                _player.RoutineManager.DoAction(OnFinish, animationState.Duration);
            }
            _collected = true;
        }

        public bool ColliderEnabled {
            get {
                if (collider) {
                    return collider.enabled;
                }
                if (collider3D) {
                    return collider3D.enabled;
                }
                return false;
            }
            set {
                if (collider) {
                    collider.enabled = value;
                }
                else if (collider3D) {
                    collider3D.enabled = value;
                }
            }
        }
        public bool Simulated {
            get {
                if (collider) {
                    return collider.attachedRigidbody.simulated;
                }
                if (collider3D) {
                    return collider3D.attachedRigidbody.detectCollisions;
                }
                return false;
            }
            set {
                if (collider) {
                    var rb = collider.attachedRigidbody;
                    if (rb) {
                        rb.simulated = value;
                    }
                }
                else if (collider3D) {
                    var rb = collider3D.attachedRigidbody;
                    if (rb) {
                        rb.detectCollisions = value;
                    }
                }
            }
        }
        private void OnFinish(bool completed, object data) {
            if (completed) {
                gameObject.SetActive(false);
            }
        }
        void IComponentExtension.Start(in BaseComponent.InitArgs initData) {
            _collected = false;
            enabled = true;
            _components = initData.components;
            if (animation) {
                animation.EnsureInit();
            }

            if (appearAnimation>=0) {
                _animationState = animation.PlayAnimation(appearAnimation, false);
            }

            if (type == 0) {
                if (initData.player is BaseLevelPlayer bPlayer) {
                    _player = bPlayer;
                    bPlayer.TotalScore += availableScore;
                }
            }
        }
        private void ApplyRandom(AnimationState animationState) {
            if (animationState != null && randomSpeedDelta != 0) {
                animationState.Speed = UnityEngine.Random.Range(1 - randomSpeedDelta, 1 + randomSpeedDelta);
            }
        }
        private void Update() {
            if (!_collected && animation != null) {
                if (_animationState == null || !animation.IsPlaying(_animationState)) {
                    float totalWeight = 0f;
                    for (int i = 0; i < cycleAnimations.Length; i++) {
                        totalWeight += GetCycleAnimationWeight(i);
                    }
                    float r = Random.Range(0f, totalWeight);
                    float c = 0f;
                    for (int i = 0; i < cycleAnimations.Length; i++) {
                        c += GetCycleAnimationWeight(i);
                        if (r > c) continue;
                        _animationState = animation.PlayAnimation(cycleAnimations[i], false);
                        ApplyRandom(_animationState);
                        break;
                    }
                }

            }
            else {
                enabled = false;
            }
        }
        private float GetCycleAnimationWeight(int i) {
            return cycleAnimationWeights == null || cycleAnimationWeights.Length <= i ? 1f : cycleAnimationWeights[i];
        }
        object ILogicComponent.HandleInput(ILogicComponent sender, int index, object input) {
            return null;
        }
#if UNITY_EDITOR
        void ILogicComponent.EditorInputs(List<LogicSlot> slots) { }
        void ILogicComponent.EditorOutputs(List<LogicSlot> slots) {
            slots.Add(new LogicSlot("collected", 0, typeof(Transform)));
        }
#endif
    }
}