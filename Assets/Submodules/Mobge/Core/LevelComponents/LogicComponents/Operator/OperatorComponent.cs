using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components
{
    public class OperatorComponent : ComponentDefinition<OperatorComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent {
            [SerializeField]
            private Mode _mode;
            private enum Mode {
                Add,
                Subtract,
                Multiply,
                Divide,
            }
            [SerializeField]
            private TriggerMode _triggerMode;
            public TriggerMode TriggerRule => _triggerMode;
            public enum TriggerMode {
                ForEveryInput,
                ForEveryPair,
                OnlyManually,
            }
            [SerializeField] private float _input1Value;
            public float Input1Value {
                get => _input1Value;
                set {
                    _input1Value = value;
                    TriggerLogic();
                } 
            }
            [SerializeField] private float _input2Value;
            public float Input2Value {
                get => _input2Value;
                set {
                    _input2Value = value;
                    TriggerLogic();
                }
            }
            [SerializeField]
            [HideInInspector]
            private LogicConnections _connections;
            private Dictionary<int, BaseComponent> _components;
            public override void Start(in InitArgs initData)
            {
                _components = initData.components;
            }
            public override LogicConnections Connections { get => _connections; set => _connections = value; }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case 0: Input1Value = (float)input; break;
                    case 1: Input2Value = (float)input; break;
                    case 2: Trigger();                break;
                }
                return null;
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("set input 1", 0, typeof(float)));
                slots.Add(new LogicSlot("set input 2", 1, typeof(float)));
                slots.Add(new LogicSlot("trigger calculation", 2));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("on calculated", 0, typeof(float)));
            }
#endif
            public void TriggerLogic() {
                switch (_triggerMode) {
                    case TriggerMode.ForEveryInput:
                        Trigger();
                        break;
                    //case TriggerMode.ForEveryPair:
                    //    if (_input1Received && _input2Received) {
                    //        _input1Received = false;
                    //        _input2Received = false;
                    //        Trigger();
                    //    }
                    //    break;
                    case TriggerMode.OnlyManually:
                        break;
                }
            }
            public void Trigger() {
                float output = 0f;
                switch (_mode) {
                    case Mode.Add: output = _input1Value + _input2Value; break;
                    case Mode.Subtract: output = _input1Value - _input2Value; break;
                    case Mode.Multiply: output = _input1Value * _input2Value; break;
                    case Mode.Divide:
                        //if (_input2Value == 0f) {
                        //    Debug.LogError("Trying to divide by zero, ERROR");
                        //    return;
                        //}
                        output = _input1Value / _input2Value; break;
                }
                Connections.InvokeSimple(this, 0, output, _components);
            }
        }
    }
}