using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class NumberComponent : ComponentDefinition<NumberComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent {
            [SerializeField]
            public float _value = 0;
            [SerializeField] 
            public Mode triggerOnAwake = 0;


            public float Value {
                get => _value;
                set {
                    _value = value;
                    if (!HasMode(Mode.TriggerManually)) {
                        OnValueChanged();
                    }
                }
            }
            [SerializeField]
            [HideInInspector]
            private LogicConnections _connections;
            private LevelPlayer _player;
            private Dictionary<int, BaseComponent> _components;
            public override void Start(in InitArgs initData)
            {
                _player = initData.player;
                _components = initData.components;
                if (HasMode(Mode.TriggerOnAwake)) {
                    initData.player.FixedActionManager.DoTimedAction(0, null, DelayedTriggerOnAwake, null);
                }
            }
            private bool HasMode(Mode mode) {
                return (mode & this.triggerOnAwake) != 0;
            }
            private void DelayedTriggerOnAwake(object data, bool completed) {
                OnValueChanged();
            }
            public override LogicConnections Connections { get => _connections; set => _connections = value; }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch(index) {
                    case 10: Value  = (float)input;  break;
                    case 11: Value += (float)input;  break;
                    case 12: Value -= (float)input;  break;
                    case 14: Value++;                break;
                    case 15: Value--;                break;
                    case 13: OnValueChanged();       break;
                    case 20: return _value;
                }
                return null;
            }
            [Flags]
            public enum Mode
            {
                TriggerOnAwake = 0x1,
                TriggerManually = 0x2,
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("set value", 10, typeof(float)));
                slots.Add(new LogicSlot("get value", 20, null, typeof(float)));
                slots.Add(new LogicSlot("increment value by", 11, typeof(float)));
                slots.Add(new LogicSlot("decrement value by", 12, typeof(float)));
                slots.Add(new LogicSlot("increment value by one", 14));
                slots.Add(new LogicSlot("decrement value by one", 15));
                slots.Add(new LogicSlot("trigger", 13));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("on value change", 0, typeof(float), null));
            }
#endif
            public void OnValueChanged() {
                Connections.InvokeSimple(this, 0, Value, _components);
            }
        }
    }
}