using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mobge.Core;
using Mobge;
using System;
namespace Mobge.Core.Components
{
    public class Movement3DComponent : ComponentDefinition<Movement3DComponent.Data>
    {
        [System.Serializable]
        public class Data : BaseComponent
        {
            public Movement3D movement;
            public bool autoStart = true;
            public OffsetMode offsetMode;
            [HideInInspector] public List<OffsetInfo> offsets;
            public override LogicConnections Connections { get => _connections; set => _connections = value; }
            public enum Mode { Normal, Loop, PingPongLoop, ContinueFromEnd, ResetOnFinish }
            public Mode mode;

            public enum MovementDirection { Forward = 1, Backward = -1 }
            public MovementDirection movementDirection = MovementDirection.Forward;

            private UpdateFunction updateFunction;
            private RangeFunction rangeFunction;

            private Dictionary<int, BaseComponent> _components;
            [HideInInspector, SerializeField] private LogicConnections _connections;
            private ActionManager.Routine _updater;
            private ExposedList<MovingBody3D> _movingBodies;
            private ActionManager _fixedActionManager;

            private int _selection = -1;
            public override void Start(in InitArgs initData)
            {
                _components = initData.components;
                _fixedActionManager = initData.player.FixedActionManager;
                _fixedActionManager.DoTimedAction(0, null, InitializeBodies);

                InitUpdateRangeFunctions();
            }
            private void InitializeBodies(object obj, bool completed)
            {
                var e = _connections.Invoke(this, 0, null, _components);
                _movingBodies = new ExposedList<MovingBody3D>();
                int index = 0;
                while (e.MoveNext())
                {
                    var cur = e.Current;
                    if (cur is Rigidbody rb)
                    {
                        InitMovingBody(rb, index, ref _movingBodies);
                        index++;
                    }
                }
                if (autoStart)
                    EnsureRoutine();
            }
            public void InitMovingBody(Rigidbody rb, int index, ref ExposedList<MovingBody3D> movingBodies)
            {
                MovingBody3D movingBody;
                movingBody.rigidbody = rb;
                movingBody.direction = (int)movementDirection;
                movingBody.loopingDirection = 1;
                movingBody.positionOffset = this.position;
                switch (offsetMode)
                {
                    default:
                    case OffsetMode.None:
                        movingBody.offsetInfo = OffsetInfo.None;
                        break;
                    case OffsetMode.ManuallySet:
                        movingBody.offsetInfo = offsets[index];
                        break;
                    case OffsetMode.Ordered:
                        movingBody.offsetInfo = OffsetInfo.GetOffsetInfoByIndex(offsets[0], index);
                        break;
                }
                movingBody.time = movementDirection == MovementDirection.Forward ? movingBody.offsetInfo.time : movement.totalTime - movingBody.offsetInfo.time;
                movingBodies.Add(movingBody);
            }
            private void EnsureRoutine()
            {
                if (_updater.IsFinished())
                {
                    _updater = _fixedActionManager.DoRoutine(Update);
                }
            }
            private void Update(float t)
            {
                bool updated = false;
                var arr = _movingBodies.array;
                for (int i = 0; i < _movingBodies.Count; i++)
                {
                    if (arr[i].direction == 0) continue;
                    updateFunction(ref arr[i]);
                    GetTransformDataFromCurves(ref arr[i], out var position, out var euler);
                    arr[i].SelfUpdate(position, euler);
                    updated = true;
                }
                if (!updated)
                    _updater.Stop();
            }
            public void GetTransformDataFromCurves(ref MovingBody3D movingBody, out Vector3 position, out Vector3 euler)
            {
                var rangedTime = rangeFunction(movingBody.time);
                position = movement.positionData.Evaluate(rangedTime) + movingBody.offsetInfo.position;
                euler = movement.rotationData.Evaluate(rangedTime);
            }
            public void GetTransformDataFromCurves(float time, ref MovingBody3D movingBody, out Vector3 position, out Quaternion rotation)
            {
                var rangedTime = rangeFunction(time + movingBody.offsetInfo.time);
                position = movement.positionData.Evaluate(rangedTime) + movingBody.offsetInfo.position;
                rotation = Quaternion.Euler(movement.rotationData.Evaluate(rangedTime));
            }
            public void InitUpdateRangeFunctions()
            {
                switch (mode)
                {
                    default:
                    case Mode.Normal:
                        updateFunction = UpdateOneShot;
                        rangeFunction = NormalRange;
                        break;
                    case Mode.Loop:
                        updateFunction = UpdateLoop;
                        rangeFunction = LoopRange;
                        break;
                    case Mode.PingPongLoop:
                        updateFunction = UpdatePingPong;
                        rangeFunction = PingPongRange;
                        break;
                    case Mode.ContinueFromEnd:
                        updateFunction = UpdateContinueFromEnd;
                        rangeFunction = NormalRange;
                        break;
                    case Mode.ResetOnFinish:
                        updateFunction = UpdateResetOnFinish;
                        rangeFunction = NormalRange;
                        break;
                }
            }
            private void UpdateOneShot(ref MovingBody3D movingBody)
            {
                movingBody.time += Time.fixedDeltaTime * movingBody.direction;
                if (movingBody.time > movement.totalTime) {
                    movingBody.direction = 0;
                    OnForwardLoopFinished();
                }
            }
            private void UpdateLoop(ref MovingBody3D movingBody)
            {
                movingBody.time += Time.fixedDeltaTime * movingBody.loopingDirection * movingBody.direction;

                if (movingBody.time > movement.totalTime)
                {
                    movingBody.time -= movement.totalTime;
                    OnForwardLoopFinished();
                }
                else if (movingBody.time < 0)
                {
                    movingBody.time += movement.totalTime;
                    OnBackwardLoopFinished();
                }
            }
            private void UpdatePingPong(ref MovingBody3D movingBody)
            {
                movingBody.time += Time.fixedDeltaTime * movingBody.loopingDirection * movingBody.direction;

                bool maxOverflow = movingBody.time > movement.totalTime;
                bool minOverflow = movingBody.time < 0;
                bool changeDir = minOverflow || maxOverflow;

                if (minOverflow)
                    movingBody.time = 0;
                else if (maxOverflow)
                {
                    movingBody.time = movement.totalTime;
                }
                if (changeDir)
                {
                    movingBody.loopingDirection *= -1;
                    if (movingBody.loopingDirection == -1)
                        OnForwardLoopFinished();
                    else
                        OnBackwardLoopFinished();
                }
            }
            private void UpdateContinueFromEnd(ref MovingBody3D movingBody)
            {
                movingBody.time += Time.fixedDeltaTime * movingBody.direction;
                if (movingBody.time > movement.totalTime)
                {
                    movingBody.time = 0;
                    GetTransformDataFromCurves(ref movingBody, out var pos, out var rot);
                    movingBody.positionOffset = movingBody.rigidbody.position - pos;
                }
                else if (movingBody.time < 0)
                {
                    movingBody.time = movement.totalTime;
                    GetTransformDataFromCurves(ref movingBody, out var pos, out var rot);
                    movingBody.positionOffset = movingBody.rigidbody.position - pos;
                }
            }
            private void UpdateResetOnFinish(ref MovingBody3D movingBody)
            {
                movingBody.time += Time.fixedDeltaTime * movingBody.direction;
                if (movingBody.time > movement.totalTime)
                {
                    movingBody.direction = 0;
                    movingBody.time = movingBody.offsetInfo.time;
                    GetTransformDataFromCurves(ref movingBody, out var pos, out var rot);
                    movingBody.rigidbody.position = pos + movingBody.positionOffset;
                    movingBody.rigidbody.rotation = Quaternion.Euler(rot);
                }
            }
            private float NormalRange(float time)
            {
                return time;
            }
            private float LoopRange(float time)
            {
                if (time > movement.totalTime)
                    time = time - movement.totalTime;
                else if (time < 0)
                    time = movement.totalTime + time;
                return time;
            }
            private float PingPongRange(float time)
            {
                if (time > movement.totalTime)
                    time = movement.totalTime * 2 - time;
                else if (time < 0)
                    time = -time;
                return time;
            }
            private void OnForwardLoopFinished()
            {
                Connections.InvokeSimple(this, 1, null, _components);
            }
            private void OnBackwardLoopFinished()
            {
                Connections.InvokeSimple(this, 2, null, _components);
            }
            public struct MovingBody3D
            {
                public Rigidbody rigidbody;
                public float time;
                public int direction; // -1 Backward, 0 Stopped, 1 Forward
                public int loopingDirection;
                public OffsetInfo offsetInfo;
                public Vector3 positionOffset;
                public void SelfUpdate(Vector3 position, Vector3 euler)
                {
                    rigidbody.MovePosition(position+ positionOffset);
                    rigidbody.MoveRotation(Quaternion.Euler(euler));
                }
               
            }
            [System.Serializable]
            public struct OffsetInfo
            {
                public float time;
                public Vector3 position;
                public Quaternion rotation;
                public static OffsetInfo None = new OffsetInfo() { rotation = Quaternion.identity };
                public static OffsetInfo GetOffsetInfoByIndex(OffsetInfo offsetInfo, int index)
                {
                    var i = index++;
                    return new OffsetInfo()
                    {
                        time = offsetInfo.time * i,
                        position = offsetInfo.position * i,
                        rotation = offsetInfo.rotation
                    };
                }
            }
            public enum OffsetMode
            {
                None = 0,
                Ordered,
                ManuallySet,
            }
            public override object HandleInput(ILogicComponent sender, int index, object input)
            {
                MovingBody3D[] arr;
                switch (index)
                {
                    case 0:
                        EnsureRoutine();
                        arr = _movingBodies.array;
                        if (_selection == -1)
                        {
                            movementDirection = MovementDirection.Forward;
                            for (int i = 0; i < arr.Length; i++)
                            {
                                arr[i].direction = 1; // MovementDirection.FORWARD
                            }
                        }
                        else if (_selection < arr.Length)
                        {
                            arr[_selection].direction = 1;
                        }
                        break;
                    case 1:
                        EnsureRoutine();
                        arr = _movingBodies.array;
                        if (_selection == -1)
                        {
                            movementDirection = MovementDirection.Backward;
                            for (int i = 0; i < arr.Length; i++)
                            {
                                arr[i].direction = -1; // MovementDirection.BACKWARD
                            }
                        }
                        else if (_selection < arr.Length)
                        {
                            arr[_selection].direction = -1;
                        }
                        break;
                    case 2:
                        arr = _movingBodies.array;
                        if (_selection == -1)
                        {
                            _updater.Stop();
                            for (int i = 0; i < arr.Length; i++)
                            {
                                arr[i].time = 0;
                            }
                        }
                        else if (_selection < arr.Length)
                        {
                            arr[_selection].time = 0;
                        }
                        break;
                    case 3:
                        arr = _movingBodies.array;
                        if (_selection == -1)
                        {
                            for (int i = 0; i < arr.Length; i++)
                            {
                                arr[i].direction = 0;
                            }
                        }
                        else if (_selection < arr.Length)
                        {
                            arr[_selection].direction = 0;
                        }
                        break;
                    case 4:
                        _selection = (int)(float)input;
                        break;
                    case 5:
                        arr = _movingBodies.array;
                        float time = ((float)input) * movement.totalTime;
                        if (_selection == -1)
                        {
                            for (int i = 0; i < arr.Length; i++)
                            {
                                arr[i].time = time;
                            }
                        }
                        else if (_selection < arr.Length)
                        {
                            arr[_selection].time = time;
                        }
                        break;
                }
                return null;
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots)
            {
                slots.Add(new LogicSlot("Go Forward", 0));
                slots.Add(new LogicSlot("Go Backward", 1));
                slots.Add(new LogicSlot("Stop", 2));
                slots.Add(new LogicSlot("Pause", 3));
                slots.Add(new LogicSlot("Select", 4));
                slots.Add(new LogicSlot("Set Progress", 5));
            }
            public override void EditorOutputs(List<LogicSlot> slots)
            {
                slots.Add(new LogicSlot("Set Rigidbody", 0, null, typeof(Rigidbody)));
                slots.Add(new LogicSlot("On Forward Loop Finished", 1));
                slots.Add(new LogicSlot("On Backward Loop Finished", 2));
            }
#endif
            private delegate void UpdateFunction(ref MovingBody3D mb);
            private delegate float RangeFunction(float t);
        }
    }
}