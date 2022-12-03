using Mobge.Core;
using Mobge.Core.Components;
using Mobge.Graph;
using Mobge.Platformer.Character;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {

    public partial class RunnerController : MonoBehaviour, RoadCameraComponent.IRunner, IRoadElement {


        public const string c_tag = "RunnerController";


        public static bool TryGet(Collider c, out RunnerController controller) {
            if (c.CompareTag(c_tag)) {
                controller = c.GetComponent<RunnerController>();
                return controller != null;
            }
            controller = null;
            return false;
        }

        public CharacterInput input;

        public AnimationModule animationModule;

        public MoveModule moveModule;

        private ModifierManager _modifierManager;
        private LevelPlayer _player;
        private RoadTracker _roadTracker;
        private float _lastGroundStartTime;

        public LevelPlayer Player => _player;

        public GroundModule groundModule = new GroundModule() {
            minOnGroundTime = 0.05f,
            groundCheckHeight = 0.3f,
        };


        public MoveData moveData = new MoveData() {
            maxSideLimit = 3,
            sideSpeed = 5,
            speed = 5,
            gravityScale = 1,
            rotationLerpAmount = 0.2f,
        };




        public bool OnGround => groundModule.GetOnGround(this);
            
        public float Time => UnityEngine.Time.time;
        public float DeltaTime => UnityEngine.Time.deltaTime;

        public Vector3 GroundNormal => groundModule.GroundNormal;

        public Vector3 RoadDirectionDerivative {
            get => _roadTracker.CurrentDerivative;
        }
        public Vector3 RoadDirectionSecondDerivative {
            get => _roadTracker.CurrentSecondDerivative;
        }
        public int RoadPieceIndex => _roadTracker.CurrentItemIndex;
        public float RoadPiecePercentage => _roadTracker.CurrentPercentage;
        public RoadGeneratorComponent.Data Road { get => _roadTracker.roadGenerator; }

        #region camera interface
        public Vector3 Velocity => moveModule.Velocity;
        public Pose AnchorPose => moveModule.AnchorePose;
        public float SideOffset => moveModule.SideOffset;
        public float UpOffset => moveModule.UpOffset;
        public bool IsRunning => enabled && _roadTracker.IsValid;

        #endregion camera interface

        protected void Awake() {
            gameObject.tag = c_tag;
            input = new CharacterInput();
            animationModule.Init(this);
            _modifierManager.Initialize(this);
        }

        void IRoadElement.SetTracker(LevelPlayer player, in RoadTracker tracker, in Pose localOffset) {
            _roadTracker = tracker;
            _player = player;
            moveModule.Init(localOffset, _roadTracker);


            _player.RoutineManager.DoRoutine(MoveUpdate);


        }

        public void SetRoadParameters(int pieceIndex, float percentage) {
            this._roadTracker.Update(this._roadTracker.roadGenerator, pieceIndex, percentage);
        }

        public bool AddModifier(IModifier modifier) {
            return _modifierManager.AddModifier(modifier, Time);
        }

        private void MoveUpdate(float progress, object data) {
            if (!enabled) {
                return;
            }
            float deltaTime = DeltaTime;
            moveModule.moveData = this.moveData;
            _modifierManager.Update(this, ref moveModule.moveData, out Vector3 thisPos);
            var flags = moveModule.Update(this, input.MoveInput, deltaTime, thisPos);
            groundModule.Update(this, flags);

            input.Consumed();

            animationModule.Update();
        }
        public void DisableGround(float time = 0.15f) {
            groundModule.DisableGround(this, time);
        }
        [Serializable]
        public struct MoveModule {

            [OwnComponent(findAutomatically = true), SerializeField] private CharacterController controller;

            [NonSerialized] public MoveData moveData;

            public UpOffsetMode upOffsetMode;

            private Pose _anchorPose;
            private float _sideOffset;
            private float _upOffset;


            public float SideOffset => _sideOffset;
            public Pose AnchorePose => _anchorPose;
            public float UpOffset => _upOffset;

            public Vector3 Velocity => controller.velocity;


            private Vector3 _roadUpDirection;

            public void Init(in Pose localOffset, in RoadTracker roadTracker) {
                _sideOffset = localOffset.position.x;
                _roadUpDirection = roadTracker.roadGenerator.rotation * Vector3.up;
            }

            public CollisionFlags Update(RunnerController controller, Vector2 moveInput, float deltaTime, in Vector3 thisPos) {
                UpdateCurrentValues(ref controller._roadTracker, thisPos, out var anchor);
                ApplyInput(ref controller._roadTracker, moveInput, deltaTime, in anchor);
                _anchorPose = controller._roadTracker.Current;
                if (upOffsetMode == UpOffsetMode.AllignToRoadRoot) {
                    var f = GetRootModeForward(_anchorPose.rotation, out _);
                    _anchorPose.rotation = Quaternion.LookRotation(f, _roadUpDirection);
                }

                UpdatePosition(controller, thisPos, deltaTime, controller.groundModule.IsForcedAir(controller));
                UpdateRotation(moveInput, deltaTime);
                return this.controller.collisionFlags;
            }
            private void UpdateCurrentValues(ref RoadTracker tracker, in Vector3 thisPos, out Pose currentAnchor) {
                // update tracker progress
                var trackerPose = tracker.Current;
                float trackerZDif;
                if (upOffsetMode == UpOffsetMode.AllignToRoadRoot) {
                    var f = GetRootModeForward(trackerPose.rotation, out var roadForward);
                    float offset = Vector3.Dot(trackerPose.position - thisPos, f);
                    var forwardOffset = offset * f;

                    trackerZDif = GetRootModeStep(forwardOffset, roadForward);
                }
                else {
                    trackerZDif = Vector3.Dot(trackerPose.position - thisPos, trackerPose.forward);
                }
                float ignoreAmount = 0.001f;
                if (trackerZDif < -ignoreAmount) {
                    tracker.MoveForwardSmallAmount(-trackerZDif);
                }
                else if (trackerZDif > ignoreAmount) {
                    tracker.MoveBackwardSmallAmount(trackerZDif);
                }


                currentAnchor = tracker.Current;
                _sideOffset = Vector3.Dot(thisPos - currentAnchor.position, currentAnchor.right);

            }
            private Vector3 GetRootModeForward(in Quaternion q, out Vector3 originalForward) {
                originalForward = q * Vector3.forward;
                var forward = originalForward - Vector3.Dot(originalForward, _roadUpDirection) * _roadUpDirection;
                return forward.normalized;
            }
            private float GetRootModeStep(in Vector3 step, in Vector3 roadForward) {
                return step.sqrMagnitude / Vector3.Dot(step, roadForward);
            }

            private void ApplyInput(ref RoadTracker tracker, Vector2 moveInput, float deltaTime, in Pose currentAnchor) {
                float step = moveInput.y * deltaTime * moveData.speed;
                if (step != 0) {
                    if (upOffsetMode == UpOffsetMode.AllignToRoadRoot) {
                        var f = GetRootModeForward(currentAnchor.rotation, out var originalForward);
                        step = GetRootModeStep(step * f, originalForward);
                    }
                    if (step > 0) {
                        tracker.MoveForwardSmallAmount(step);
                    }
                    else {
                        tracker.MoveBackwardSmallAmount(-step);
                    }
                }

                _sideOffset += moveData.sideSpeed * moveInput.x * deltaTime;
                _sideOffset = Mathf.Clamp(_sideOffset, -moveData.maxSideLimit, moveData.maxSideLimit);
            }

            private void UpdatePosition(RunnerController runner, Vector3 thisPos, float dt, bool forcedAir) {
                var up = _anchorPose.up;
                var right = _anchorPose.right;
                _upOffset = Vector3.Dot(thisPos - _anchorPose.position, up);
                var targetPos = _anchorPose.position + up * _upOffset + _sideOffset * right;



                var motion = targetPos - controller.transform.position;

                ApplyGravity(ref motion, up, dt, runner);

                controller.Move(motion);

            }
            void ApplyGravity(ref Vector3 motion, Vector3 up, float dt, RunnerController runner) {

                float gravity = Physics.gravity.y * moveData.gravityScale;
                bool onGround = !runner.groundModule.IsForcedAir(runner) && controller.isGrounded;
                if (Mathf.Abs(gravity) >= 0.001f) {
                    switch (moveData.gravityMode) {
                        case GravityMode.Global: {
                                var vel = controller.velocity;
                                if (!onGround) {
                                    vel.y += gravity * dt;

                                    motion.y = vel.y * dt;
                                }
                                else {
                                    vel.y = 0;
                                    motion.y = Mathf.Min(motion.y, -Vector3.Dot(vel, runner.groundModule.GroundNormal) * dt);
                                }

                            }

                            break;
                        case GravityMode.LocalToRoad: {

                                if (!onGround) {
                                    float yVel = Vector3.Dot(controller.velocity, up);
                                    yVel += gravity * dt;
                                    motion -= up * Vector3.Dot(motion, up);
                                    motion += (yVel * dt) * up;
                                }
                                else {
                                    var n = runner.groundModule.GroundNormal;
                                    motion -= Vector3.Dot(motion, up) * up;

                                    motion += n * (-Vector3.Dot(controller.velocity, n) * dt);
                                }

                            }
                            break;
                        case GravityMode.None:
                        default:
                            break;
                    }
                }

            }

            private void UpdateRotation(Vector2 input, float deltaTime) {
                bool none = false;
                Vector3 forward = Vector3.zero;
                Vector3 up = Vector3.up;
                switch (moveData.rotatoinMode) {
                    case RotationUpdateMode.LookForward:
                        forward = _anchorPose.forward;
                        up = _anchorPose.up;
                        break;
                    case RotationUpdateMode.InputDirection:
                        if (input.sqrMagnitude > 0.001f) {
                            forward = _anchorPose.forward * input.y;
                            forward += _anchorPose.right * input.x;
                            up = _anchorPose.up;
                        }
                        else {
                            none = true;
                        }

                        break;
                    case RotationUpdateMode.InputBySpeed:
                        if (input.sqrMagnitude > 0.001f) {
                            var inp = Vector2.Scale(input, new Vector2(moveData.sideSpeed, moveData.speed));
                            forward = _anchorPose.forward * inp.y;
                            forward += _anchorPose.right * inp.x;
                            up = _anchorPose.up;
                        }
                        else {
                            none = true;
                        }
                        break;
                    default:
                    case RotationUpdateMode.None:
                        none = true;
                        break;
                }

                if (!none) {
                    if (forward.sqrMagnitude > 0.0001f) {
                        if (deltaTime != 0) {
                            var ctr = controller.transform;
                            var target = Quaternion.LookRotation(forward, up);
                            if (moveData.rotationLerpAmount == 0) {
                                ctr.rotation = target;

                            }
                            else {
                                var lp = MathExtensions.CalculateLerpAmount(moveData.rotationLerpAmount, deltaTime);
                                ctr.rotation = Quaternion.LerpUnclamped(ctr.rotation, target, lp);
                            }
                        }
                    }
                }
            }

        }


        [Serializable]
        public struct GroundModule {
            public float minOnGroundTime;
            public float groundCheckHeight;
            public Vector3 GroundNormal { get; private set; }
            private float _disableGroundUntil;
            private bool _onGround;
            //private float _groundStartTime;
            public bool IsForcedAir(RunnerController controller) {
                return  _disableGroundUntil > controller.Time;
            }
            public bool GetOnGround(RunnerController cont) {
                //float time = cont.Time;
                if (IsForcedAir(cont)) {
                    return false;
                }
                return _onGround;// || (time - _groundStartTime <= minOnGroundTime);
            }

            public void Update(RunnerController cont, CollisionFlags flags) {
                GroundNormal = Vector3.up;
                if (IsForcedAir(cont)) {
                    _onGround = false;
                    return;
                }
                // bool contGround = (flags & CollisionFlags.Below) != CollisionFlags.None;
                //bool start = !_onGround & contGround;
                //if (start) {
                //    _groundStartTime = cont.Time;
                //}
                ///_onGround = contGround;

                _onGround = false;

                var ray = new Ray(cont.transform.position, -cont.transform.up);
                Debug.DrawLine(ray.origin, ray.origin + ray.direction * groundCheckHeight);
                if (Physics.Raycast(ray,out var rh, groundCheckHeight, 1 << 0, QueryTriggerInteraction.Ignore)) {
                    _onGround = true;
                    GroundNormal = rh.normal;
                }

            }


            public void DisableGround(RunnerController cont, float time) {
                _disableGroundUntil = Mathf.Max(cont.Time + time, _disableGroundUntil);
            }
        }

        public struct ModifierManager {
            private List<ModifierReference> _modifiers;
            public int Count => _modifiers.Count;
            public void Initialize(RunnerController controller) {
                _modifiers = new List<ModifierReference>();
            }
            public bool AddModifier(IModifier modifier, float time) {
                if (IndexOf(modifier) < 0) {
                    ModifierReference r;
                    r.modifier = modifier;
                    r.startTime = time;
                    _modifiers.Add(r);
                    return true;
                }
                return false;
            }
            private int IndexOf(IModifier modifier) {
                for (int i = 0; i < _modifiers.Count; i++) {
                    if (_modifiers[i].modifier == modifier) {
                        return i;
                    }
                }
                return -1;
            }
            public void Update(RunnerController runnerController, ref MoveData data, out Vector3 position) {
                var tr = runnerController.transform;
                Pose pose = new Pose(tr.position, tr.rotation);
                if (_modifiers.Count == 0) {
                    position = pose.position;
                    return;
                }
                Pose originalPose = pose;
                float time = runnerController.Time;
                int i = 0;
                do {
                    var mod = _modifiers[i];
                    bool result = mod.modifier.Modify(time - mod.startTime, ref data, ref pose, runnerController);
                    if (!result) {
                        _modifiers.RemoveAt(i);
                    }
                    else {
                        i++;
                    }
                }
                while (i < _modifiers.Count);

                if (pose.rotation != originalPose.rotation) {
                    tr.rotation = pose.rotation;
                }
                //if (pose.position != originalPose.position) {
                //    runnerController.moveModule.controller.Move(pose.position - runnerController.transform.position);
                //}
                position = pose.position;
            }
            private struct ModifierReference {
                public IModifier modifier;
                public float startTime;
            }
        }

        [Serializable]
        public struct MoveData {

            public float speed;
            public float sideSpeed;
            public float maxSideLimit;
            public RotationUpdateMode rotatoinMode;
            public GravityMode gravityMode;
            public float gravityScale;
            public float rotationLerpAmount;
            //public Transform alignToGround;
        }
        public enum UpOffsetMode {
            AllignToRoad = 0,
            AllignToRoadRoot = 1
        }
        public enum GravityMode {
            Global = 0,
            LocalToRoad = 1,

            None = -1
        }

        public enum RotationUpdateMode {
            LookForward = 0,
            InputDirection = 1,
            InputBySpeed = 2,
            
            None = -1
        }

        public interface IModifier {
            bool Modify(float modifierTime, ref MoveData data, ref Pose pose, RunnerController controller);
        }

#if UNITY_EDITOR
        public void OnDrawGizmosSelected() {
            var pos = transform.position;
            var right = transform.right;
            Gizmos.color = Color.yellow;

            var p1 = pos + right * moveData.maxSideLimit;
            var p2 = pos - right * moveData.maxSideLimit;
            Gizmos.DrawLine(p1, p2);
            var forward = transform.forward;
            var sideStep = forward * moveData.maxSideLimit * 0.3f;
            Gizmos.DrawLine(p1 + sideStep, p1 - sideStep);
            Gizmos.DrawLine(p2 + sideStep, p2 - sideStep);

            Gizmos.color = Color.white;
        }

#endif
    }
}