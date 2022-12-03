using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    [Serializable]
    public struct Collider2DShape
    {
        public Shape shape;
        public Vector2[] points;
        public Vector2 Offset {
            get => points[0];
            set => points[0] = value;
        }
        public float Radius {
            get => points[1].x;
            set => points[1].x = value;
        }
        public Vector2 Size {
            get => points[1];
            set => points[1] = value;
        }
        public void EnsureData() {
            int size;
            switch(shape) {
                case Shape.Rectangle:
                size = 2;
                break;
                case Shape.Circle:
                case Shape.Capsule:
                    size = 2;
                break;
                case Shape.Polygon:
                if(points == null) {
                    size = 3;
                }
                else {
                    size = Mathf.Max(3, points.Length);
                }
                break;
                default:
                case Shape.None:
                    size = 0;
                break;
            }
            if(points == null) {
                points = new Vector2[size];
            }
            else if(points.Length != size) {
                Array.Resize(ref points, size);
            }
        }
        public enum Shape {
            Polygon = 0,
            Rectangle = 1,
            Circle = 2,
            None = 3,
            Capsule = 4,
        }
        public Collider2D AddCollider(GameObject go) {
            switch(shape) {
                default:
                case Shape.Polygon:
                var pc = go.AddComponent<PolygonCollider2D>();
                pc.points = points;
                return pc;
                case Shape.Circle:
                var cc = go.AddComponent<CircleCollider2D>();
                cc.offset = Offset;
                cc.radius = Radius;
                return cc;
                case Shape.Rectangle:
                var bc = go.AddComponent<BoxCollider2D>();
                bc.offset = Offset;
                bc.size = Size;
                return bc;
                case Shape.Capsule:
                    var ccc = go.AddComponent<CapsuleCollider2D>();
                    ccc.offset = Offset;
                    ccc.size = Size;
                    return ccc;
                case Shape.None:
                return null;
            }
        }

        public Bounds CalculateBounds() {
            switch (shape) {
                default:
                case Shape.None:
                    return new Bounds();
                case Shape.Polygon:
                    Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                    Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                    for(int i = 0; i < points.Length; i++) {
                        var p = points[i];
                        max = Vector2.Max(max, p);
                        min = Vector2.Min(min, p);
                        
                    }
                    return new Bounds((max + min) * 0.5f, (max - min));
                case Shape.Rectangle:
                case Shape.Capsule:
                    return new Bounds(Offset, Size);
                case Shape.Circle:
                    var r = Radius * 2;
                    return new Bounds(Offset, new Vector3(r, r));
            }
        }
    }
}