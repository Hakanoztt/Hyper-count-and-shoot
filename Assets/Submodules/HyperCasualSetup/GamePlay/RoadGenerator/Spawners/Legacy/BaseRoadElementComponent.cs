using System;
using Mobge.Core;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    public class BaseRoadElementComponent : ComponentDefinition<BaseRoadElementComponent.Data> {
        [Serializable]
        public class Data : BaseComponent, IRotationOwner {

            public Quaternion rotation = Quaternion.identity;
            [HideInInspector] public int parentIndex;
            [HideInInspector, ElementReference(typeof(RoadGeneratorComponent.Data))] public ElementReference roadGenerator = -1;


            Quaternion IRotationOwner.Rotation { get => rotation; set => rotation = value; }

            public override void Start(in InitArgs initData) {

            }

            internal protected virtual void RoadCreated(RoadGeneratorComponent.Data data) {

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