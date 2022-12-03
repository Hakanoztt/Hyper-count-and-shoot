using System;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.Core.Components;
using Mobge.Platformer;
using UnityEngine;
using static Mobge.HyperCasualSetup.RoadGenerator.RoadCameraComponent;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    public class RunnerCameraComponent : ComponentDefinition<RunnerCameraComponent.Data> {
        [Serializable]
        public class Data : BaseComponent, IRotationOwner {

            public float moveForce = 150;
            public float angularMoveForce = 12;
            public float sideMoveRate = 0.3f;
            public float upMoveRate = 0.2f;

            public override LogicConnections Connections { get => connections; set => connections = value; }
            [SerializeField, HideInInspector] private LogicConnections connections;

            public Quaternion rotation = Quaternion.identity;

            public bool updateMainCamera = true;

            private Dictionary<int, BaseComponent> _components;

            private LevelPlayer _player;
            private UpdateRoutines _updateRoutines;

            private Target _target;
            private Camera _camera;

            private Pose _followOffset;
            private Pose _oldPose;

            private Rigidbody _cameraTarget;

            private InterpolationCalculator _interpolation;
            private bool _goTargetImmediately;

            public Pose CurrentCameraPose { get; set; }
            public Camera CameraToUpdate { get => _camera; set => _camera = value; }

            Quaternion IRotationOwner.Rotation { get => rotation; set => rotation = value; }

            public Pose FollowOffset {
                get => _followOffset;
                set => _followOffset = value;
            }
            
            public Pose OldPose {
                get => _oldPose;
                set => _oldPose = value;
            }
            public override void Start(in InitArgs initData) {
                _player = initData.player;
                _components = initData.components;

                _player.FixedRoutineManager.DoAction(DelayedStart, 0);
                if (updateMainCamera) {
                    _camera = Camera.main;
                    CurrentCameraPose = _camera != null ? new Pose(_camera.transform.position, _camera.transform.rotation) : new Pose(position, rotation);
                }

                _oldPose = new Pose(position, rotation);

                _cameraTarget = new GameObject("camera target").AddComponent<Rigidbody>();
                _cameraTarget.transform.SetParent(initData.parentTr, false);
            }



            private void DelayedStart(bool complete, object data) {
                if (connections != null) {
                    var comp = connections.InvokeSimple(this, 0, null, _components);
                    if (comp is Component c) {
                        var chr = c.GetComponentInChildren<IRunner>();
                        if (chr != null) {
                            SetTarget(chr, true);
                        }
                    }
                }
            }

            public void SetTarget(IRunner character, bool goTargetImmediately = false) {
                var chr = character;
                _goTargetImmediately = goTargetImmediately;
                _target.SetCharacter(chr);
                if (chr != null) {

                    _updateRoutines.EnsureStart(this);
                }
                else {
                    _updateRoutines.Stop();
                }
                //var tr = _camera.transform;
                //_prevCameraPos = new Pose(tr.position, tr.rotation);
            }


            private void UpdateCamera(float progress, object data) {


                if (!_target.HasCharacter) {

                    _updateRoutines.Stop();
                }
                else {
                    if (_target.TryGetTargetPose(this, out var pose)) {

                        //Debug.Log(tp.rotation);
                        var worlPose = _followOffset.GetTransformedBy(pose);
                        var iPose = worlPose;// = Interpolate(worlPose, _target.Velocity, _target.AngularVelocity);
                        //Debug.Log(worlPose.rotation);
                        float dt = Time.deltaTime;

                        var cp = CurrentCameraPose;
                        if (_goTargetImmediately) {
                            _goTargetImmediately = false;
                            cp.position = worlPose.position;
                            cp.rotation = worlPose.rotation;
                        }

                        else if (dt > 0) {
                            cp.position = Side2DCamera.CameraApproach(iPose.position, cp.position, new Vector3(moveForce, moveForce, moveForce), _oldPose.position, dt);

                            cp.rotation =ACameraComponentData.ApproachRotation(iPose.rotation, cp.rotation, this.angularMoveForce, _oldPose.rotation, dt);
                        }

                        _oldPose = CurrentCameraPose;
                        CurrentCameraPose = cp;

                        if (_camera != null) {

                            var tr = _camera.transform;
                            tr.position = cp.position;
                            tr.rotation = cp.rotation;
                        }
                        //_prevCameraPos = _pose;

                    }
                }
            }



            Pose Interpolate(in Pose pose, Vector3 velocity, Vector3 angularVelocity) {
                _interpolation.Update(out float rate);
                rate -= 1;
                //Debug.Log(pose.position + " " + velocity.magnitude + " " + rate);
                //Debug.Log(dif.magnitude);
                //Vector3 dif = nextPose.position - prevPose.position;
                float fdt = Time.fixedDeltaTime;
                Pose p;
                p.position = pose.position + velocity * (fdt * rate);
                p.rotation = pose.rotation;
                p.rotation.ApplyAngularVelocity(fdt * rate, angularVelocity);

                return p;
            }



            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case 0:
                        return this;
                }
                return null;
            }

            private struct Target {
                private IRunner _character;
                private bool _isReady;
                public Vector3 Velocity => _character.Velocity;
                //public Vector3 AngularVelocity => _character.Rigidbody.angularVelocity;
                public bool HasCharacter => _character != null;

                public void SetCharacter(IRunner character) {
                    if (character != _character) {
                        _character = character;
                        _isReady = false;

                    }
                }
                public bool TryGetTargetPose(Data data, out Pose pose) {
                    pose = default;
                    if (!_isReady) {
                        if (_character != null) {
                            if (_character.IsRunning) {
                                _isReady = true;
                                var tp = GetTargetPose(data);
                                data._followOffset = data._oldPose.GetInverseTransformedBy(tp);
                                data._camera.transform.position = data._oldPose.position;
                                data._camera.transform.rotation = data._oldPose.rotation;
                            }
                            else {
                                return false;
                            }
                        }
                        else {
                            return false;
                        }
                    }
                    pose = GetTargetPose(data);
                    return true;
                }

                private Pose GetTargetPose(Data data) {

                    var roadPos = _character.AnchorPose;
                    var side = _character.SideOffset;
                    var up = _character.UpOffset;
                    roadPos.position += side * data.sideMoveRate * roadPos.right;
                    roadPos.position += up * data.upMoveRate * roadPos.up;
                    return roadPos;

                }
            }
            private struct UpdateRoutines {
                private RoutineManager.Routine _update;


                public void EnsureStart(Data data) {
                    if (_update.IsFinished) {
                        _update = data._player.RoutineManager.DoRoutine(data.UpdateCamera);
                        //_fixedUpdate = data._player.FixedRoutineManager.DoRoutine(data.FixedUpdate);
                    }
                }
                public void Stop() {
                    _update.Stop();
                    //_fixedUpdate.Stop();
                }
            }

#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("this", 0, null, typeof(Data)));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("follow target", 0, null, typeof(Component)));
            }
#endif
        }
    }
}
            