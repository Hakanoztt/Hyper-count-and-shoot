using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class PhysicalComplex : ComponentDefinition<PhysicalComplex.Data> {

#if UNITY_EDITOR
        public Texture2D editorButtonBg;
#endif
        [Serializable]
        public class Data : BaseComponent {

            private static ExposedList<int> s_tempKeys = new ExposedList<int>();

            public Dictionary<int, JointData> jointDatas;
            public Dictionary<int, RigidbodyData> physicalBodies;
            public float breakForceMultiplayer = 1;
            public float breakTorqueMultiplayer = 1;
            public float massMultiplayer = 1;
            public float breakForceTime = 0;
            public ReusableReference breakEffect;

            public Action<JointData, Vector3> onBreak;

            public override LogicConnections Connections { get => _connections; set => _connections = value; }

            [SerializeField] [HideInInspector] private LogicConnections _connections;
            private Dictionary<int, BaseComponent> _components;

            private LevelPlayer _player;

            public override void Start(in InitArgs initData) {
                _player = initData.player;
                _components = initData.components;

                _player.FixedActionManager.DoRoutine(FixedUpdate);
                _player.FixedActionManager.DoTimedAction(0, null, DelayedStart);
            }

            private void DelayedStart(object data, bool completed) {

                var rbe = physicalBodies.GetEnumerator();
                UpdateTempKeys(physicalBodies);
                for (int i = 0; i < s_tempKeys.Count; i++) {
                    var key = s_tempKeys.array[i];
                    var c = physicalBodies[key];
                    var rbd = (PrefabSpawnerComponent.Data)_components[key];
                    var rb = rbd.Instance.GetComponent<Rigidbody2D>();
                    rb.mass *= massMultiplayer;
                    c.body = rb;
                    physicalBodies[key] = c;

                }
                UpdateTempKeys(jointDatas);
                for (int i = 0; i < s_tempKeys.Count; i++) {
                    var key = s_tempKeys.array[i];
                    var c = jointDatas[key];
                    var jData = (JointComponentData)_components[key];
                    c.jointData = jData;
                    jointDatas[key] = c;
                }
            }
            private void UpdateTempKeys<T>(Dictionary<int, T> dic) {
                s_tempKeys.ClearFast();
                var e = dic.Keys.GetEnumerator();
                while (e.MoveNext()) {
                    s_tempKeys.Add(e.Current);
                }
            }
            private void FixedUpdate(float obj) {
                var e = jointDatas.GetEnumerator();
                while (e.MoveNext()) {
                    var c = e.Current;
                    var jData= (JointComponentData)_components[c.Key];
                    var joint = jData.Joint;
                    if (joint.enabled) {
                        var force = breakForceMultiplayer * c.Value.breakForce;
                        if(joint.GetReactionForce(breakForceTime).sqrMagnitude > force * force || joint.GetReactionTorque(breakForceTime) > breakTorqueMultiplayer * c.Value.breakForce) {
                            joint.enabled = false;
                            HandleBreak(joint.transform.TransformPoint(joint.anchor), c.Value);
                        }
                    }
                }
            }

            private void HandleBreak(Vector3 position, JointData jointData) {
                var e = breakEffect.SpawnItem(position, _player.transform);
                if (onBreak != null) {
                    onBreak(jointData, position);
                }
                if (_connections != null) {
                    _connections.InvokeSimple(this, 0, position, _components);
                }
            }

            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case 0:
                        return this;
                }
                return null;
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("this", 0, null, typeof(Data), true));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("on break at position", 0, typeof(Vector3)));
            }
#endif
        }
        [Serializable]
        public struct RigidbodyData {
            [NonSerialized] public Rigidbody2D body;
        }
        [Serializable]
        public struct JointData {
            public float breakForce;
            [NonSerialized] public JointComponentData jointData;
        }
    }
}
            