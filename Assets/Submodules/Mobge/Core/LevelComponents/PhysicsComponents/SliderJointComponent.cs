using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class SliderJointComponent : ComponentDefinition<SliderJointComponent.Data>
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
            public float motorForce;
            public float angle;
            public float maxLimit;
            public float minLimit;
            private SliderJoint2D _joint;
            public override AnchoredJoint2D Joint => _joint;
            public override LogicConnections Connections {
                get {
                    return _connections;
                }
                set {
                    _connections = value;
                }
            }

            public bool UseLimits { get => maxLimit - minLimit > 0;}

            public override void Start(in InitArgs initData)
            {
                _initArgs = initData;
                initData.player.FixedActionManager.DoTimedAction(0, null, InitJoint);
            }
            private void InitJoint(object data, bool success) {
                if(success) {
                    // This body is obligatory so there is no need for type checking. Program should crash if this is null
                    Rigidbody2D body1 = (Rigidbody2D)_connections.InvokeSimple(this, 0, null, _initArgs.components);
                    // This body is optional so we should cast it safely
                    Rigidbody2D body2 = _connections.InvokeSimple(this, 1, null, _initArgs.components) as Rigidbody2D;
                    _joint = body1.gameObject.AddComponent<SliderJoint2D>();
                    _joint.enableCollision = collideConnected;
                    _joint.anchor = body1.transform.InverseTransformPoint(position);
                    _joint.connectedBody = body2;
                    _joint.autoConfigureAngle = false;
                    _joint.angle = angle;
                    _joint.useLimits = UseLimits;
                    _joint.autoConfigureConnectedAnchor = false;
                    _joint.connectedAnchor = body2.transform.InverseTransformPoint(position);
                    if(_joint.useLimits) {
                        JointTranslationLimits2D lim = new JointTranslationLimits2D();
                        lim.min = minLimit;
                        lim.max = maxLimit;
                        _joint.limits = lim;
                    }
                    if(useMotor) {
                        _joint.useMotor = true;
                        JointMotor2D motor = new JointMotor2D();
                        motor.maxMotorTorque = motorForce;
                        motor.motorSpeed = motorSpeed;
                        _joint.motor = motor;
                    }
                }
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                var motor = _joint.motor;
                switch (index) {
                    case 10: motor.motorSpeed = (float)input; break;
                    case 11: motor.maxMotorTorque = (float)input; break;
                }
                _joint.motor = motor;
                return null;
            }
#if UNITY_EDITOR
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("body 1", 0, null, typeof(Rigidbody2D)));
                slots.Add(new LogicSlot("body 2", 1, null, typeof(Rigidbody2D)));
            }
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("set motor speed", 10, typeof(float)));
                slots.Add(new LogicSlot("set motor torque", 11, typeof(float)));
            }
#endif
        }
    }
}