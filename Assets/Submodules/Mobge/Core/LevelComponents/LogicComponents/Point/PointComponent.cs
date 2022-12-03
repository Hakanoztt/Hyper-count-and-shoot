using System;
using System.Collections.Generic;
using Mobge.Core;
using UnityEngine;

namespace Mobge {
    public class PointComponent : ComponentDefinition<PointComponent.Data> {
        [Serializable]
        public class Data : BaseComponent, IChild {
            public ElementReference parent = -1;
            ElementReference IChild.Parent { get => parent; set => parent = value; }

            public Transform ParentTr { get; private set; }

            public override void Start(in InitArgs initData) {
                ParentTr = initData.parentTr;
            }


            public Vector3 WorldPosition {
                get {
                    if (ParentTr == null) {
                        return position;
                    }
                    return ParentTr.TransformPoint(position);
                }
            }

            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case 0:
                        return WorldPosition;
                }
                return null;
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("get location", 0, null, typeof(Vector3)));
            }

#endif
        }
    }
}
            