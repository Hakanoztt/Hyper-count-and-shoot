using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components
{
    public class Vector3Component : ComponentDefinition<Vector3Component.Data>
    {
        [Serializable]
        public class Data : BaseComponent
        {
            public bool triggerOnAwake;
            public bool autoTrigger;

            private LevelPlayer _player;
            private Dictionary<int, BaseComponent> _components;
            [SerializeField] private Vector3 _value;

            [SerializeField, HideInInspector] private LogicConnections _connections;
            public override LogicConnections Connections { get => _connections; set => _connections = value; }
            public override void Start(in InitArgs initData) {

                _player = initData.player;
                _components = initData.components; 
                if (triggerOnAwake) {
                    OnValueChanged();
                }
            }
            public Vector3 Value {
                get => _value;
                set {
                    _value = value;
                    if (autoTrigger) {
                        OnValueChanged();
                    }
                }
            }
            public void OnValueChanged() {
                Connections.InvokeSimple(this, 0, Value, _components);
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    default:
                    case 5:
                        return Value;
                    case 10:
                        Value = (Vector3)input;
                        break;
                    case 11:
                        Value += (Vector3)input;
                        break;
                    case 12:
                        Value -= (Vector3)input;
                        break;
                    case 13:
                        OnValueChanged();
                        break;
                    
                }
                return null;
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("get value", 5, null, typeof(Vector3)));
                slots.Add(new LogicSlot("set value", 10, typeof(Vector3)));
                slots.Add(new LogicSlot("add to value", 11, typeof(Vector3)));
                slots.Add(new LogicSlot("substract from value", 12, typeof(Vector3)));
                slots.Add(new LogicSlot("trigger", 13));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("on value change", 0, null, typeof(float)));
            }
#endif
        }
    }
}