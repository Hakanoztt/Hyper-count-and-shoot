using System;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.Platformer;
using UnityEngine;

namespace Mobge.Core.Components {
    public class BasicCameraComponent : ComponentDefinition<BasicCameraComponent.Data> {
        

        [Serializable]
        public class Data : ACameraComponentData {
            public bool activateOnStart;



            private Pose _oldPose;

            private Transform _target;
            private Pose _lastTargetPose = Pose.identity;

            private Camera _camera;






            public Pose CurrentCameraPose { get; private set; }

            public override Camera CameraToUpdate {
                get => _camera;
                set {
                    if (_camera != value) {
                        _camera = value;
                        StatusUpdated();
                    }
                }
            }


            private Pose TargetPose {
                get {
                    var targetPos = data.offset.GetTransformedBy(_lastTargetPose);
                    return targetPos;
                }
            }

            public override void Start(in InitArgs initData) {
                base.Start(initData);

                if (_camera == null) {
                    CameraToUpdate = Camera.main;
                }
                _lastTargetPose = new Pose(this.position, this.rotation);

                LevelPlayer.FixedRoutineManager.DoAction(DelayedStart);
            }

            private void DelayedStart(bool complete, object data) {
                if (_target == null) {
                    var target = Connections.InvokeSimple(this, 0, null, Components);
                    if (target is Transform t) {
                        SetTarget(t);
                    }
                }
                if (activateOnStart) {
                    Active = true;
                }
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


            public void SetActive(bool teleportCamera = true) {
                Active = true;
                if (teleportCamera) {
                    this.TeleportToTarget();
                }
            }


            public void SetTarget(Transform target, bool activate = false) {
                _target = target;
                if (activate) {
                    Active = true;
                }
            }
            public void SetTarget(Pose pose, bool activate = false) {
                _target = null;
                _lastTargetPose = pose;
                if (activate) {
                    Active = true;
                }
            }
            public void TeleportToTarget() {
                if (_camera != null) {
                    var targetPos = data.offset.GetTransformedBy(_lastTargetPose);
                    CurrentCameraPose = targetPos;
                    _oldPose = targetPos;
                    var tr = _camera.transform;
                    tr.position = targetPos.position;
                    tr.rotation = targetPos.rotation;

                }
            }


            public override void UpdateCamera() {
                if (_target != null) {
                    _lastTargetPose = new Pose(_target.position, _target.rotation);
                }
                if (_camera != null) {
                    var targetPos = data.offset.GetTransformedBy(_lastTargetPose);

                    float dt = Time.deltaTime;
                    var cp = CurrentCameraPose;
                    if (dt > 0) {
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

    }
}
            