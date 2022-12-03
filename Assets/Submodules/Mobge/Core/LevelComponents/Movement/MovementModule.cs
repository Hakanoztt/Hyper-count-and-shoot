using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public interface IMovementModule {
        /// <summary>
        /// Add body to the module.
        /// </summary>
        /// <param name="body"></param>
        /// <returns>Id of the added body.</returns>
        int AddBody(Rigidbody2D body, float normalizedTime = 0);
        void SetSpeed(int index, float speed);
        void RemoveBody(int id);
        void SetBody(int id, Rigidbody2D body);
        float GetNormalizedTime(int id);
    }
    public class MovementModule : BaseMovementModule<MovementModule.Data> {
        [Serializable]
        public class Data : BaseData, IMovementModule {
            [SerializeField]
            private int _mode;
            public Mode Mode {
                get => (Mode)_mode;
                set => _mode = (int)value;
            }

            public bool autoStart;
            public float motorForce;
            public bool startFromInitialPosition = true;
            private ExposedList<MovingObject> _objects;
            private ActionManager.Routine _routine;
            private int _selectedIndex = -1;
            private LevelPlayer _player;
            private Dictionary<int, BaseComponent> _components;
            private int _nextAddIndex = 0;

            public override void Start(in InitArgs initData) {
                base.Start(initData);
                _player = initData.player;
                _components = initData.components;
                EnsureData();
                _objects = new ExposedList<MovingObject>();
                initData.player.FixedActionManager.DoTimedAction(0, null, InitializeBodies, null);
                _selectedIndex = -1;
            }


            private void InitializeBodies(object data, bool completed) {
                if (completed) {
                    var e = _connections.Invoke(this, 0, null, _components);
                    while (e.MoveNext()) {
                        var rb = e.Current as Rigidbody2D;
                        AddBody(rb);
                    }
                    if (autoStart) {
                        EnsureRoutine();
                        var arr = _objects.array;
                        for (int i = 0; i < _objects.Count; i++) {
                            arr[i].velocity = 1;
                        }
                    }
                }
            }

            public int AddBody(Rigidbody2D body, float normalizedTime = 0) {
                EnsureRoutine();
                MovingObject mo;
                mo.body = body;
                mo.time = normalizedTime * poseCurve.duration;
                //Debug.Log(normalizedTime);
                mo.updated = false;
                mo.velocity = 0;
                mo.offset = Vector2.zero;

                int id = _nextAddIndex;
                if (startFromInitialPosition) {
                    UpdatePose(mo);
                    //mo.body.position = EvaluatePosition(mo.time, mo.offset);
                }
                if (_nextAddIndex == _objects.Count) {
                    _objects.Add(mo);
                    _nextAddIndex++;
                }
                else {
                    _objects.array[_nextAddIndex] = mo;
                    do {
                        _nextAddIndex++;
                    }
                    while (_nextAddIndex < _objects.Count && _objects.array[_nextAddIndex].body != null);
                }
                return id;
            }

            public void SetSpeed(int index, float speed) {
                _objects.array[index].velocity = speed;
            }

            public void RemoveBody(int id) {
                _objects.array[id].body = null;
                _objects.array[id].velocity = 0;
            }

            public void SetBody(int id, Rigidbody2D body) {
               
                _objects.array[id].body = body;
                UpdatePose(_objects.array[id]);
            }
            public float GetNormalizedTime(int id) {
                return _objects.array[id].time / this.poseCurve.duration;
            }

            private void EnsureRoutine() {
                var fam = _player.FixedActionManager;
                if (_routine.IsFinished()) {
                    _routine = fam.DoRoutine(Update);
                }
            }
            private void Update(float time) {
                var arr = _objects.array;
                bool updated = false;
                for (int i = 0; i < _objects.Count; i++) {
                    var b = UpdateObject(ref arr[i], i);
                    updated = updated || b;

                }
                if (!updated) {
                    _routine.Stop();
                }
            }
            private bool UpdateObject(ref MovingObject obj, int index) {
                if (obj.velocity == 0) {
                    if (obj.updated) {
                        obj.updated = false;
                        obj.body.velocity = Vector2.zero;
                        if (poseCurve.hasAngle) {
                            obj.body.angularVelocity = 0;
                        }
                    }
                    return false;
                }
                obj.updated = true;
                float valueTime = obj.time + obj.velocity * Time.fixedDeltaTime;
                if (obj.velocity < 0) {
                    if (obj.time < 0) {
                        switch (Mode) {
                            case Mode.Normal:
                                valueTime = 0;
                                obj.velocity = 0;
                                _connections.InvokeSimple(this, 2, (float)index, _components);
                                break;
                            case Mode.Loop:
                                valueTime += poseCurve.duration;
                                break;
                            case Mode.ContinueFromEnd:
                                valueTime += poseCurve.duration;
                                obj.offset += poseCurve.EvaluatePosition(0) - poseCurve.EvaluatePosition(poseCurve.duration);
                                break;
                            case Mode.ResetOnFinish:
                                valueTime = 1;
                                obj.velocity = 0;
                                _connections.InvokeSimple(this, 2, (float)index, _components);
                                break;
                        }
                    }
                }
                else {
                    if (obj.time > poseCurve.duration) {
                        switch (Mode) {
                            case Mode.Normal:
                                valueTime = poseCurve.duration;
                                obj.velocity = 0;
                                _connections.InvokeSimple(this, 1, (float)index, _components);
                                break;
                            case Mode.Loop:
                                valueTime -= poseCurve.duration;
                                break;
                            case Mode.ContinueFromEnd:
                                valueTime -= poseCurve.duration;
                                obj.offset += poseCurve.EvaluatePosition(poseCurve.duration) - poseCurve.EvaluatePosition(0);
                                break;
                            case Mode.ResetOnFinish:
                                valueTime = 0;
                                obj.velocity = 0;
                                _connections.InvokeSimple(this, 1, (float)index, _components);
                                break;
                        }
                    }
                }
                obj.time = valueTime;
                float idt = 1f / Time.fixedDeltaTime;
                var realPos = EvaluatePosition(valueTime, obj.offset);
                var dif = realPos - obj.body.position;
                var vel = dif * idt;
                obj.body.velocity = vel;
                if (poseCurve.hasAngle) {
                    var angle = EvaluateAngle(valueTime);
                    var adif = Mathf.DeltaAngle(obj.body.rotation, angle);
                    var avel = adif * idt;
                    obj.body.angularVelocity = avel;
                }
                return true;
            }

            public override object HandleInput(ILogicComponent sender, int index, object input) {
                MovingObject[] arr;
                switch (index) {
                    case 0:
                    default:
                        EnsureRoutine();
                        arr = _objects.array;
                        if (_selectedIndex == -1) {
                            for (int i = 0; i < _objects.Count; i++) {
                                arr[i].velocity = 1;
                            }
                        }
                        else {
                            if (arr.Length > _selectedIndex) {
                                arr[_selectedIndex].velocity = 1;
                            }
                        }
                        break;
                    case 1:
                        EnsureRoutine();
                        arr = _objects.array;
                        if (_selectedIndex == -1) {
                            for (int i = 0; i < _objects.Count; i++) {
                                arr[i].velocity = -1;
                            }
                        }
                        else {
                            if (arr.Length > _selectedIndex) {
                                arr[_selectedIndex].velocity = -1;
                            }
                        }
                        break;
                    case 2:
                        arr = _objects.array;
                        if (_selectedIndex == -1) {
                            for (int i = 0; i < _objects.Count; i++) {
                                arr[i].velocity = 0;
                            }
                        }
                        else {
                            if (_objects.Count > _selectedIndex) {
                                arr[_selectedIndex].velocity = 0;
                            }
                        }
                        break;
                    case 3:
                        _selectedIndex = (int)(float)input;
                        break;
                    case 4:
                        EnsureRoutine();
                        arr = _objects.array;
                        if (_selectedIndex == -1) {
                            for (int i = 0; i < _objects.Count; i++) {
                                arr[i].time = (float)input;
                            }
                        }
                        else {
                            if (_objects.Count > _selectedIndex) {
                                arr[_selectedIndex].time = (float)input;
                            }
                        }
                        break;
                    case 1024:
                        return this;
                }
                return null;
            }

#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("go forward", 0));
                slots.Add(new LogicSlot("go backward", 1));
                slots.Add(new LogicSlot("stop", 2));
                slots.Add(new LogicSlot("select (-1 to select all)", 3, typeof(float)));
                slots.Add(new LogicSlot("set progress", 4, typeof(float)));
                slots.Add(new LogicSlot("this", 1024, null, GetType(), true));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("bodies", 0, null, typeof(Rigidbody2D)));
                slots.Add(new LogicSlot("on finish forward", 1, typeof(float), null));
                slots.Add(new LogicSlot("on finish backward", 2, typeof(float), null));
            }
#endif

        }

    }
}