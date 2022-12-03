using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR

namespace Mobge {
    [CreateAssetMenu(menuName = "Mobge/Custom Grid")]
    public class EditorGrid : ScriptableObject {
        [HideInInspector] public float[] dimensions = new[] { 1f };
        public GridType gridType = GridType.Square;
        public Plane plane = Plane.XY;
        public Color color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        public bool autoSnap = true;
        private static float s_eqlTriHeight = Mathf.Sqrt(3) * 0.5f;
        public Vector3 Snap(Vector3 point) {
            var p2 = ToGridPlane(point);
            switch (gridType) {
                case GridType.Square:
                    p2 = new Vector2(Round(p2.x, dimensions[0]), Round(p2.y, dimensions[0]));
                    break;
                case GridType.Rectangle:
                    p2 = new Vector2(Round(p2.x, dimensions[0]), Round(p2.y, dimensions[1]));
                    break;
                case GridType.Triangle:
                    p2 = TriangleSnapLocal(p2);
                    break;
                case GridType.Radial:
                    p2 = RadialSnap(p2);
                    break;
                case GridType.None:
                default:
                    break;

            }
            point = FromGridPlane(point, p2);
            return point;
        }
        private Vector3 RadialSnap(Vector3 point) {
            float radiusStep = dimensions[0];
            float angleCount = dimensions[1];
            float angleStep = Mathf.PI * 2f / angleCount;
            float distance = point.magnitude;
            float angle = Mathf.Atan2(point.y, point.x);
            angle = Round(angle, angleStep);
            distance = Round(distance, radiusStep);
            return new Vector3(distance * Mathf.Cos(angle), distance * Mathf.Sin(angle));
        }
        public Vector3 TriangleSnapLocal(Vector3 point) {
            float xLength = dimensions[0] * 0.5f;
            float yLength = dimensions[0] * s_eqlTriHeight;
            int ix = Mathf.FloorToInt(point.x / xLength);
            int iy = Mathf.FloorToInt(point.y / yLength);
            Vector2 p1, p2;
            if ((ix + iy) % 2 == 0) {
                p1 = new Vector2(ix * xLength, iy * yLength);
                p2 = p1 + new Vector2(xLength, yLength);
            }
            else {
                p1 = new Vector2(ix * xLength, iy * yLength);
                p2 = p1;
                p1.x += xLength;
                p2.y += yLength;
            }
            Vector2 point2 = point;
            if ((p1 - point2).sqrMagnitude < (p2 - point2).sqrMagnitude) {
                return new Vector3(p1.x, p1.y, point.z);
            }
            else {
                return new Vector3(p2.x, p2.y, point.x);
            }
        }
        
        private float Round(float value, float length) {
            float val = Mathf.Round(value / length);
            return val * length;
        }

        public enum GridType {
            Square = 0,
            Rectangle = 1,
            Triangle = 2,
            Radial = 4,
            None = 1024,
        }

        public Vector2 ToGridPlane(Vector3 point) {
            switch (plane) {
                default:
                case Plane.XY:
                    return new Vector2(point.x, point.y);
                case Plane.XZ:
                    return new Vector2(point.x, point.z);
                case Plane.YZ:
                    return new Vector2(point.z, point.y);
            }
        }
        public Vector3 FromGridPlane(Vector3 original, Vector2 snapped) {
            switch (plane) {
                default:
                case Plane.XY:
                    return new Vector3(snapped.x, snapped.y, original.z);
                case Plane.XZ:
                    return new Vector3(snapped.x, original.y, snapped.y);
                case Plane.YZ:
                    return new Vector3(original.x, snapped.y, snapped.x);
            }
        }
        public enum Plane {
            XY = 0,
            XZ = 1,
            YZ = 2,
        }
    }
}

#endif