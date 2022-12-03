using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class DisableCollision : ComponentDefinition<DisableCollision.Data> {

        [Serializable]
        public class Data : BaseComponent {
            private static List<Collider2D> _tempList = new List<Collider2D>();
            private static Collider2D[] _tempColliders = new Collider2D[32];
            [SerializeField]
            [HideInInspector]
            private LogicConnections _connections;
            private Dictionary<int, BaseComponent> _components;
            public override void Start(in InitArgs initData) {
                _components = initData.components;
                initData.player.FixedActionManager.DoTimedAction(0, null, DisableCollisions);
            }
            public override LogicConnections Connections { get => _connections; set => _connections = value; }

            private void DisableCollisions(object data, bool completed) {
                if (completed) {
                    var cons = _connections.Invoke(this, 0, null, _components);
                    while (cons.MoveNext()) {
                        var c = cons.Current as Rigidbody2D;
                        if (c != null) {
                            int count = c.GetAttachedColliders(_tempColliders);
                            for (int i = 0; i < count; i++) {
                                _tempList.Add(_tempColliders[i]);
                            }
                        }
                    }
                    for (int i = 0; i < _tempList.Count - 1; i++) {
                        for (int j = i + 1; j < _tempList.Count; j++) {
                            Physics2D.IgnoreCollision(_tempList[i], _tempList[j]);
                        }
                    }
                    _tempList.Clear();
                }
            }
#if UNITY_EDITOR
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("get bodies", 0, typeof(Rigidbody2D)));
            }
#endif
        }
    }
}
