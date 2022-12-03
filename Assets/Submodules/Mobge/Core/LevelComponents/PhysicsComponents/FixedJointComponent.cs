
using System;
using System.Collections.Generic;
using Mobge.Core;
using UnityEngine;

namespace Mobge.Core.Components {
    public class FixedJointComponent : ComponentDefinition<FixedJointComponent.Data> {
        [Serializable]
        public class Data : JointComponentData {
            
            public bool enableCollision;

            public override LogicConnections Connections { get => _connections; set => _connections = value; }
           [SerializeField, HideInInspector] private LogicConnections _connections;
            //private Dictionary<int, BaseComponent> _components;

            //private LevelPlayer _player;
            private FixedJoint2D _joint;
            private Dictionary<int, BaseComponent> _components;

            public override AnchoredJoint2D Joint => _joint;
            public override void Start(in InitArgs initData) {
                _components = initData.components;
                initData.player.FixedActionManager.DoTimedAction(0, null, InitJoint);
            }

            private void InitJoint(object data, bool completed) {

                // This body is obligatory so there is no need for type checking. Program should crash if this is null
                Rigidbody2D body1 = (Rigidbody2D)_connections.InvokeSimple(this, 0, null, _components);
                // This body is optional so we should cast it safely
                Rigidbody2D body2 = _connections.InvokeSimple(this, 1, null, _components) as Rigidbody2D;

                _joint = body1.gameObject.AddComponent<FixedJoint2D>();
                _joint.autoConfigureConnectedAnchor = false;
                _joint.enableCollision = enableCollision;
                _joint.anchor = body1.transform.InverseTransformPoint(position);
                _joint.connectedAnchor = body2.transform.InverseTransformPoint(position);
                _joint.connectedBody = body2;
                

            }

            //public override object HandleInput(ILogicComponent sender, int index, object input) {
            //    switch (index) {
            //        case 0:
            //            //on example input fired
            //            return "example output";
            //    }
            //    return null;
            //}
#if UNITY_EDITOR
            //public override void EditorInputs(List<LogicSlot> slots) {
            //    slots.Add(new LogicSlot("example input", 0));
            //}
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("body 1", 0, null, typeof(Rigidbody2D)));
                slots.Add(new LogicSlot("body 2", 1, null, typeof(Rigidbody2D)));
            }
#endif
        }
    }
}
            