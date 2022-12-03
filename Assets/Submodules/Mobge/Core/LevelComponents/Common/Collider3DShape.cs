using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace Mobge.Core.Components {
    [Serializable]
    public struct Collider3DShape {
        public Shape shape;
        public Vector3[] points;

        public Vector3 Offset {
            get => points[0];
            set => points[0] = value;
        }
        public float Radius {
            get => points[1].x;
            set => points[1].x = value;
        }
        public Vector3 Size {
            get => points[1];
            set => points[1] = value;
        }

        public void EnsureData() {
            int size;
            switch (shape) {
                case Shape.Box:
                    size = 2;
                    break;
                case Shape.Sphere:
                    size = 2;
                    break;
                default:
                case Shape.None:
                    size = 0;
                    break;
            }
            if (points == null) {
                points = new Vector3[size];
            } else if (points.Length != size) {
                Array.Resize(ref points, size);
            }
        }

        public enum Shape {
            Box,
            Sphere,
            None
        }

        public Collider AddCollider(GameObject go) {
            switch (shape) {
                default:
                case Shape.Box:
                    var bc = go.AddComponent<BoxCollider>();
                    bc.center = Offset;
                    bc.size = Size;
                    return bc;
                case Shape.Sphere:
                    var cc = go.AddComponent<SphereCollider>();
                    cc.center = Offset;
                    cc.radius = Radius;
                    return cc;
                case Shape.None:
                    return null;
            }
        }
        public Bounds CalculateBounds() {
            switch (shape) {
                default:
                case Shape.None:
                    return new Bounds();
                case Shape.Box:
                    return new Bounds(Offset, Size);
                case Shape.Sphere:
                    return new Bounds(Offset, Size);
            }
        }
    }
}