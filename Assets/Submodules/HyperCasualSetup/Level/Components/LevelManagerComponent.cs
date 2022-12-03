using Mobge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.Components
{
    public class LevelManagerComponent : ComponentDefinition<LevelManagerComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent
        {
            private BaseLevelPlayer _player;
            private Dictionary<int, BaseComponent> _components;
            [SerializeField, HideInInspector] private LogicConnections _connections;

            public override LogicConnections Connections { get => _connections; set => _connections = value; }
            public override void Start(in InitArgs initData) {
                _player = (BaseLevelPlayer)initData.player;
                _components = initData.components;
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    default:
                    case 0:
                        if (_player.FinishGame(true)) {
                            return _connections.InvokeSimple(this, 0, input, _components);
                        }
                        break;
                    case 1:
                        if (_player.FinishGame(false)) {
                            return _connections.InvokeSimple(this, 1, input, _components);
                        }
                        break;
                }
                return null;
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("level completed", 0));
                slots.Add(new LogicSlot("level failed", 1));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("on success", 0));
                slots.Add(new LogicSlot("on fail", 1));
            }
#endif
        }
    }
}