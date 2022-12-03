using System;
using System.Collections.Generic;
using Mobge.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Mobge {
    public class RandomComponent : ComponentDefinition<RandomComponent.Data> {
        [Serializable]
        public class Data : BaseComponent {

            public float minValue = 0f;
            public float maxValue = 1f;
            public bool triggerAtStart = false;
            [Tooltip("seed = 0 is random seed, any other value sets seed before generating random value")]
            public int seed = 0;

            private Random.State _state;
            
            public override LogicConnections Connections { get => connections; set => connections = value; }
            private Dictionary<int, BaseComponent> _components;
            [SerializeField] [HideInInspector] private LogicConnections connections;

            public override void Start(in InitArgs initData) {
                _components = initData.components;
                if (seed != 0) {
                    var globalState = Random.state;
                    Random.InitState(seed);
                    _state = Random.state;
                    Random.state = globalState;
                }
                if (triggerAtStart) {
                    initData.player.FixedRoutineManager.DoAction(DelayedInit, 0);
                }
            }
            private void DelayedInit(bool completed, object data) {
                Connections.InvokeSimple(this, 0, GetRandom(), _components);
            }
            public float GetRandom() {
                if (seed == 0) {
                    return Random.Range(minValue, maxValue);
                }
                else {
                    var globalState = Random.state;
                    Random.state = _state;
                    var randomValue = Random.Range(minValue, maxValue);
                    Random.state = globalState;
                    return randomValue;
                }
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case 0:
                        return GetRandom();
                    case 1:
                        Connections.InvokeSimple(this, 0, GetRandom(), _components);
                        break;
                }
                return null;
            }
            #if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("trigger", 1));
                slots.Add(new LogicSlot("get random value", 0, null, typeof(float)));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("on random value", 0, typeof(float), null));
            }
            #endif
            
        }
    }
}