using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mobge;
using Mobge.Core;
using UnityEngine;

namespace Mobge.Core.Components
{
    public class Rigidbody2DInstantiaterComponent : ComponentDefinition<Rigidbody2DInstantiaterComponent.Data>
    {
        [Serializable]
        public struct Pool {
            public Rigidbody2D body;
            private int _nextIndex;
            private Rigidbody2D[] _bodies;
            public void Initialize(Transform parent, int cacheCount, bool instantiateEarly) {
                _nextIndex = 0;
                _bodies = new Rigidbody2D[cacheCount];
                if (instantiateEarly) {
                    for(int i = 0; i < _bodies.Length; i++) {
                        var b = Instantiate(body, parent, false);
                        b.gameObject.SetActive(false);
                        _bodies[i] = b;
                    }
                }
            }

            public Rigidbody2D SpawnOne(Transform parent, Vector3 position) {
                Rigidbody2D obj;
                if ((obj = _bodies[_nextIndex]) == null) {
                    obj = Instantiate(body, parent, false);
                    obj.transform.position = position;
                    _bodies[_nextIndex] = obj;
                }
                else {
                    obj.gameObject.SetActive(false);
                    obj.transform.position = position;
                    obj.gameObject.SetActive(true);
                }
                _nextIndex++;
                if(_nextIndex == _bodies.Length) {
                    _nextIndex = 0;
                }
                return obj;
            }
        }

        [Serializable]
        public class Data : BaseComponent
        {
            [SerializeField] [HideInInspector] private LogicConnections _connections;
            private LevelPlayer _player;
            private Dictionary<int, BaseComponent> _components;
            public Vector2 spawnVelocity;
            public IntervalF spawnAngle;
            public IntervalF spawnAngularVelocity;
            public int maxCount = 8;
            public bool cacheObjectsOnStart = true;
            public Pool[] bodies;
            public SpawnMode mode;

            private Vector2 _spawnVelocity;
            private int _nextObject = -1;

            public override Transform PrefabReference => (bodies.Length > 0 && bodies[0].body) ? bodies[0].body.transform : null;

            public override void Start(in InitArgs initData) {
                _player = initData.player;
                _components = initData.components;
                _spawnVelocity = spawnVelocity;
                for(int i = 0; i < bodies.Length; i++) {
                    bodies[i].Initialize(initData.parentTr, maxCount, cacheObjectsOnStart);
                }
            }

            public override LogicConnections Connections {
                get => _connections;
                set => _connections = value;
            }

            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    default:
                    case 0:
                        Spawn();
                        break;
                    case 1:
                        Spawn((Vector3)input);
                        break;
                    case 2:
                        _spawnVelocity = (Vector3)input;
                        break;
                }
                return null;
            }

            private Rigidbody2D Spawn() {
                return Spawn(position);
            }
            private Rigidbody2D Spawn(Vector2 position) {
                switch (mode) {
                    default:
                    case SpawnMode.Ordered:
                        _nextObject++;
                        if (_nextObject == bodies.Length) {
                            _nextObject = 0;
                        }
                        break;
                    case SpawnMode.Random:
                        _nextObject = UnityEngine.Random.Range(0, bodies.Length);
                        break;
                }
                var b = bodies[_nextObject].SpawnOne(_player.transform, position);
                b.rotation = this.spawnAngle.NextRandom();
                b.angularVelocity = this.spawnAngularVelocity.NextRandom();
                b.velocity = _spawnVelocity;
                _connections.InvokeSimple(this, 0, b, _components);
                return b;
            }

            public enum SpawnMode {
                Ordered = 0,
                Random = 1,
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("spawn", 0));
                slots.Add(new LogicSlot("spawn at position", 1, typeof(Vector3)));
                slots.Add(new LogicSlot("set spawn velocity", 1, typeof(Vector3)));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("on spawn", 0, typeof(Rigidbody2D)));
            }
#endif
            [Serializable]
            public struct IntervalF {
                public float min, max;
                public float NextRandom() {
                    return UnityEngine.Random.Range(min, max);
                }
            }
        }
    }
}