using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components
{
    public class Rigidbody2DOperationsComponent : ComponentDefinition<Rigidbody2DOperationsComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent
        {
            [SerializeField, HideInInspector] private LogicConnections _connections;
            private Rigidbody2D _rb;
            private LevelPlayer _player;
            private Dictionary<int, BaseComponent> _components;

            public override LogicConnections Connections { get => _connections; set => _connections = value; }
            public override void Start(in InitArgs initData) {
                _player = initData.player;
                _components = initData.components;
                _player.FixedActionManager.DoTimedAction(0, null, RequestBody);
            }

            private void RequestBody(object data, bool completed) {
                if(_rb == null) {
                    _rb = _connections.InvokeSimple(this, 0, null, _components) as Rigidbody2D;
                }
            }

            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    default:
                    case 0:
                        _rb = (Rigidbody2D)input;
                        break;
                    case 1:
                        return (Vector3)_rb.position;
                    case 2:
                        _rb.position = (Vector3)input;
                        break;
                    case 3:
                        _rb.velocity = ((Vector2)(Vector3)input - _rb.position) / Time.fixedDeltaTime;
                        break;
                    case 4:
                        _rb.velocity = (Vector3) input;
                        break;
                }

                return null;
            }

#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("set body", 0, typeof(Rigidbody2D)));
                slots.Add(new LogicSlot("get position", 1, null, typeof(Vector3)));
                slots.Add(new LogicSlot("set position", 2, typeof(Vector3)));
                slots.Add(new LogicSlot("set position by velocity", 3, typeof(Vector3)));
                slots.Add(new LogicSlot("set velocity", 4, typeof(Vector3)));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("request body", 0, typeof(Rigidbody2D)));
            }
#endif
        }
    }
}