using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Character3D {
    [RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))]
    public class CharacterBody : MonoBehaviour {
        public float maxSlope;
        public LayerMask groundLayers = 1 << 0;
        public float groundCheckDistance = 0.2f;

        private Rigidbody _rigidbody;
        //private CollisionFlags _collisionFlags;
        //private CollisionFlags _collisionFlagsPrev;

        private float _minSlopeY;
        private bool _onGround;

        private Vector3 _groundNormal = Vector3.up;
        private Vector2 _groundPoint;
        public Vector3 GroundNormal => _groundNormal;
        public Vector3 GroundPoint => _groundPoint;

        //public CollisionFlags collisionFlags => _collisionFlags | _collisionFlagsPrev;
        public Rigidbody Rigidbody => _rigidbody;

        public bool OnGround => _onGround;

        protected void Awake() {
            _rigidbody = GetComponent<Rigidbody>();
            _minSlopeY = Mathf.Sin(maxSlope * Mathf.Deg2Rad);
        }

        public void MoveTo(in Pose worldPose) {
            MoveTo(worldPose.position);
            if (_rigidbody.isKinematic) {
                _rigidbody.MoveRotation(worldPose.rotation);
            }
            else {
                RotateTo(worldPose.rotation);
            }
        }
        public void RotateTo(Quaternion target) {
            float dt = Time.fixedDeltaTime;
            if (PhysicsExtensions.TryCalculateRequiredAngularVelocity(_rigidbody.rotation, target, Time.fixedDeltaTime, out Vector3 velocity)) {
                _rigidbody.angularVelocity = velocity;
            }
            else {
                _rigidbody.rotation = target;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }
        public void MoveTo(Vector3 position) {
            if (_rigidbody.isKinematic) {
                _rigidbody.MovePosition(position);
            }
            else {
                _rigidbody.velocity = (position - _rigidbody.position) / Time.fixedDeltaTime;
            }
        }

        public void MoveTo(Vector3 position, float maxForce, Vector3 up) {
            float dt = Time.fixedDeltaTime;
            var requiredVelocity = (position - _rigidbody.position) / dt;
            var requiredForce = requiredVelocity - _rigidbody.velocity;
            requiredForce /= dt;
            requiredForce -= Vector3.Dot(requiredForce, up) * up;
            _rigidbody.AddForce(Vector3.ClampMagnitude(requiredForce, maxForce));
        }
        public void RotateTo(Quaternion target, float maxTorque) {
            float dt = Time.fixedDeltaTime;
            if (PhysicsExtensions.TryCalculateRequiredAngularVelocity(_rigidbody.rotation, target, dt, out Vector3 velocity)) {
                velocity -= _rigidbody.angularVelocity;
                var torque = velocity / dt;

                _rigidbody.AddRelativeTorque(Vector3.ClampMagnitude(torque, maxTorque));
                //Debug.Log("torque: " + torque);
            }
            else {

            }
        }

        protected void FixedUpdate() {
            float distance = groundCheckDistance;
            float vely = Rigidbody.velocity.y;
            if (vely < 0) {
                distance -= vely * Time.fixedDeltaTime;
            }
            _onGround = false;
            if(Physics.Raycast(new Ray(Rigidbody.position, Vector3.down), out var hit, distance, groundLayers, QueryTriggerInteraction.Ignore)) {
                _onGround = true;
                _groundNormal = hit.normal;
                _groundPoint = hit.point;

            }
            if (Rigidbody.IsSleeping()) {
                enabled = false;
            }
        }
    }
}