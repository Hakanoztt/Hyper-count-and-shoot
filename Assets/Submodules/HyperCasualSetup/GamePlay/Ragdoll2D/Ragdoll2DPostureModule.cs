using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.GamePlay
{
    public class Ragdoll2DPostureModule : MonoBehaviour
    {
        [OwnComponent(true), SerializeField] private Ragdoll2D _ragdoll;
        public float maxMuscleForce;
        ExposedList<Joint> _joints;
        private struct Joint
        {
            public HingeJoint2D joint;
            public Rigidbody2D body;
            public TargetPositionMotor motor;

            public Joint(HingeJoint2D joint, Rigidbody2D body, float force)
            {
                this.joint = joint;
                this.body = body;
                motor = new TargetPositionMotor(force);
            }
        }

        protected void Awake()
        {
            if (_ragdoll != null)
            {
                InitializeJoints();
            }
        }

        private void InitializeJoints()
        {
            if (_joints == null)
            {
                _joints = new ExposedList<Joint>();
            }
            else
            {
                _joints.Clear();
            }
            for(int i  = 0; i < _ragdoll.bones.Length; i++)
            {
                var b = _ragdoll.bones[i];
                var j = b.body.GetComponent<HingeJoint2D>();
                if (j)
                {
                    _joints.Add(new Joint(j, b.body, maxMuscleForce));
                }
            }
        }

        public Ragdoll2D Ragdoll {
            get => _ragdoll;
            set {
                if(_ragdoll != value)
                {
                    _ragdoll = value;
                    if (_ragdoll)
                    {
                        InitializeJoints();
                    }
                    else
                    {
                        _joints.Clear();
                    }
                }
            }
        }
        protected void FixedUpdate()
        {
            if (_joints != null)
            {
                for (int i = 0; i < _joints.Count; i++)
                {
                    var j = _joints.array[i];
                    var lims = j.joint.limits;
                    var targetRotation = j.joint.connectedBody.rotation - (lims.min + lims.max) * 0.5f;
                    var requiredRot = targetRotation - j.body.rotation;
                    //j.joint.useLimits = false;
                    //j.body.gravityScale = 0;
                    if(j.motor.CalculateForce(requiredRot * Mathf.Deg2Rad,j.body.angularVelocity*Mathf.Deg2Rad,j.body.inertia, out float torque))
                    {
                        j.body.AddTorque(torque);
                    }
                }
            }
        }
        public static float CalculateForce(float requiredVelocity, float maxForce, float mass)
        {
            var reqForce = requiredVelocity * mass / Time.fixedDeltaTime;
            reqForce = Mathf.Clamp(reqForce, -maxForce, maxForce);
            return reqForce;
        }
        public struct TargetPositionMotor
        {
            public float maxForce;
            private float _intendedVelocity;

            public TargetPositionMotor(float maxForce)
            {
                this.maxForce = maxForce;
                _intendedVelocity = 0;

            }

            public bool CalculateForce(float targetDistance, float velocity, float mass, out float force)
            {
                float dt = Time.fixedDeltaTime;
                float idt = 1f / dt;
                var reqVel = targetDistance * idt + _intendedVelocity - velocity;

                force = Ragdoll2DPostureModule.CalculateForce(reqVel, maxForce, mass);
                _intendedVelocity = velocity + force * dt;
                return true;
            }
            public void Reset(float target = 0)
            {
                _intendedVelocity = target;
            }


        }
    }
}