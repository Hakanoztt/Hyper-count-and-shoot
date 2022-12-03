using System.Collections.Generic;
using System;
using UnityEngine;

namespace Mobge.Core.Components {
    [Serializable]
    public struct Polygon {
        public Corner[] corners;
        public bool noCollider;
        public float skinScale;
        public bool openShape;
        public bool noFill;
        public float height;

        public Polygon(Corner[] corners, float skinScale = 1f, bool noCollider = false, bool openShape = false, bool noFill = false, float height = 0) {
            this.corners = corners;
            this.skinScale = skinScale;
            this.noFill = noFill;
            this.openShape = openShape;
            this.noCollider = noCollider;
            this.height = height;
        }
        /// <summary>
        /// Adds corner positions to the list.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="closedShape"></param>
        public void AddPositions2List(List<Vector2> list, bool closedShape = false) {
            for (int i = 0; i < corners.Length; i++) {
                list.Add(corners[i].position);
            }
            if (closedShape) {
                list.Add(corners[0].position);
            }
        }
        public Vector2[] ToVector2Array(bool closedShape = false) {
            Vector2[] points;
            if (closedShape) {
                //if (corners == null) {
                //    Debug.Log("Corners are null.");
                //}

                points = new Vector2[corners.Length + 1];
                points[corners.Length] = corners[0].position;
            }
            else {
                points = new Vector2[corners.Length];
            }

            for (int i = 0; i < corners.Length; i++) {
                points[i] = corners[i].position;
            }

            return points;
        }

        public static bool IsClockWise(in Polygon polygon) {
            var corners = polygon.corners;
            if (corners.Length <= 2) {
                return true;
            }
            float signedArea = 0f;
            for (int i = 0; i < corners.Length - 1; i++) {
                signedArea += corners[i].position.x * corners[i + 1].position.y;
                signedArea -= corners[i].position.y * corners[i + 1].position.x;
            }
            signedArea += corners[corners.Length - 1].position.x * corners[0].position.y;
            signedArea -= corners[corners.Length - 1].position.y * corners[0].position.x;

            // signedArea *= .5f;
            return signedArea <= 0f;
        }
        public static bool Contains(in Polygon polygon, Vector2 position) {
            int windingNumber = 0;
            for (int i = 0; i < polygon.corners.Length; i++) {
                var ls = polygon.corners[i].position;
                var le = polygon.corners[(i + 1) % (polygon.corners.Length)].position;
                if (ls.y > position.y && le.y < position.y) {
                    //inside, downward crossing
                    if (GeometryUtils.PointLineRightSide(ls, le, position)) {
                        windingNumber++;
                    }
                }else if (le.y > position.y && ls.y < position.y) {
                    //inside, upward crossing
                    if (!GeometryUtils.PointLineRightSide(ls, le, position)) {
                        windingNumber--;
                    }
                }
            }
            return windingNumber != 0;
        }
    }
    [Serializable]
    public struct Corner {
        public Vector2 position;
        public Corner(Vector2 position) {
            this.position = position;
        }

        public static Vector2[] CornerToVector2Array(in Corner[] corners) {
            var vectorArray = new Vector2[corners.Length];
            for (int i = 0; i < corners.Length; i++)
                vectorArray[i] = corners[i].position;
            return vectorArray;
        }
    }
}