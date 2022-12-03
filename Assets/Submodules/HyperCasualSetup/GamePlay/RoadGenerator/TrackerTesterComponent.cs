using System;
using System.Collections.Generic;
using Mobge.Core;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    public class TrackerTesterComponent : ComponentDefinition<TrackerTesterComponent.Data> {
        [Serializable]
        public class Data : BaseComponent {
            public override LogicConnections Connections { get => connections; set => connections = value; }
            [SerializeField] [HideInInspector] private LogicConnections connections;
            private Dictionary<int, BaseComponent> _components;

            private LevelPlayer _player;
            private RoadTracker _tracker;
            private Transform _test;
            public override void Start(in InitArgs initData) {
                _player = initData.player;
                _components = initData.components;
                
                _tracker = (RoadTracker) Connections.InvokeSimple(this, 0, null, _components);
                _test = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                
                _player.RoutineManager.DoRoutine(Update);
            }
            private void Update(float obj, object data) {
                if (Input.GetMouseButtonDown(0)) {
                    _tracker.MoveForward(10f);
                }
                _tracker.MoveForward(Time.deltaTime * 10f);
                _test.position = _tracker.Current.position + Vector3.up * 5f;
                _test.rotation = _tracker.Current.rotation;
            }
            //public override object HandleInput(ILogicComponent sender, int index, object input) {
            //    switch (index) {
            //        case 0:
            //            return "example output";
            //    }
            //    return null;
            //}
        #if UNITY_EDITOR
            //public override void EditorInputs(List<LogicSlot> slots) {
            //    slots.Add(new LogicSlot("example input", 0));
            //}
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("tracker", 0));
            }
        #endif
        }
    }
}
            