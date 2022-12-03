using Mobge.Character3D;
using Mobge.Core;
using Mobge.Core.Components;
using Mobge.Platformer.Character;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {

    public class RoadRunnerCharacter : MonoBehaviour , RoadCameraComponent.IRunner, IRoadElement {

        public MoveModule moveModule = new MoveModule() {
            speed = 12,
            sideSpeed = 5,
            maxSideLimit = 2,
            maxForce = 300,
            maxTorque = float.PositiveInfinity,
            turnPercentage = 0f,
            rotateAccordingToGround = false,
            rotationUpdateMode = RotationUpdateMode.Update,
        };
        public JumpModule jumpModule;
        public CharacterInput input;

        private IBody _body;

        public IBody Body {
            get => _body;
            set {
                _body = value;
            }
        }

        private RoadTracker _roadTracker;
        private LevelPlayer _player;


        public bool IsRunning {
            get => enabled && _roadTracker.IsValid;
        }
        public LevelPlayer Player => _player;
        public Vector3 Velocity {
            get => _body.Velocity;
        }

        public Pose AnchorPose => moveModule.CenterPose;

        public float SideOffset => moveModule.SideOffset;
        public float UpOffset { get => jumpModule.UpOffset; set => jumpModule.UpOffset = value; }

        public bool OnGround {
            get => _body.OnGround;
        }

        private void Awake() {
            input = new CharacterInput();
        }



        private void FindBody() {
            if (_body == null) {
                CharacterBody cb = GetComponent<CharacterBody>();
                if (cb != null) {
                    _body = new CharacterBodyWrapper(cb);
                }
                else {
                    CharacterController cc = GetComponent<CharacterController>();
                    if (cc != null) {
                        _body = new CharacterControllerWrapper(cc);
                    }
                }
            }
        }

        private void MoveUpdate(float progress, object data) {
            if (!enabled) {
                return;
            }
            float deltaTime = Time.deltaTime;
            moveModule.UpdatePosition(ref _roadTracker, input.MoveInput, _body, deltaTime);
            
            input.Consumed();
        }

        void IRoadElement.SetTracker(LevelPlayer player, in RoadTracker tracker, in Pose localOffset) {

            _player = player;
            FindBody();

            _roadTracker = tracker;

            moveModule.Init(localOffset, _roadTracker);
            jumpModule.Init();

            if (_body != null) {
                if (_body.RequiredFixedUpdate) {
                    _player.FixedRoutineManager.DoRoutine(MoveUpdate);
                }
                else {
                    _player.RoutineManager.DoRoutine(MoveUpdate);
                }
            }
        }
        [Serializable]
        public struct MoveModule {
            public float speed;
            public float sideSpeed;
            public float maxSideLimit;
            public float maxForce;
            public float maxTorque;
            public float turnPercentage;
            public bool rotateAccordingToGround;
            public bool dontStuck;

            public RotationUpdateMode rotationUpdateMode;
            public float SideOffset { get; private set; }
            private Pose _pose;
            public Pose CenterPose => _pose;
            
            public Quaternion Rotation => _pose.rotation;



            public void Init(in Pose localPose, in RoadTracker tracker) {
                _pose = tracker.Current;
                SideOffset = localPose.position.x;

            }
            public void UpdatePosition(ref RoadTracker tracker, Vector2 moveInput, IBody body, float deltaTime) {
                Vector3 thisPos = body.Position;
                if (moveInput.y > 0) {
                    float step = speed * deltaTime * moveInput.y;
                    if (!dontStuck) {
                        var dif = Vector3.Dot(thisPos - _pose.position, _pose.forward);
                        if (dif < 0) {
                            step += dif;
                        }
                    }
                    if (step > 0) {
                        tracker.MoveForwardSmallAmount(step);
                        //var p = _pose;

                        _pose = tracker.Current;
                        //Debug.Log((p.position - _pose.position).magnitude / Time.fixedDeltaTime);
                    }
                    else {
                        Debug.DrawLine(thisPos, _pose.position, Color.red);
                        Debug.DrawLine(_pose.position, _pose.position + Vector3.up*3, Color.blue);
                    }
                }
                Axis a = new Axis(_pose.forward, _pose.up);
                a.right = -a.right;
                SideOffset = Vector3.Dot(thisPos - _pose.position, a.right);
                ClampSides(body, a.right, ref thisPos);
                SideOffset += moveInput.x * sideSpeed * deltaTime;
                SideOffset = Mathf.Clamp(SideOffset, -maxSideLimit, maxSideLimit);

                Vector3 target = _pose.position;
                target += SideOffset * a.right;
                body.MoveTo(target, maxForce);
                switch (rotationUpdateMode) {
                    default:
                    case RotationUpdateMode.None:
                        break;
                    case RotationUpdateMode.Update:
                        if (TryCalculateLookRotation(body, a, out var rr)) {
                            body.RotateTo(rr, maxTorque);

                        }
                        break;
                }
            }

            private void ClampSides(IBody body, Vector3 right, ref Vector3 pos) {
                bool bigger;
                float eccessPos;
                if (SideOffset > maxSideLimit) {
                    bigger = true;
                    eccessPos = SideOffset - maxSideLimit;
                    SideOffset = maxSideLimit;
                }
                else if (SideOffset < -maxSideLimit) {
                    bigger = false;
                    eccessPos = SideOffset + maxSideLimit;
                    SideOffset = -maxSideLimit;
                }
                else {
                    return;
                }
                pos -= right * eccessPos;
                body.Position = pos;
                var eccessSpeed = Vector3.Dot(body.Velocity, right);
                if (eccessSpeed > 0 == bigger) {
                    body.Velocity -= eccessSpeed * right;
                }
            }

            private bool TryCalculateLookRotation(IBody body, in Axis axis, out Quaternion rotation){
                var vel = body.Velocity;
                float limit = (speed * 0.05f);
                var right = Vector3.Dot(axis.right, vel)* axis.right;
                var forward = Vector3.Dot(axis.forward, vel)* axis.forward;
                var dir = forward + right * turnPercentage;
                if (rotateAccordingToGround) {
                    if (body.OnGround) {
                        dir -= Vector3.Dot(body.GroundNormal, dir) * body.GroundNormal;
                    }
                }
                if (dir.sqrMagnitude <= limit * limit) {
                    rotation = body.Rotation;
                    return true;
                }
                rotation = Quaternion.LookRotation(dir, axis.up);
                return true;
            }
        }

        [Serializable]
        public struct JumpModule {
            public AnimationCurve jumpCurve;
            public float maxJumpHeight;
            public float UpOffset { get; set; }
            public float jumpEndSpeedMultiplayer;

            public bool variableJumpHeight;

            private float _verticalVelocity;
            private float _jumpTime;
            private float _lastGroundUpOffset;
            private bool _jumping;

            private bool GroundDisabled {
                get {
                    float passed = Time.fixedTime - _jumpTime;
                    return passed < 0.2f;
                }
            }
            public void Init() {
                _jumpTime = float.NegativeInfinity;
            }

            public void Update(RoadRunnerCharacter runner, ref Vector3 position, float deltaTime) {
                bool onGround = runner.OnGround;

                ref MoveModule mm = ref runner.moveModule;
                var pose = mm.CenterPose;

                UpOffset = Vector3.Dot(position - pose.position, pose.up);
                bool jumpInput = runner.input.Jump;
                if (onGround) {
                    //_verticalVelocity = runner.Rigidbody.velocity.y;
                    //Plane p = new Plane(runner.controller., Vector3.zero);
                    //var vel = runner.body.Rigidbody.velocity;
                    //vel.y = 0;
                    //p.Raycast(new Ray(vel, Vector3.up), out float enter);
                    _verticalVelocity = 0;// runner.controller.velocity.y;
                    //Debug.Log(_verticalVelocity);
                    _lastGroundUpOffset = UpOffset;
                    if (jumpInput && !GroundDisabled) {
                        _jumpTime = Time.fixedTime;
                    }
                }
                else {
                    _verticalVelocity += Physics.gravity.y * deltaTime;
                }
                //pos += runner.moveModule.CurrentPose.up * _verticalVelocity * Time.fixedDeltaTime;

                HandleJump(jumpInput, deltaTime);

                //Debug.Log(_verticalVelocity);
                var up = pose.up;
                position = pose.position + mm.SideOffset * pose.right + UpOffset * up + _verticalVelocity * up * deltaTime;
            }

            private void HandleJump(bool jumpInput, float deltaTime) {
                float dt = deltaTime;
                float passed = Time.fixedTime - _jumpTime;
                float duration = jumpCurve[jumpCurve.length - 1].time;
                bool newJumping = passed < duration && (jumpInput || !variableJumpHeight);
                if (newJumping) {
                    float offset = jumpCurve.Evaluate(passed) * maxJumpHeight;
                    float target = _lastGroundUpOffset + offset;
                    _verticalVelocity = (target - UpOffset) / dt;
                    //Debug.Log("jumping");
                }
                else if (_jumping) {
                    passed = Mathf.Min(passed, duration);
                    float v1 = jumpCurve.Evaluate(passed - dt);
                    float v2 = jumpCurve.Evaluate(passed);
                    _verticalVelocity = jumpEndSpeedMultiplayer * maxJumpHeight * (v2 - v1) / dt;
                    _jumpTime = float.NegativeInfinity;
                    //Debug.Log("end jumping");

                }
                _jumping = newJumping;
            }
        }
        public enum RotationUpdateMode {
            None = 0,
            Update = 2,
        }

#if UNITY_EDITOR
        public void OnDrawGizmosSelected() {
            var pos = transform.position;
            var right = transform.right;
            Gizmos.color = Color.yellow;

            var p1 = pos + right * moveModule.maxSideLimit;
            var p2 = pos - right * moveModule.maxSideLimit;
            Gizmos.DrawLine(p1, p2);
            var forward = transform.forward;
            var sideStep = forward * moveModule.maxSideLimit * 0.3f;
            Gizmos.DrawLine(p1 + sideStep, p1 - sideStep);
            Gizmos.DrawLine(p2 + sideStep, p2 - sideStep);

            Gizmos.color = Color.white;
        }

#endif
        public interface IBody {
            Vector3 Position { get; set; }
            Quaternion Rotation { get; }
            Vector3 Velocity { get; set; }
            public bool UseGravity { get; set; }
            void MoveTo(Vector3 offset, float maxForce);
            void RotateTo(Quaternion target, float maxTorque);
            void Look(Quaternion rotation);
            void Stop();

            bool OnGround { get; }
            Vector3 GroundNormal { get; }
            bool RequiredFixedUpdate {get; }
        }
        private class CharacterBodyWrapper : IBody {
            private CharacterBody _body;

            public bool UseGravity {
                get => _body.Rigidbody.useGravity;
                set => _body.Rigidbody.useGravity = value;
            }
            public CharacterBodyWrapper(CharacterBody body) {
                _body = body;
            }

            public bool RequiredFixedUpdate => true;
            public Vector3 Position { get => _body.Rigidbody.position; set => _body.Rigidbody.position = value; }
            public Vector3 Velocity {
                get => _body.Rigidbody.velocity; 
                set {
                    _body.Rigidbody.velocity = value;
                }
            }

            public bool OnGround => _body.OnGround;

            public Vector3 GroundNormal => _body.GroundNormal;

            public Quaternion Rotation => _body.Rigidbody.rotation;

            public void Look(Quaternion rotation) {
                _body.RotateTo(rotation);
            }

            public void MoveTo(Vector3 target, float maxForce) {
                Vector3 up;
                if (_body.OnGround) {
                    up = _body.GroundNormal;
                }
                else {
                    up = Vector3.up;
                }
                _body.MoveTo(target, maxForce, up);
            }

            public void RotateTo(Quaternion target, float maxTorque) {
                _body.RotateTo(target);
            }

            public void Stop() {
                _body.Rigidbody.velocity = Vector3.zero;
                _body.Rigidbody.angularVelocity = Vector3.zero;
            }

        }
        private class CharacterControllerWrapper : IBody {
            private CharacterController _controller;
            private Vector3 _velocityDelta;
            private bool _useGravity = true;
            public bool UseGravity {
                get => _useGravity;
                set => _useGravity = value;
            }

            public CharacterControllerWrapper(CharacterController controller) {
                _controller = controller;
            }
            public bool RequiredFixedUpdate => false;

            public Vector3 Position {
                get => _controller.transform.position;
                set {
                    _controller.transform.position = value;
                }
            }
            public Vector3 Velocity {
                get => _controller.velocity + _velocityDelta;
                set {
                    _velocityDelta = value - _controller.velocity;
                    //_controller.Move((value - _controller.velocity) * Time.deltaTime);
                }
            }

            public bool OnGround => _controller.isGrounded;

            public Vector3 GroundNormal => Vector3.up;

            public Quaternion Rotation => _controller.transform.rotation;

            public void Look(Quaternion rotation) {
                _controller.transform.rotation = rotation;
            }

            public void MoveTo(Vector3 target, float maxForce) {
                float dt = Time.deltaTime;
                if (dt > 0) {
                    var velocity = _controller.velocity;
                    Vector3 offset = target - _controller.transform.position;
                    var targetVelocity = offset / dt;
                    var requiredForce = targetVelocity - velocity;
                    requiredForce.y = 0;
                    requiredForce /= dt;
                    requiredForce = Vector3.ClampMagnitude(requiredForce, maxForce);
                    if (_controller.isGrounded) {
                        //velocity.y = 0;
                    }
                    else {
                        if (UseGravity) {
                            velocity.y += Physics.gravity.y * dt;
                        }
                    }
                    velocity += _velocityDelta;
                    //Debug.Log("move to: " + (velocity + requiredForce * dt) * dt + " ------- " + _controller.isGrounded);
                    //Debug.Log("--------: " + _velocityDelta.y + "   ------------:  " + velocity.y + " -------- " + _controller.transform.position.y);
                    _controller.Move((velocity + requiredForce * dt) * dt);
                    // Debug.Log("move: " + ((velocity + requiredForce * dt) * dt));
                    // Debug.DrawLine(Position, Position + (velocity + requiredForce * dt) * dt, Color.blue);
                }

                _velocityDelta = Vector3.zero;
            }

            public void RotateTo(Quaternion target, float maxTorque) {
                _controller.transform.rotation = target;
            }

            public void Stop() {
                
            }
        }
    }
}