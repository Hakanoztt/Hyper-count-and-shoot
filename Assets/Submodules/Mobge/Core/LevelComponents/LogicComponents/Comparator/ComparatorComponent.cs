using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class ComparatorComponent : ComponentDefinition<ComparatorComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent {
            [SerializeField] private float value;
            [SerializeField] private ComparisonMode comparisonMode = ComparisonMode.GraterThan;
            [SerializeField] private TriggerMode triggerMode = TriggerMode.OnEveryInput;
            [SerializeField] private bool startValue;
            private enum ComparisonMode {
                GraterThan = 0,
                GraterEqual,
                Equal,
                NotEqual,
                LessEqual,
                LessThan,
            }

            private enum TriggerMode
            {
                OnEveryInput = 0,
                OnComparisonResultChanged = 1,
                Manual = 2,
            }
            
            private float _value;
            private bool _lastValue;
            
            [SerializeField]
            [HideInInspector]
            private LogicConnections _connections;
            private Dictionary<int, BaseComponent> _components;
            public override void Start(in InitArgs initData) {
                _value = value;
                _components = initData.components;
                _lastValue = startValue;
            }
            public override LogicConnections Connections { get => _connections; set => _connections = value; }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch(index) {
                    case 0: _value = (float)input; break;
                    case 1: OnInput((float)input); break;
                    case 2: Trigger(); break;
                }
                return null;
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("compare", 1, typeof(float)));
                slots.Add(new LogicSlot("set value", 0, typeof(float)));
                slots.Add(new LogicSlot("trigger", 2, typeof(float)));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("true", 0, null, null));
                slots.Add(new LogicSlot("false", 1, null, null));
            }
#endif
            private void OnInput(float input) {
                switch (triggerMode) {
                    case TriggerMode.OnEveryInput: {
                            bool newValue = CompareInput(input);
                            _lastValue = newValue;
                            Trigger();
                        }
                        break;
                    case TriggerMode.OnComparisonResultChanged: {
                            bool newValue = CompareInput(input);
                            if (newValue != _lastValue) {
                                _lastValue = newValue;
                                Trigger();
                            }
                        }
                        break;
                    case TriggerMode.Manual:
                    default:
                        _lastValue = CompareInput(input);
                        break;
                }
            }
            void Trigger() {
                if(_lastValue) {
                    Connections.InvokeSimple(this, 0, null, _components);
                }
                else {
                    Connections.InvokeSimple(this, 1, null, _components);
                }
            }
            private bool CompareInput(float input) {
                switch (comparisonMode) {
                    case ComparisonMode.GraterThan:   return input > _value;
                    case ComparisonMode.GraterEqual:  return input >= _value;
                    case ComparisonMode.Equal:         return input == _value;
                    case ComparisonMode.NotEqual:      return input != _value;
                    case ComparisonMode.LessEqual:     return input <= _value;
                    case ComparisonMode.LessThan:      return input < _value;
                    default:                            return false;
                }
            }
        }
    }
}