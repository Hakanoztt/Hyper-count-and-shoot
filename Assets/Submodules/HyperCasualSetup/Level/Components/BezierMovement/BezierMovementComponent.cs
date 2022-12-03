using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mobge;
using Mobge.Animation;
using Mobge.Core;
using Mobge.Core.Components;
using UnityEngine;

namespace Mobge.HyperCasualSetup
{
    public class BezierMovementComponent : ComponentDefinition<BezierMovementComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent, IMovementModule, IRotationOwner
        {
            [HideInInspector] public float maxSpeed = 3;
            [HideInInspector] public float damping = float.PositiveInfinity;
            [HideInInspector] public bool autoStart = false;
            [HideInInspector] public Mode mode = Mode.Normal;
            [HideInInspector] public bool enableDebugButtons = false;
            [HideInInspector] public bool applyRotation = false;
            [HideInInspector] public float forwardAngleOffset = 270;
            [HideInInspector] public float backwardAngleOffset = 90;
            
#if UNITY_EDITOR
            public static BezierPath3D DefaultBezierPath {
                get {
                    var bezier = new BezierPath3D {
                        closed = false,
                        controlMode = BezierPath3D.ControlMode.Automatic
                    };
                    var points = new[] {
                        new BezierPath3D.Point {
                            position = new Vector3(-2, 2),
                        },
                        new BezierPath3D.Point {
                            position = new Vector3(-2, -2),
                        },
                        new BezierPath3D.Point {
                            position = new Vector3(2, -2),
                        },
                        new BezierPath3D.Point {
                            position = new Vector3(2, 2),
                        },
                    };
                    bezier.Points.SetArray(points, points.Length);
                    bezier.UpdateControlsForAuto();
                    return bezier;
                }
            }
            public BezierPath3D path = DefaultBezierPath;
#else
            public BezierPath3D path;
#endif
            
            [SerializeField] [HideInInspector] private LogicConnections _connections;
            private LevelPlayer _player;
            private Dictionary<int, BaseComponent> _components;
            private ExposedList<ConnectedBody> _connectedBodies;
            private RoutineManager.Routine _routine;
            private BezierPath3D _path;
            private int _selectedIndex = -1;
            
            [HideInInspector, SerializeField] private Quaternion _rotation = Quaternion.identity;
            public Quaternion Rotation { get => _rotation; set => _rotation = value; }

            public override LogicConnections Connections {
                 get => _connections;
                 set => _connections = value;
            }

            public override void Start(in InitArgs initData)
            {
                _player = initData.player;
                _components = initData.components;
                
                _path = path.Clone();
                LocalToWorld();

                if (enableDebugButtons)
                {
                    new GameObject("BezierMovement Temp Buttons").AddComponent<Temp>().SeedData(this);
                }

                _player.FixedRoutineManager.DoAction(DelayedStart, 0);
            }

            private void LocalToWorld()
            {
                var pointList = _path.Points;
                for (int i = 0; i < pointList.Count; i++)
                {
                    var point = pointList.array[i];
                    point.position = _rotation * point.position;
                    point.position += position;
                    point.leftControl = _rotation * point.leftControl;
                    point.leftControl += position;
                    point.rightControl = _rotation * point.rightControl;
                    point.rightControl += position;
                    pointList.array[i] = point;
                }
            }

            private void DelayedStart(bool completed, object data)
            {
                int connectionCount = _connections.GetConnectionCount(0);
                if (connectionCount <= 0)
                {
                    Debug.LogError("There is no rigidbody connected to the " + nameof(BezierMovementComponent));
                }
                else
                {
                    _connectedBodies = new ExposedList<ConnectedBody>();
                    
                    var e = _connections.Invoke(this, 0, null, _components);
                    
                    while (e.MoveNext())
                    {
                        if (e.Current is Rigidbody2D body)
                        {
                            _connectedBodies.Add(new ConnectedBody(_connectedBodies.Count, this, body, _path, maxSpeed, damping));
                        } 
                    }

                    if (autoStart)
                    {
                        Forward();
                    }
                }
            }
            
            private void Update(float deltaTime, object data)
            {
                bool playingBodyExist = false;
                for (int i = 0; i < _connectedBodies.Count; i++)
                {
                    if (_connectedBodies.array[i].IsPlaying)
                    {
                        _connectedBodies.array[i].Update();
                        playingBodyExist = true;
                    }
                }

                if (!playingBodyExist) StopTheRoutine();
            }

            private void Forward()
            {
                if (_selectedIndex == -1)
                {
                    for (int i = 0; i < _connectedBodies.Count; i++)
                    {
                        _connectedBodies.array[i].Forward();
                    }
                }
                else if(_selectedIndex < _connectedBodies.Count)
                {
                    _connectedBodies.array[_selectedIndex].Forward();
                }
            }
            
            private void Backward()
            {
                if (_selectedIndex == -1)
                {
                    for (int i = 0; i < _connectedBodies.Count; i++)
                    {
                        _connectedBodies.array[i].Backward();
                    }
                }
                else if(_selectedIndex < _connectedBodies.Count)
                {
                    _connectedBodies.array[_selectedIndex].Backward();
                }
            }

            private void OnRouteFinish(PlayState state, int index)
            {
                if (state == PlayState.Forward) _connections.InvokeSimple(this, 1, index, _components);
                else _connections.InvokeSimple(this, 2, index, _components);
            }
            
            private void Stop()
            {
                if (_selectedIndex == -1)
                {
                    for (int i = 0; i < _connectedBodies.Count; i++)
                    {
                        _connectedBodies.array[i].Stop();
                    }
                }
                else if(_selectedIndex < _connectedBodies.Count)
                {
                    _connectedBodies.array[_selectedIndex].Stop();
                }
            }

            private void StopTheRoutine()
            {
                if (!_routine.IsFinished)
                    _routine.Stop();
            }

            private void Resume()
            {
                if (_routine.IsFinished)
                    _routine = _player.FixedRoutineManager.DoRoutine(Update);
            }
            
            private void Reset()
            {
                if (_selectedIndex == -1)
                {
                    for (int i = 0; i < _connectedBodies.Count; i++)
                    {
                        _connectedBodies.array[i].Reset();
                    }
                }
                else if(_selectedIndex < _connectedBodies.Count)
                {
                    _connectedBodies.array[_selectedIndex].Reset();
                }
            }
            
            private void ResetToEnd()
            {
                if (_selectedIndex == -1)
                {
                    for (int i = 0; i < _connectedBodies.Count; i++)
                    {
                        _connectedBodies.array[i].ResetToEnd();
                    }
                }
                else if(_selectedIndex < _connectedBodies.Count)
                {
                    _connectedBodies.array[_selectedIndex].ResetToEnd();
                }
            }
            
            private void SetDirection(float direction)
            {
                if (_selectedIndex == -1)
                {
                    for (int i = 0; i < _connectedBodies.Count; i++)
                    {
                        _connectedBodies.array[i].SetDirection(direction);
                    }
                }
                else if(_selectedIndex < _connectedBodies.Count)
                {
                    _connectedBodies.array[_selectedIndex].SetDirection(direction);
                }
            }
            
            private void SetDamping(float damping)
            {
                if (_selectedIndex == -1)
                {
                    for (int i = 0; i < _connectedBodies.Count; i++)
                    {
                        _connectedBodies.array[i].SetDamping(damping);
                    }
                }
                else if(_selectedIndex < _connectedBodies.Count)
                {
                    _connectedBodies.array[_selectedIndex].SetDamping(damping);
                }
            }

            public override object HandleInput(ILogicComponent sender, int index, object input)
            {
                switch (index)
                {
                    case 0:
                        _selectedIndex = (int)(float)input;
                        break;
                    case 1:
                        Forward();
                        break;
                    case 2:
                        Backward();
                        break;
                    case 3:
                        Stop();
                        break;
                    case 4:
                        Reset();
                        break;
                    case 5:
                        ResetToEnd();
                        break;
                    case 6:
                        if(input is float direction)
                            SetDirection(direction);
                        break;
                    case 7:
                        if(input is float damping) 
                            if(damping >= 0)
                                SetDamping(damping);
                            else
                                Debug.LogError("Damping should be positive number!");
                        break;
                    case 1024:
                        return this;
                }
                return base.HandleInput(sender, index, input);
            }

            class Temp : MonoBehaviour
            {
                private Data _data;
                private string _indexString;
                private string _dampingString;
                private string _directionString;

                public void SeedData(Data data)
                {
                    _data = data;
                }
                
                private void OnGUI()
                {
                    GUIStyle myButtonStyle = new GUIStyle(GUI.skin.button);
                    myButtonStyle.fontSize = 50;

                    GUIStyle myTextFieldStyle = new GUIStyle(GUI.skin.textField);
                    myTextFieldStyle.fontSize = 50;

                    float.TryParse(_indexString = GUI.TextField(new Rect(10, 10, 270, 90), _indexString, myTextFieldStyle), out float index);
                    if (GUI.Button(new Rect(290, 10, 320, 90), "Select Index", myButtonStyle))
                        _data.HandleInput(null, 0, index);

                    if (GUI.Button(new Rect(10, 120, 270, 90), "Forward", myButtonStyle))
                        _data.HandleInput(null, 1, null);
                    
                    if (GUI.Button(new Rect(10, 230, 270, 90), "Backward", myButtonStyle))
                        _data.HandleInput(null, 2, null);
                    
                    if (GUI.Button(new Rect(10, 340, 270, 90), "Stop", myButtonStyle))
                        _data.HandleInput(null, 3, null);
                    
                    if (GUI.Button(new Rect(10, 450, 270, 90), "Reset", myButtonStyle))
                        _data.HandleInput(null, 4, null);
                    
                    if (GUI.Button(new Rect(10, 560, 320, 90), "Reset To End", myButtonStyle))
                        _data.HandleInput(null, 5, null);
                    
                    float.TryParse(_directionString = GUI.TextField(new Rect(10, 670, 270, 90), _directionString, myTextFieldStyle), out float direction);
                    if (GUI.Button(new Rect(290, 670, 320, 90), "Set Direction", myButtonStyle))
                        _data.HandleInput(null, 6, direction);
                    
                    float.TryParse(_dampingString = GUI.TextField(new Rect(10, 780, 270, 90), _dampingString, myTextFieldStyle), out float damping);
                    if (GUI.Button(new Rect(290, 780, 320, 90), "Set Damping", myButtonStyle))
                        _data.HandleInput(null, 7, damping);
                }
            }

#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots)
            {
                slots.Add(new LogicSlot("select (-1 to select all)", 0, typeof(float)));
                slots.Add(new LogicSlot("go forward", 1));
                slots.Add(new LogicSlot("go backward", 2));
                slots.Add(new LogicSlot("stop", 3));
                slots.Add(new LogicSlot("reset", 4));
                slots.Add(new LogicSlot("reset to end", 5));
                slots.Add(new LogicSlot("set direction", 6, typeof(float)));
                slots.Add(new LogicSlot("set damping", 7, typeof(float)));
                slots.Add(new LogicSlot("this", 1024, null, GetType(), true));
            }

            public override void EditorOutputs(List<LogicSlot> slots)
            {
                slots.Add(new LogicSlot("bodies", 0, null, typeof(Rigidbody2D)));
                slots.Add(new LogicSlot("on finish forward", 1, typeof(int)));
                slots.Add(new LogicSlot("on finish backwards", 2, typeof(int)));
            }
#endif
            private struct ConnectedBody
            {
                private readonly int _bodyIndex;
                private readonly Data _data;
                private Rigidbody2D _rb;
                private readonly BezierPath3D _path;
                private BezierPath3D.SegmentEnumerator _pathEnumerator;
                private IntendedDirection _lastIntendedDirection;
                private PlayState _state;
                private Vector3 _offset;
                private float _maxSpeed;
                private float _targetSpeed;
                private float _speed;
                private float _direction;
                private float _damping;
                private bool _pause;

                public Rigidbody2D Body => _rb;
                private float Direction
                {
                    get => _direction;
                    set
                    {
                        _direction = value;
                        _targetSpeed = _maxSpeed * _direction;
                        _lastIntendedDirection = _direction >= 0 ? IntendedDirection.Forward : IntendedDirection.Backward;
                    }
                }
                
                private PlayState State
                {
                    get => _state;
                    set
                    {
                        _state = value;
                        if (_data.mode == Mode.Normal && _path.closed)
                        {
                            if (_state == PlayState.Forward)
                            {
                                var segmentInfo = _pathEnumerator.Current; 
                                if(segmentInfo.index >= _path.Points.Count) _pathEnumerator.Reset();
                            }
                            else if(_state == PlayState.Backward)
                            {
                                var segmentInfo = _pathEnumerator.Current;
                                if(segmentInfo.index == 0 && segmentInfo.progress == 0) _pathEnumerator.RollForth();
                            }
                        }
                    }
                }

                public bool IsPlaying
                {
                    get => !_pause;
                }

                public ConnectedBody(int bodyIndex, Data data, Rigidbody2D rb, BezierPath3D path, float speed, float damping)
                {
                    _bodyIndex = bodyIndex;
                    _data = data;
                    _rb = rb;
                    _path = path;
                    
                    _pathEnumerator = _path.GetEnumerator(0);
                    _pathEnumerator.MoveNext(0);
                    _rb.transform.position = _pathEnumerator.CurrentPoint;
                    
                    _lastIntendedDirection = IntendedDirection.None;
                    _state = PlayState.Stopped;
                    _offset = Vector2.zero;
                    _maxSpeed = speed;
                    _targetSpeed = speed;
                    _speed = 0;
                    _direction = 1;
                    _damping = damping;
                    _pause = true;
                }

                public void Update()
                {
                    bool moveNext;

                    if (_speed > 0) moveNext = _pathEnumerator.MoveForward(_speed * Time.deltaTime);
                    else if (_speed < 0) moveNext = _pathEnumerator.MoveBackward(Mathf.Abs(_speed * Time.deltaTime));
                    else //Calling forward or backward is a crucial part. That's why we are making sure to call intended direction when speed is zero.
                    {
                        if(_lastIntendedDirection == IntendedDirection.Forward) moveNext = _pathEnumerator.MoveForward(0);
                        else moveNext = _pathEnumerator.MoveBackward(0);
                    }
                    
                    if (moveNext)
                    {
                        var targetPoint = _pathEnumerator.CurrentPoint + _offset;
                        float idt = 1f / Time.fixedDeltaTime;
                        var dif = (Vector2)targetPoint - _rb.position;
                        var vel = dif * idt;
                        _rb.velocity = vel;

                        if (_data.applyRotation && _speed != 0)
                        {
                            var axisAngleOffset = _speed < 0 ? _data.backwardAngleOffset : _data.forwardAngleOffset;
                            float angle = Mathf.Atan2(dif.y, dif.x) * Mathf.Rad2Deg + axisAngleOffset;
                            var adif = Mathf.DeltaAngle(_rb.rotation, angle);
                            var avel = adif * idt;
                            _rb.angularVelocity = avel;
                        }
                    }
                    else
                    {
                        _data.OnRouteFinish(_state, _bodyIndex);
                        var changingDirection = (int) State == (int)_lastIntendedDirection * -1;
                    
                        switch (_data.mode)
                        {
                            case Mode.Normal:
                                _speed = 0;
                                
                                if (!changingDirection)
                                {
                                    Stop();
                                }
                                break;
                            
                            case Mode.Loop:
                                if (_path.closed)
                                {
                                    if (_speed > 0)
                                    {
                                        _pathEnumerator.Reset();
                                    }
                                    else if(_speed < 0)
                                    {
                                        _pathEnumerator.RollForth();
                                    }
                                }
                                else
                                {
                                    _speed = 0;
                                    if (State == PlayState.Forward || _state == PlayState.Backward)
                                    {
                                        SetDirection(_direction * -1);
                                    }
                                }
                                break;
                            
                            case Mode.ContinueFromEnd:
                                if (State == PlayState.Forward)
                                {
                                    _pathEnumerator.Reset();
                                    _offset += _path.Points.array[_path.Points.Count - 1].position - _path.Points.array[0].position;
                                }
                                else
                                {
                                    _pathEnumerator.RollForth();
                                    _offset -= _path.Points.array[_path.Points.Count - 1].position - _path.Points.array[0].position;
                                }
                                break;
                            
                            case Mode.ResetOnFinish:
                                if (!changingDirection) _speed = 0;
                                if (State == PlayState.Forward)
                                    _pathEnumerator.Reset();
                                else
                                    _pathEnumerator.RollForth();
                                break;
                        }
                    }
                
                    if (_targetSpeed != _speed)
                    {
                        int direction = _targetSpeed - _speed > 0 ? 1 : -1;
                        _speed += _damping * Time.deltaTime * direction;
                        if (_speed * direction > _targetSpeed * direction) _speed = _targetSpeed;
                        
                        if(State != PlayState.Stopping)
                        {
                            GetState();
                        }
                    }
                    
                    if (State == PlayState.Stopping)
                    {
                        if (_speed == 0)
                        {
                            State = PlayState.Stopped;
                            Pause();
                        }
                    }
                }

                public void Forward(float value = 1)
                {
                    if (_data.mode == Mode.ResetOnFinish)
                    {
                        _speed = 0;
                        _pathEnumerator.Reset();
                    }
                    else if(_data.mode == Mode.Normal && _path.closed)
                    {
                        var segmentInfo = _pathEnumerator.Current; 
                        if(segmentInfo.index >= _path.Points.Count) _pathEnumerator.Reset();
                    }

                    Direction = value;
                    GetState();
                    
                    _pause = false;
                    _data.Resume();
                }
                
                public void Backward(float value = -1)
                {
                    if (_data.mode == Mode.ResetOnFinish)
                    {
                        _speed = 0;
                        _pathEnumerator.RollForth();
                    }
                    else if(_data.mode == Mode.Normal && _path.closed)
                    {
                        var segmentInfo = _pathEnumerator.Current; 
                        if(segmentInfo.index == 0 && segmentInfo.progress == 0) _pathEnumerator.RollForth();
                    }

                    Direction = value;
                    GetState();
                    
                    _pause = false;
                    _data.Resume();
                }

                public void SetDirection(float value)
                {
                    if(value > 0) Forward(value);
                    else if (value < 0) Backward(value);
                    else Pause();
                }
                
                public void Stop()
                {
                    State = PlayState.Stopping;
                    _targetSpeed = 0;
                }
                
                private void Pause()
                {
                    _targetSpeed = 0;
                    _speed = 0;
                    _rb.velocity = Vector2.zero;
                    _rb.angularVelocity = 0;
                    _lastIntendedDirection = IntendedDirection.None;
                    _pause = true;
                }
                
                public void Reset()
                {
                    _pathEnumerator.Reset();
                    _pathEnumerator.MoveForward(0);
                    _rb.transform.position = _pathEnumerator.CurrentPoint;
                }
                
                public void ResetToEnd()
                {
                    _pathEnumerator.RollForth();
                    _rb.transform.position = _pathEnumerator.CurrentPoint;
                }
                
                private void GetState()
                {
                    if (_speed != 0)
                    {
                        State = (PlayState)Mathf.RoundToInt(_speed / Mathf.Abs(_speed));
                    }
                    else
                    {
                        State = PlayState.Stopped;
                    }
                }

                public void SetMaxSpeed(float maxSpeed)
                {
                    _maxSpeed = maxSpeed;
                    if (_state != PlayState.Stopped || _state != PlayState.Stopping) _targetSpeed = maxSpeed * _direction;
                }
                
                public void SetDamping(float damping)
                {
                    _damping = damping;
                }

                public void SetBody(Rigidbody2D rb)
                {
                    _rb = rb;
                    var segmentInfo = _pathEnumerator.Current; 
                    
                    //making sure tne enumerator's index isn't out of boundaries 
                    if((_path.closed && segmentInfo.index >= _path.Points.Count - 1) || (!_path.closed && segmentInfo.index >= _path.Points.Count))
                        _pathEnumerator.RollForth();
                    
                    _rb.transform.position = _pathEnumerator.CurrentPoint;
                }

                public float GetNormalizedTime()
                {
                    var bezierCount = _path.closed ? _path.Points.Count : _path.Points.Count;
                    var normSegmentMul = 1f/bezierCount;
                    
                    var segmentInfo = _pathEnumerator.Current;
                    return normSegmentMul * (segmentInfo.index + segmentInfo.progress);
                }
            }
            
            public int AddBody(Rigidbody2D body, float normalizedTime = 0)
            {
                var index = _connectedBodies.Count;
                _connectedBodies.Add(new ConnectedBody(index, this, body, _path, maxSpeed, damping));
                return index;
            }

            public void SetSpeed(int index, float speed)
            {
                _connectedBodies.array[index].SetMaxSpeed(speed);
            }

            public void RemoveBody(int id)
            {
                _connectedBodies.array[id].Body.velocity = Vector2.zero;
                _connectedBodies.RemoveAt(id);
            }

            public void SetBody(int id, Rigidbody2D body)
            {
                _connectedBodies.array[id].SetBody(body);
            }

            public float GetNormalizedTime(int id)
            {
                return _connectedBodies.array[id].GetNormalizedTime();
            }
            
            public enum Mode {
                Normal = 0,
                Loop = 1,
                ContinueFromEnd = 2,
                ResetOnFinish = 3,
            }
            
            private enum PlayState
            {
                Forward = 1,
                Backward = -1,
                Stopping = 2,
                Stopped = 0,
            }
            
            private enum IntendedDirection
            {
                Forward = 1,
                Backward = -1,
                None = 0
            }
            
            public enum AlignAxis
            {
                Right = 0,
                Bottom = 90,
                Left = 180,
                Top = 270
            }

        }
    }
}