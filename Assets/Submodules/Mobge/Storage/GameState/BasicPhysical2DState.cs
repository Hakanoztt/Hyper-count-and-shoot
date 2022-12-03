using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Serialization { 
    public struct BasicPhysical2DState
    {
        public Vector2 velocity, position;
        public float angularVelocity;
        public Quaternion rotation;
        public RigidbodyType2D type;
        public BasicPhysical2DState(Rigidbody2D rb) {
            var tr = rb.transform;
            type = rb.bodyType;
            position = tr.localPosition;
            rotation = tr.localRotation;
            velocity = rb.velocity;
            angularVelocity = rb.angularVelocity;
        }
        public void ApplyState(Rigidbody2D rb) {
            var tr = rb.transform;
            rb.bodyType = type;
            tr.localPosition = position;
            tr.localRotation = rotation;
            rb.velocity = velocity;
            rb.angularVelocity = angularVelocity;
        }
    }
}