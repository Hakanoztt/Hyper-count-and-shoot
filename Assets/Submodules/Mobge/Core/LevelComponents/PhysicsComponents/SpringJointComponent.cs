using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class SpringJointComponent : ComponentDefinition<SpringJointComponent.Data>
    {
        [Serializable]
        public class Data : JointComponentData {
            [SerializeField]
            [HideInInspector]
            private LogicConnections _connections;
            private InitArgs _initArgs;
            public bool enableCollision;
			public float dampingRatio;
			public float frequencyHertz = 5;
			public float breakForce = float.PositiveInfinity;
            private SpringJoint2D _joint;
            public override AnchoredJoint2D Joint => _joint;

            public override void Start(in InitArgs initData)
            {
                _initArgs = initData;
                initData.player.FixedActionManager.DoTimedAction(0, null, InitJoint);
            }
            private void InitJoint(object data, bool success) {
                if(success){
                    // This body is obligatory so there is no need for type checking. Program should crash if this is null
                    Rigidbody2D body1 = (Rigidbody2D)_connections.InvokeSimple(this, 0, null, _initArgs.components);
                    // This body is optional so we should cast it safely
                    Rigidbody2D body2 = _connections.InvokeSimple(this, 1, null, _initArgs.components) as Rigidbody2D;
                    var sj = body1.gameObject.AddComponent<SpringJoint2D>();
                    sj.enableCollision = enableCollision;
                    sj.anchor = body1.transform.InverseTransformPoint(position);
                    sj.connectedBody = body2;
					sj.dampingRatio = dampingRatio;
					sj.frequency = frequencyHertz;
                    sj.autoConfigureConnectedAnchor = true;
                    if(!breakForce.Equals(float.PositiveInfinity)) {
						sj.breakForce = breakForce;
                    }
                    _joint = sj;
                }
            }
            public override LogicConnections Connections {
                get {
                    return _connections;
                }
                set {
                    _connections = value;
                }
            }
            #if UNITY_EDITOR
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("body 1", 0, null, typeof(Rigidbody2D)));
                slots.Add(new LogicSlot("body 2", 1, null, typeof(Rigidbody2D)));
            }
            #endif
        }
    }
}