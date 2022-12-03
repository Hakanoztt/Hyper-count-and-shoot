using System;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.Core.Components;
using Mobge.Platformer;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    public class RoadCameraComponent : ComponentDefinition<RoadCameraComponent.Data> {
        [Serializable]
        public class Data : ACameraComponentData {

            public bool activateOnStart;

            private IRunner _target;
            private Pose _lastTargetPose = Pose.identity;
            private Camera _camera;


            private Pose _oldPose;


            public float sideMoveRate = 0.3f;
            public float upMoveRate = 0.2f;



            public Pose CurrentCameraPose { get; set; }

            public override Camera CameraToUpdate {
                get => _camera;
                set {
                    if (_camera != value) {
                        _camera = value;
                        StatusUpdated();
                    }
                }
            }

            public override void Start(in InitArgs initData) {
                base.Start(initData);

                if (_camera == null) {
                    CameraToUpdate = Camera.main;
                }

                LevelPlayer.FixedRoutineManager.DoAction(DelayedStart, 0);
            }

            private void DelayedStart(bool complete, object data) {
                if (!complete) {
                    return;
                }
                if (_target == null) {
                    if (Connections != null) {
                        var comp = Connections.InvokeSimple(this, 0, null, Components);
                        if (comp is Component c) {
                            var chr = c.GetComponentInChildren<IRunner>();
                            if (chr != null) {
                                SetTarget(chr);
                                if (activateOnStart) {
                                    Active = true;
                                    TeleportToTarget();
                                }
                            }
                        }
                    }
                }
            }

            public void SetTarget(IRunner runner) {
                _target = runner;
                if (_target != null) {
                    _lastTargetPose = CalculateRunnerPose();
                }
            }

            public void SetTarget(Pose pose) {
                _target = null;
                _lastTargetPose = pose;
            }

            public override void Activated() {
                StatusUpdated();

            }


            private void StatusUpdated() {
                if (_camera != null) {
                    var tr = _camera.transform;
                    CurrentCameraPose = new Pose(tr.position, tr.rotation);
                    _oldPose = CurrentCameraPose;
                }
            }

            public override void Deactivated() {

            }
            public void TeleportToTarget() {
                _oldPose = data.offset.GetTransformedBy(_lastTargetPose);
                var tr = _camera.transform;
                tr.position = _oldPose.position;
                tr.rotation = _oldPose.rotation;
                CurrentCameraPose = _oldPose;
            }
            public override void UpdateCamera() {


                if (_target != null) {
                    _lastTargetPose = CalculateRunnerPose();
                }
                if (_camera != null) {
                    var targetPos = data.offset.GetTransformedBy(_lastTargetPose);

                    float dt = Time.deltaTime;
                    var cp = CurrentCameraPose;
                    if(dt > 0) {
                        var moveForce = data.moveForce;
                        cp.position = Side2DCamera.CameraApproach(targetPos.position, cp.position, moveForce, _oldPose.position, dt);

                        cp.rotation = ApproachRotation(targetPos.rotation, cp.rotation, data.angularMoveForce, _oldPose.rotation, dt);

                        _oldPose = CurrentCameraPose;
                        CurrentCameraPose = cp;
                    }
                    var tr = _camera.transform;
                    tr.position = cp.position;
                    tr.rotation = cp.rotation;

                }
            }

            private Pose CurrentTargetPos {
                get {
                    if(_target == null) {
                        return _lastTargetPose;
                    }
                    return CalculateRunnerPose();
                }
            }

            private Pose CalculateRunnerPose() {
                var anc = _target.AnchorPose;
                anc.rotation = FlattenRotation(anc.rotation);
                var side = _target.SideOffset;
                var up = _target.UpOffset;
                anc.position += side * sideMoveRate * anc.right;
                anc.position += up * upMoveRate * anc.up;

                return anc;
            }

            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case 0:
                        return this;
                    case 1:
                        Active = true;
                        break;
                    case 2:
                        Active = false;
                        break;
                }
                return null;
            }


#if UNITY_EDITOR

            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("this", 0, null, typeof(Data)));
                slots.Add(new LogicSlot("activate", 1));
                slots.Add(new LogicSlot("deactivate", 2));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("target object", 0, null, typeof(Transform)));
            }
#endif
        }


        public interface IRunner {
            Vector3 Velocity { get; }
            Pose AnchorPose { get; }
            float SideOffset { get; }
            float UpOffset { get; }
            bool IsRunning { get; }

        }
    }
}
            