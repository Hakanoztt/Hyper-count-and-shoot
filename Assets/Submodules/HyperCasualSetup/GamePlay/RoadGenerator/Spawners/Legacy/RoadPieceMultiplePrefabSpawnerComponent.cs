using System;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.Core.Components;
using UnityEditor;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {

    public class RoadPieceMultiplePrefabSpawnerComponent : ComponentDefinition<RoadPieceMultiplePrefabSpawnerComponent.Data> {

        [Serializable]
        public class Data : BaseRoadElementComponent.Data {

            public const int c_componentSlotIdStart = 1024;
            [SerializeField, HideInInspector] private LogicConnections _connections;
            [HideInInspector] public ObjectReference[] objects;
            public Component[] prefabReferences;
            public bool autoSpawn = true;
            public SpawnModule spawnModule;
            public float autoMovementOffsetTime = 1;
            public bool movementTimesRelativeToTheFirst = true;
            [HideInInspector] public ConnectionMap[] componentConnections;

            private bool _spawnStarted;
            private InitArgs _initArgs;
            private IMovementModule _movementModule;
            private RoutineManager.Routine _spawnRoutine;
            private int _elementToSpawn;
            private int _object0Id;
            private int[] _eventCounts;

            private Dictionary<int, BaseComponent> _components;

            public override void Start(in InitArgs initData) {
                base.Start(initData);

                _initArgs = initData;

                initData.player.FixedRoutineManager.DoAction(DelayedStart);
                _components = initData.components;

                if (_connections != null) {
                    _movementModule = _connections.InvokeSimple(this, 0, null, _initArgs.components) as IMovementModule;
                }

                for (int i = 0; i < objects.Length; i++) {
                    Init(i);
                }
                //InitializeObjectConnections();
                if (autoSpawn) {
                    StartSpawnRoutine();
                }
            }

            private void DelayedStart(bool complete, object data) {
                if (complete) {
                    var road = ((RoadGeneratorComponent.Data)_components[roadGenerator]);
                    Transform parent = road.items[parentIndex].instance.gameObject.transform;

                    foreach(var obj in objects) {
                        obj.instance.transform.SetParent(parent, false);
                    }
                }
            }

            private void StartSpawnRoutine() {
                if (!_spawnStarted) {

                    _spawnStarted = true;
                    if (_spawnRoutine.IsFinished) {
                        _spawnRoutine = _initArgs.player.FixedRoutineManager.DoRoutine(SpawnUpdate);
                    }
                }
            }

            private void SpawnUpdate(float time, object data) {
                if (spawnModule.spawnCooldown <= 0) {
                    for (int i = 0; i < objects.Length; i++) {
                        Spawn(i);
                    }
                    _spawnRoutine.Stop();
                } else {
                    if (time >= spawnModule.spawnCooldown * _elementToSpawn) {
                        Spawn(_elementToSpawn);
                        _elementToSpawn++;
                        if (_elementToSpawn == objects.Length) {
                            _spawnRoutine.Stop();
                        }
                    }
                }
            }

            private Vector3 Divide(Vector3 v1, Vector3 divider) {
                return new Vector3(v1.x / divider.x, v1.y / divider.y, v1.z / divider.z);
            }

            public void Init(int index) {
                Pose p = new Pose(position, rotation);
                var obj = objects[index];
                var pose = new Pose(obj.position, obj.rotation);
                pose = pose.GetTransformedBy(p);
                var o = obj.PickComponent(prefabReferences, out int componentIndex);
                o = Instantiate(o);
                var otr = o.transform;
                otr.SetParent(_initArgs.parentTr, false);
                otr.localPosition = Divide(pose.position, _initArgs.parentTr.localScale);
                otr.localRotation = pose.rotation;
                otr.gameObject.SetActive(false);

                obj.instance = o;
                objects[index] = obj;

                if (o is IComponentExtension compEx) {
                    compEx.Start(_initArgs);
                }

                if (o is ILogicComponent lc && _connections != null && componentConnections != null) {
                    for (int i = 0; i < componentConnections.Length; i++) {
                        var cc = componentConnections[i];
                        if (cc.componentIndex == componentIndex) {
                            var cons = _connections.GetConnections(cc.slotId);
                            if (cons.MoveNext()) {
                                var conList = lc.Connections;
                                if (conList == null) {
                                    conList = new LogicConnections();
                                    lc.Connections = conList;
                                }
                                if (cc.all) {
                                    if (this._eventCounts == null) {
                                        _eventCounts = new int[componentConnections.Length];
                                    }
                                    _eventCounts[i]++;
                                    LogicConnection lg;
                                    lg.input = cc.slotId;
                                    lg.output = cc.componentOutputId;
                                    lg.target = _initArgs.componentId;
                                    conList.AddConnection(lg);
                                } else {
                                    do {
                                        var con = cons.Current;
                                        con.output = cc.componentOutputId;
                                        conList.AddConnection(con);
                                    } while (cons.MoveNext());
                                }
                            }
                        }
                    }
                }
            }

            private int IndexOfComponentConnection(int output) {
                for (int i = 0; i < componentConnections.Length; i++) {
                    if (componentConnections[i].slotId == output) {
                        return i;
                    }
                }
                return -1;
            }

            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                default:
                    int componentOutputIndex = IndexOfComponentConnection(index);
                    int count = --this._eventCounts[componentOutputIndex];
                    if (count == 0) {
                        _connections.InvokeSimple(this, index, input, _initArgs.components);
                    }
                    break;
                case 0:
                    StartSpawnRoutine();
                    break;
                case 1:
                    Spawn((int) (float) input);
                    break;
                }
                return 0;
            }

            private void Spawn(int index) {
                var o = objects[index];
                if (o.spawned) {
                    return;
                }
                objects[index].spawned = true;
                o.instance.gameObject.SetActive(true);
                if (_movementModule != null) {
                    var time = o.movementOffset + (index * autoMovementOffsetTime / objects.Length);
                    if (index != 0) {
                        if (movementTimesRelativeToTheFirst) {
                            time += _movementModule.GetNormalizedTime(_object0Id);
                        }
                    }
                    time = Mathf.Repeat(time, 1f);
                    var id = _movementModule.AddBody(o.instance.GetComponent<Rigidbody2D>(), time);
                    if (index == 0) {
                        _object0Id = id;
                    }
                    _movementModule.SetSpeed(id, 1);
                }
            }

            public override LogicConnections Connections {
                get => _connections;
                set => _connections = value;
            }

#if UNITY_EDITOR
            public static List<LogicSlot> s_tempSlots = new List<LogicSlot>();
            public static ConnectionMap[] s_defaultMaps = new ConnectionMap[0];

            private static int GetTempSlotId(int output) {
                for (int i = 0; i < s_tempSlots.Count; i++) {
                    if (s_tempSlots[i].id == output) {
                        return i;
                    }
                }
                return -1;
            }

            public void TrimComponentConnections() {
                if (prefabReferences == null) {
                    componentConnections = s_defaultMaps;
                }
                if (componentConnections != null) {
                    for (int i = 0; i < componentConnections.Length;) {
                        var cc = componentConnections[i];
                        if (cc.componentIndex < prefabReferences.Length) {
                            var comp = prefabReferences[cc.componentIndex] as ILogicComponent;
                            if (comp != null) {
                                s_tempSlots.Clear();
                                comp.EditorOutputs(s_tempSlots);
                                if (GetTempSlotId(cc.componentOutputId) >= 0) {
                                    i++;
                                    continue;
                                }
                            }
                        }
                        ArrayUtility.RemoveAt(ref componentConnections, i);
                    }
                }
            }

            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("Start spawn", 0));
                slots.Add(new LogicSlot("Spawn at index", 1));
            }

            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot(typeof(IMovementModule).ToString(), 0, null, typeof(IMovementModule)));
                TrimComponentConnections();
                if (this.componentConnections != null) {
                    for (int i = 0; i < componentConnections.Length; i++) {
                        var cc = componentConnections[i];
                        var c = (ILogicComponent) this.prefabReferences[cc.componentIndex];
                        s_tempSlots.Clear();
                        c.EditorOutputs(s_tempSlots);
                        var slot = s_tempSlots[GetTempSlotId(cc.componentOutputId)];
                        slot.id = cc.slotId;
                        if (cc.all) {
                            slot.name = "All: " + slot.name;
                        }
                        slots.Add(slot);
                    }
                }
            }
#endif
        }

        [Serializable]
        public struct ConnectionMap {
            public int componentIndex;
            public int componentOutputId;
            public int slotId;
            public bool all;
        }

        [Serializable]
        public struct SpawnModule {
            public float spawnCooldown;
        }

        [Serializable]
        public struct ObjectReference {

            public Vector3 position;
            public Quaternion rotation;
            public int index;
            public float movementOffset;
            [NonSerialized] public bool spawned;
            [NonSerialized] public Component instance;

            public Component PickComponent(Component[] comps, out int index) {
                if (this.index < 0) {
                    index = UnityEngine.Random.Range(0, comps.Length);
                    return comps[index];
                }
                index = this.index;
                return comps[this.index];
            }
        }
    }

}