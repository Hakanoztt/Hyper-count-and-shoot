using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public abstract class BaseListComponent<T, TL> : ComponentDefinition<T> where T : BaseListComponent<T,TL>.Data<TL> {
        [Serializable]
        public abstract class Data<TType> : BaseComponent {
            public Type WorkType => typeof(TType);
            public TType[] keyArray = new TType[0];
            public OutputMode outputMode;
            public enum OutputMode {
                Single = 0,
                Multiple = 1,
            }
            #region Connection
            [SerializeField]
            [HideInInspector]
            private LogicConnections connections;
            public override LogicConnections Connections { get => connections; set => connections = value; }
            private Dictionary<int, BaseComponent> _components;

            #endregion
            public override void Start(in InitArgs initData) {
                _components = initData.components;
            }
            public int Count => keyArray.Length;
            public void Trigger(int index) {
                switch (outputMode) {
                    case OutputMode.Single:   Connections.InvokeSimple(this, -10,  keyArray[index], _components); break;
                    case OutputMode.Multiple: Connections.InvokeSimple(this, index, keyArray[index], _components);  break;
                }
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case -10: Trigger((int)(float)input); return null;
                    case -11: return (float)this.keyArray.Length;
                    default: Trigger(index); return null;
                }
            }
            #if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("trigger by index", -10, typeof(float), null));
                for (int i = 0; i < keyArray.Length; i++) {
                    slots.Add(new LogicSlot(keyArray[i].ToString(), i, null, null));
                }
                slots.Add(new LogicSlot("get count", -11, null, typeof(float)));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                if (outputMode == OutputMode.Single)
                    slots.Add(new LogicSlot("output", -10, WorkType));
                else if (outputMode == OutputMode.Multiple)
                    for (int i = 0; i < keyArray.Length; i++) {
                        slots.Add(new LogicSlot(keyArray[i].ToString(), i, WorkType));
                    }
            }
            #endif
        }
    }
    
}

