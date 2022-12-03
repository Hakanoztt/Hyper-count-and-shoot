using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mobge.Animation;

namespace Mobge.Core.Components {
    public class BaseMovementModule<T> : ComponentDefinition<T> where T : BaseMovementModule<T>.BaseData
    {
        public static float ConvertToAngle(Quaternion q) {
            Vector2 p = q * new Vector2(1, 0);
            var a = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
            return a;
        }
        [Serializable]
        public abstract class BaseData : BaseComponent, IRotationOwner
        {
            public PoseCurve poseCurve = new PoseCurve() {
                duration = 2,
            };
            [SerializeField,HideInInspector] protected LogicConnections _connections;



            [HideInInspector, SerializeField] private Quaternion _rotation = Quaternion.identity;
            public Quaternion Rotation { get => _rotation; set => _rotation = value; }

            private float _cachedAngle;

            public override void Start(in InitArgs initData) {
                _cachedAngle = ConvertToAngle(_rotation);
            }

            public override LogicConnections Connections {
                get {
                    return _connections;
                }
                set {
                    _connections = value;
                }
            }

            

            public void EnsureData() {
                poseCurve.Ensure();
            }


            public Vector2 EvaluatePosition(float time, in Vector2 offset) {
                var p = poseCurve.EvaluatePosition(time) + offset;
                p = _rotation * p;
                p += (Vector2)position;
                return p;
            }
            protected void UpdatePose(in MovingObject obj) {
                obj.body.position = EvaluatePosition(obj.time, obj.offset);
                if (poseCurve.hasAngle) {
                    var r = EvaluateAngle(obj.time);
                    obj.body.rotation = r;
                }
            }
            public float EvaluateAngle(float time) {
                var p = poseCurve.EvaluateAngle(time);
                p = _cachedAngle + p;
                return p;
            }
        }

        
        [Serializable]
        public struct PoseCurve {
            public Curve x;
            public Curve y;
            public Curve angle;
            public bool hasAngle;
            public float duration;
            public void Ensure(bool forced = false)
            {
                if(!x.IsValid) {
                    x = Curve.New();
                    y = Curve.New();
                    angle = Curve.New();
                }
                x.EnsureInit(forced);
                y.EnsureInit(forced);
                angle.EnsureInit(forced);
            }

            public Vector2 EvaluatePosition(float time)
            {
                return new Vector2(x.Evaluate(time), y.Evaluate(time));
            }

            public float EvaluateAngle(float time)
            {
                return angle.Evaluate(time);
            }
        }
        
        public struct MovingObject {
            public float time;
            public Rigidbody2D body;
            public float velocity;
            public bool updated;
            public Vector2 offset;
        }
        public enum Mode {
            Normal = 0,
            Loop = 1,
            ContinueFromEnd = 2,
            ResetOnFinish = 3,
        }
    }
    

}