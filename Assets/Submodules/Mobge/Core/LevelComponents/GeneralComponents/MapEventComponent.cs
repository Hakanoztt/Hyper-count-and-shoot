using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class MapEventComponent : ComponentDefinition<MapEventComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent
        {
            [SerializeField] [HideInInspector] private LogicConnections _connections;
            public override LogicConnections Connections {get => _connections; set => _connections = value; }
            private InitArgs _args;
            public override void Start(in InitArgs initData)
            {
                _args = initData;
                if(_connections.GetConnections(0).MoveNext()) {
                    initData.player.FixedRoutineManager.DoAction(OnStart, Time.fixedDeltaTime + 0.001f);
                }
            }

            private void OnStart(bool completed, object data)
            {
                _connections.InvokeSimple(this, 0, null, _args.components);
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("game start", 0));
            }
#endif
        }
    }
}