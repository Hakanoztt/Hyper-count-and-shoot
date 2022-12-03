using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class TimerComponent : ComponentDefinition<TimerComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent {
            
            [SerializeField]
            private float[] _times = new float[] {1.5f};
            public bool startOnAwake = false;
            [SerializeField]
            [HideInInspector]
            private LogicConnections _connections;
            private LevelPlayer _player;
            private Dictionary<int, BaseComponent> _components;
            private RoutineManager.Routine _routine;
            private int _index;
            public override void Start(in InitArgs initData)
            {
                _player = initData.player;
                _components = initData.components;
                if (startOnAwake) {
                    StartTimer();
                }
            }
            public override LogicConnections Connections { get => _connections; set => _connections = value; }
            public override object HandleInput(ILogicComponent sender, int index, object input)
            {
                switch(index) {
                    case 0:
                        StartTimer();
                        break;
                    case 1:
                        _routine.Stop();
                        break;
                    case 2:
                        if (_times.Length == 1) {
                            _times[0] = (float) input;
                        }
                        else {
                            _times = new[] {(float) input};
                        }
                        break;
                }
                return null;
            }

            private void StartTimer() {
                _routine.Stop();
                _index = 0;
                _routine = _player.FixedRoutineManager.DoRoutine(UpdateTime);
            }

            private void UpdateTime(float time, object _) {
                if(_times[_index] <= time) {
                    int i = _index;
                    _index++;
                    if(_index == _times.Length) {
                        _routine.Stop();
                    }
                    Connections.InvokeSimple(this, i, null, _components);
                }
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("start", 0));
                slots.Add(new LogicSlot("stop", 1));
                slots.Add(new LogicSlot("set time", 2, typeof(float), null));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                for(int i = 0; i < _times.Length; i++) {
                    var time = _times[i];
                    slots.Add(new LogicSlot(time.ToString(), i));
                }
            }

#endif
        }
    }
}