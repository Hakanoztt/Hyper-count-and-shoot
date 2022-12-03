using System;
using System.Collections.Generic;
using Mobge;
using Mobge.Core;
using UnityEngine;

namespace Mobge.Core.Components
{
    public class Rope2DComponent : ComponentDefinition<Rope2DComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent, IVisualSpawner
        {
            [SerializeField] [HideInInspector] private LogicConnections _connections;
            public override LogicConnections Connections {
                get => _connections;
                set => _connections = value;
            }
            public Vector2[] positions;
            [Layer] public int layer;
            [LabelPicker] public LineRendererPlus lineRes;
            public LineRendererPlus.PhysicalProperties physics;
            private LevelPlayer _player;
            private Dictionary<int, BaseComponent> _components;
            private LineRendererPlus _instance;

            private LineRendererPlus.PiecePhysics[] _bodies;

            public override void Start(in InitArgs initData) {
                _player = initData.player;
                _components = initData.components;
                _instance = InitializeRope(initData.parentTr);
                InitializeBodies();
                _player.FixedRoutineManager.DoAction(HandleConnections, 0);
            }

            private LineRendererPlus InitializeRope(Transform parent) {
                var p = Instantiate(lineRes);
                p.transform.SetParent(parent, false);
                p.transform.localPosition = position;
                UpdateParameters(p);
                return p;
            }
            private void InitializeBodies() {

                _bodies = _instance.AddPhysics(physics);
            }
            private void UpdateParameters(LineRendererPlus line) {
                if (positions.Length > 0) {
                    line.Initialize(positions, line.Width);
                    for (int i = 0; i < line.PieceCount; i++) {
                        line.GetPiece(i).gameObject.layer = this.layer;
                    }
                }
            }

            private void HandleConnections(bool completed, object data) {
                var head = _connections.InvokeSimple(this, 0, null, _components) as Rigidbody2D;
                Connect(0, 0, head);
                var tail = _connections.InvokeSimple(this, 1, null, _components) as Rigidbody2D;
                int tailIndex = _instance.PieceCount - 1;
                Connect(tailIndex, _instance[tailIndex].length, tail);
            }
            private void Connect(int index, float offset, Rigidbody2D target) {
                if(target == null) {
                    return;
                }
                var j = _bodies[index].body.gameObject.AddComponent<HingeJoint2D>();
                j.autoConfigureConnectedAnchor = true;
                j.anchor = new Vector2(0, offset);
                j.connectedBody = target;
            }

#if UNITY_EDITOR
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("attach head", 0, null, typeof(Rigidbody2D)));
                slots.Add(new LogicSlot("attach tail", 1, null, typeof(Rigidbody2D)));
            }

            Transform IVisualSpawner.CreateVisuals() {
                if (lineRes == null || positions.Length == 0) {
                    return null;
                }
                return InitializeRope(null).transform;
            }

            void IVisualSpawner.UpdateVisuals(Transform instance) {
                if (instance == null) {
                    return;
                }
                var rp = instance.GetComponent<LineRendererPlus>();
                if (rp == null) {
                    return;
                }
                UpdateParameters(rp);
            }
#endif
        }
    }
}