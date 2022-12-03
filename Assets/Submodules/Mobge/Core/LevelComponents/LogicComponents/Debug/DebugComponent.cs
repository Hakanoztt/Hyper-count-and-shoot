using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class DebugComponent : ComponentDefinition<DebugComponent.Data> {
        [Serializable]
        public class Data : BaseComponent {
            public string label;
            public bool useLabelAsFormat;
            public override void Start(in InitArgs initData) {

            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    default:
                        string l;
                        if (useLabelAsFormat) {
                            l = string.Format(label, input);
                        }
                        else {
                            l = label + ": " +  input;
                        }
                        Debug.Log(l, input as UnityEngine.Object);
                        break;
                }
                return null;
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("log", 0));
            }
#endif
        }
    }
}