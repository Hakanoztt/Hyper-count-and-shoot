using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components
{
    public class PoseToPoseMovementComponent : ComponentDefinition<PoseToPoseMovementComponent.Data>
    {
        [Serializable]
        public struct Pose
        {
            public Vector2 position;
            public float angle;
            [SerializeField, HideInInspector] private int _autoPlay;
            public int AutoPlay { get => ~_autoPlay; set => _autoPlay = ~value; }

            public Pose(Vector2 position, float angle) {
                this.position = position;
                this.angle = angle;
                _autoPlay = 0;
            }
            public static Pose LerpUnclamped(in Pose p1, in Pose p2, float time) {
                Pose p;
                p.position = Vector2.LerpUnclamped(p1.position, p2.position, time);
                p.angle = Mathf.LerpUnclamped(p1.angle, p2.angle, time);
                p._autoPlay = 0;
                return p;
            }
        }
        public enum SpeedMode
        {
            ConstantTime = 0,
            AccelerationSpeed = 1,
        }

        [Serializable]
        public struct SpeedModule
        {
            public SpeedMode mode;
            public float value;
            public float value2;
            public Mobge.Animation.Curve easeMode;
            public float Time { get => value; set => this.value = value; }
            public float Speed { get => value; set => this.value = value; }
            public float Acceleration { get => value2; set => this.value2 = value; }
        }
        [Serializable]
        public class Data : BaseComponent, IRotationOwner
        {
            [SerializeField, HideInInspector] private LogicConnections _connections;
            public Pose[] poses;
            
            public SpeedModule speedModule;
            [SerializeField, HideInInspector] private Quaternion _rotation;
            public int initialPoseIndex;

            private int _selectedIndex = -1;
            private LevelPlayer _player;
            private Dictionary<int, BaseComponent> _components;
            private ActionManager.Routine _routine;
            private MovingObject[] _objects;


            private float _cachedAngle;



            public override LogicConnections Connections {
                get => _connections;
                set => _connections = value;
            }
            public Quaternion Rotation { get => _rotation; set => _rotation = value; }

            public override void Start(in InitArgs initData) {
                _player = initData.player;
                _components = initData.components;
                speedModule.easeMode.EnsureInit(true);
                _cachedAngle = MovementModule.ConvertToAngle(_rotation);
                _player.FixedActionManager.DoTimedAction(0, null, InitializeBodies, null);

            }

            private void InitializeBodies(object data, bool completed) {
                if (completed) {
                    int bodyCount = _connections.GetConnectionCount(0);
                    _objects = new MovingObject[bodyCount];
                    int index = 0;
                    var e = _connections.Invoke(this, 0, null, _components);
                    while (e.MoveNext()) {
                        var rb = e.Current as Rigidbody2D;
                        MovingObject mo;
                        mo.body = rb;
                        mo.targetIndex = -1;
                        mo.startTime = 0;
                        mo.startPose = new Pose(rb.position, rb.rotation);
                        mo.updated = false;
                        _objects[index] = mo;
                        

                        index++;
                    }
                    if (this.initialPoseIndex >= 0) {
                        var pose = GetRealPos(initialPoseIndex);
                        for (int i = 0; i < _objects.Length; i++) {
                            _objects[i].SetPoseForced(pose);
                        }
                        int autoPlay = pose.AutoPlay;
                        if (autoPlay >= 0) {
                            for (int i = 0; i < _objects.Length; i++) {
                                _objects[i].StartMoving(autoPlay);
                                _objects[i].startPose = pose;
                            }
                        }
                        EnsureRoutine();
                    }
                }
            }

            private Pose GetRealPos(int index) {
                var pose = poses[index];
                pose.position = _rotation * pose.position;
                pose.position += (Vector2)position;
                pose.angle += _cachedAngle;
                return pose;
            }


            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case 0:
                        EnsureRoutine();
                        int target = (int)(float)input;
                        if (_selectedIndex < 0) {
                            for (int i  = 0; i < _objects.Length; i++) {
                                _objects[i].StartMoving(target);
                            }
                        }
                        else {
                            _objects[_selectedIndex].StartMoving(target);
                        }
                        break;
                    case 1:
                        var pose = GetRealPos((int)(float)input);
                        if (_selectedIndex < 0) {
                            for (int i = 0; i < _objects.Length; i++) {
                                _objects[i].MoveImmediate(pose);
                            }
                        }
                        else {
                            _objects[_selectedIndex].MoveImmediate(pose);
                        }
                        break;
                    case 2:
                        if (_selectedIndex < 0) {
                            for (int i = 0; i < _objects.Length; i++) {
                                _objects[i].Stop();
                            }
                        }
                        else {
                            _objects[_selectedIndex].Stop();
                        }
                        break;
                    case 3:
                        _selectedIndex = (int)(float)input;
                        break;
                    case 10:
                        return (float)poses.Length;
                }
                return null;
            }
            private void EnsureRoutine() {
                var fam = _player.FixedActionManager;
                if (_routine.IsFinished()) {
                    _routine = fam.DoRoutine(Update);
                }
            }

            private void Update(float obj) {
                bool updated = false;
                switch (speedModule.mode) {
                    default:
                    case SpeedMode.ConstantTime:
                        for(int i = 0; i < _objects.Length; i++) {
                            bool b = UpdateConstantTime(ref _objects[i]);
                            updated = updated || b;
                        }
                        break;
                    case SpeedMode.AccelerationSpeed:
                        for (int i = 0; i < _objects.Length; i++) {
                            bool b = UpdateAcceleration(ref _objects[i]);
                            updated = updated || b;
                        }
                        break;
                }
                if (!updated) {
                    _routine.Stop();
                }
            }
            protected bool UpdateConstantTime(ref MovingObject obj) {
                if (obj.targetIndex < 0) {
                    if (obj.updated) {
                        obj.Stop();
                    }
                    return false;
                }
                float passedTime = Time.fixedTime - obj.startTime;
                var prog = passedTime / speedModule.Time;
                Pose target = GetRealPos(obj.targetIndex);
                if (prog >= 1) {
                    obj.MoveToPose(target);
                    if (target.AutoPlay < 0) {
                        obj.targetIndex = -1;
                    }
                    else {
                        obj.StartMoving(target.AutoPlay);
                    }
                }
                else {
                    prog = speedModule.easeMode.Evaluate(prog);
                    var pose = Pose.LerpUnclamped(obj.startPose, target, prog);
                    obj.MoveToPose(pose);
                }
                return true;
            }
            protected bool UpdateAcceleration(ref MovingObject obj) {
                if (obj.targetIndex < 0) {
                    if (obj.updated) {
                        obj.Stop();
                    }
                    return false;
                }
                return true;
            }
            
            protected struct MovingObject
            {
                public Rigidbody2D body;
                public int targetIndex;
                public Pose startPose;
                public float startTime;
                public bool updated;

                public void SetPose(in Pose pose) {
                    body.MovePosition(pose.position);
                    body.MoveRotation(pose.angle);
                    //startPose = pose;
                }
                public void SetPoseForced(in Pose pose) {
                    body.transform.position = (pose.position);
                    body.transform.rotation = Quaternion.AngleAxis(pose.angle, Vector3.forward);
                    //startPose = pose;
                }
                public void MoveToPose(in Pose pose) {
                    float idt = 1f / Time.fixedDeltaTime;
                    var dPos = pose.position - body.position;
                    body.velocity = dPos * idt;
                    var dRot = pose.angle - body.rotation;
                    body.angularVelocity = dRot * idt;
                    updated = true;
                }
                public void MoveImmediate(in Pose pose) {
                    SetPose(pose);
                    targetIndex = -1;
                    updated = false;
                }

                public void StartMoving(int target) {
                    startPose = new Pose(body.position, body.rotation);
                    targetIndex = target;
                    startTime = Time.fixedTime;
                }

                public void Stop() {
                    targetIndex = -1;
                    body.velocity = Vector2.zero;
                    body.angularVelocity = 0;
                }
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("go to index", 0, typeof(float)));
                slots.Add(new LogicSlot("go to index immediate", 1, typeof(float)));
                slots.Add(new LogicSlot("stop", 2));
                slots.Add(new LogicSlot("select (-1 to select all)", 3, typeof(float)));
                slots.Add(new LogicSlot("get pose count", 10, null, typeof(float)));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("bodies", 0, null, typeof(Rigidbody2D)));
            }
#endif
        }
    }
}