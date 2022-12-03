using System;
using System.Collections.Generic;
using Mobge.Core;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    public class RunnerRampComponent : ComponentDefinition<RunnerRampComponent.Data> {
        [Serializable]
        public class Data : BaseComponent {
            //public override LogicConnections Connections { get => connections; set => connections = value; }
            //[SerializeField] [HideInInspector] private LogicConnections connections;
            //private Dictionary<int, BaseComponent> _components;

            //private LevelPlayer _player;

            public override void Start(in InitArgs initData) {
                //_player = initData.player;
                //_components = initData.components;
                
                //string value = "example argument value";
                //Connections.InvokeSimple(this, 0, value, _components);
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
            //public override void EditorOutputs(List<LogicSlot> slots) {
            //    slots.Add(new LogicSlot("example output", 0));
            //}
        #endif
        }
    }
}
            