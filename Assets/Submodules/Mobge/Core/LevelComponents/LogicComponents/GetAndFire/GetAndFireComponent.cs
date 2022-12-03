using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class GetAndFireComponent : ComponentDefinition<GetAndFireComponent.Data> {
        [Serializable]
        public class Data : BaseComponent {
            [SerializeField, HideInInspector] private LogicConnections _connections;
            private Dictionary<int, BaseComponent> _components;
            public bool triggerAtStart = false;
            public override void Start(in InitArgs initData) {
                _components = initData.components;
                if (triggerAtStart) {
                    initData.player.FixedRoutineManager.DoAction(ConditionalDelayedInit, 0f);
                }
            }
            private void ConditionalDelayedInit(bool completed, object data) {
                Trigger();
            }
            private void Trigger() {
                var arg = Connections.InvokeSimple(this, 0, null, _components);
                Connections.InvokeSimple(this, 1, arg, _components);
            }
            public override LogicConnections Connections { get => _connections; set => _connections = value; }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch(index) {
                    case 0: Trigger(); break;
                }
                return null;
            }
#if UNITY_EDITOR
            [NonSerialized] public Type parameterType, returnType;
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("trigger", 0, null));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("get parameter", 0, null, null));
                slots.Add(new LogicSlot("fire wire", 1, parameterType, returnType));
            }
#endif
        }
    }
}