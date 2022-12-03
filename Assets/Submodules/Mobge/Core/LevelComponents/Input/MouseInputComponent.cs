using Mobge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components
{
    public class MouseInputComponent : ComponentDefinition<MouseInputComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent
        {
            [SerializeField] private LogicConnections _connections;
            public Vector3 center;
            public Vector3 normal = new Vector3(0, 0, 1);
            public bool updateOnHover;
            public override LogicConnections Connections { get => _connections; set => _connections = value; }

            private LevelPlayer _player;
            private Dictionary<int, BaseComponent> _components;

            private bool _pressed;

            public override void Start(in InitArgs initData) {
                _player = initData.player;
                _components = initData.components;
                initData.player.FixedRoutineManager.DoRoutine(Update);
            }
            private void Update(float dt, object obj) {
                var pos = Input.mousePosition;
                var r = Camera.main.ScreenPointToRay(pos);
                Plane plane = new Plane(normal, center);
                var pressed = Input.GetMouseButton(0);
                if (updateOnHover || pressed) {
                    if (plane.Raycast(r, out float enter)) {
                        _connections.InvokeSimple(this, 0, r.origin + r.direction * enter, _components);
                    }
                }
                if (pressed != _pressed) {
                    _pressed = pressed;
                    if (_pressed) {
                        _connections.InvokeSimple(this, 1, null, _components);
                    }
                    else {
                        _connections.InvokeSimple(this, 2, null, _components);
                    }
                }

            }

#if UNITY_EDITOR
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("mouse position", 0, typeof(Vector3)));
                slots.Add(new LogicSlot("mouse down", 1));
                slots.Add(new LogicSlot("mouse up", 2));
            }
#endif
        }
    }
}