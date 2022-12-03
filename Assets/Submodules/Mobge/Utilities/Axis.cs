using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge{
    public struct Axis {
        public Vector3 up;
        public Vector3 right;
        public Vector3 forward;

        public Axis(Vector3 forward, Vector3 up) {
            this.forward = forward;
            this.up = up;
            // this.up = up;
            this.right = Vector3.Cross(forward, up);
        }

        public void Update(Vector3 forward) {
            // var normal = normal;
            this.forward = forward;
            var dirXZ = new Vector2(forward.x, forward.z);
            var sqrMagXZ = dirXZ.sqrMagnitude;
            if (sqrMagXZ == 0) {
                up = new Vector3(0, 0, -1);
                right = new Vector3(1, 0, 0);
            }
            else {
                var magXZ = Mathf.Sqrt(sqrMagXZ);
                dirXZ /= magXZ;
                up = new Vector3(-forward.y * dirXZ.x, magXZ, -forward.y * dirXZ.y);
                right = Vector3.Cross(forward, up);
            }
        }
        public void Update(Vector3 forward, Vector3 right) {
            this.forward = forward;
            this.right = right;
            // this.up = up;
            this.up = Vector3.Cross(right, forward);
        }
        public Vector3 WorldToLocal(Vector3 world) {
            return new Vector3 (
                Vector3.Dot(world, right),
                Vector3.Dot(world, up),
                Vector3.Dot(world, forward)
                );
        }
        public Vector3 LocalToWorld(Vector3 local) {
            return local.x * right + local.y * up + local.z * forward;
        }
        public Vector3 Convert(Vector2 offset) {
            return offset.x * right + offset.y * up;
        }
    }
}