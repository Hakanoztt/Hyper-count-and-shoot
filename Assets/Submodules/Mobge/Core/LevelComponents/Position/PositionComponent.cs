using Mobge.Core;
using Mobge.Core.Components;
using Mobge.Platformer.Character;
using System;
using System.Collections;
using System.Collections.Generic;
using Mobge.Animation;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace Mobge.Core.Components {
    public class PositionComponent : ComponentDefinition<PositionComponent.Data> {
        [Serializable]
        public class Data : BaseComponent, IChild {
            [SerializeField]
            [HideInInspector]
            private ElementReference _parent = -1;
            ElementReference IChild.Parent { get => _parent; set => _parent = value; }
            private Transform parentTransform;
            public override void Start(in InitArgs initData) {
                _components = initData.components;
                parentTransform = initData.parentTr;
                Connections.InvokeSimple(this, 0, parentTransform.position + position, _components);
            }
            #region Logic Connections
            [SerializeField]
            [HideInInspector]
            private LogicConnections _connections;
            private Dictionary<int, BaseComponent> _components;
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case 0:
                        return parentTransform.position + position;                        
                }
                return null;
            }

            public override LogicConnections Connections { get => _connections; set => _connections = value; }
            #if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("get position", 0, null, typeof(Vector3)));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("get position", 0, typeof(Vector3), null));
            }
#endif
            #endregion
        }
    }
}