using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class HingeJointComponent : ComponentDefinition<HingeJointComponent.Data>
    {
        [Serializable]
        public class Data : JointComponentData {
            [SerializeField]
            [HideInInspector]
            private LogicConnections _connections;
            private InitArgs _initArgs;
            public bool collideConnected = false;
            public bool useMotor;
            public float motorSpeed;
            public float maxMotorTorque;
            private HingeJoint2D _hingeJoint;
            public override AnchoredJoint2D Joint => _hingeJoint;
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
                    _hingeJoint = body1.gameObject.AddComponent<HingeJoint2D>();
                    _hingeJoint.enableCollision = collideConnected;
                    _hingeJoint.anchor = body1.transform.InverseTransformPoint(position);
                    _hingeJoint.connectedBody = body2;
                    if(useMotor) {
                        _hingeJoint.useMotor = true;
                        JointMotor2D motor = new JointMotor2D {
                            maxMotorTorque = maxMotorTorque, 
                            motorSpeed = motorSpeed};
                        _hingeJoint.motor = motor;
                    }
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
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                var motor = _hingeJoint.motor;
                switch(index) {
                    case 10: motor.motorSpeed = (float)input; break;
                    case 11: motor.maxMotorTorque = (float)input; break;
                }
                _hingeJoint.motor = motor;
                return null;
            }
            #if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("set motor speed", 10, typeof(float)));
                slots.Add(new LogicSlot("set motor torque", 11, typeof(float)));
            }
            
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("body 1", 0, null, typeof(Rigidbody2D)));
                slots.Add(new LogicSlot("body 2", 1, null, typeof(Rigidbody2D)));
            }
            #endif
        }
    }
    [Serializable]
    public abstract class JointComponentData : BaseComponent {
        public abstract AnchoredJoint2D Joint { get; }
    }
}