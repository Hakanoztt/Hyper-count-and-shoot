using System;
using System.Collections.Generic;
using ElephantSDK;
using Mobge.Core;
using UnityEngine;

namespace Mobge.Telemetry
{
    public class RemoteConfigComponent : ComponentDefinition<RemoteConfigComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent {
            [SerializeField] [HideInInspector] private LogicConnections _connections;
            public string configName;
            public bool autoTrigger;

            private Dictionary<int, BaseComponent> _components;
            private float _value;

            public override void Start(in InitArgs initData) {
                try {
                    float.TryParse(RemoteConfig.GetInstance().Get(configName), out _value);
                    _components = initData.components;
                }
                catch { }
                if (autoTrigger) {
                    initData.player.FixedRoutineManager.DoAction(DelayedInit, 0);
                }
            }

            private void DelayedInit(bool completed, object data) {
                _connections.InvokeSimple(this, 0, _value, _components);
            }

            public override LogicConnections Connections {
                get => _connections;
                set => _connections = value;
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    default:
                    case 0:
                        return _value;
                    case 1:
                        _connections.InvokeSimple(this, 0, _value, _components);
                        break;
                }
                return null;
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("Get Value", 0));
                slots.Add(new LogicSlot("Trigger Value", 1));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("Value", 0, typeof(float)));
            }
#endif
        }
    }
}