using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public static class GeometryUtils {
        public struct Range {
            public float min, max;

            public Range(float min, float max) {
                this.min = min;
                this.max = max;
            }

            public float Clamp(float value) {
                return Mathf.Clamp(value, min, max);
            }
            public bool IsInside(float value) {
                return value >= min && value <= max;
            }
        }
        public static Vector2 NearestPointToPolygon(Vector2[] points, Vector2 point, bool closed = true) {
            return NearestPointToPolygon(points, point, closed, out int i1, out int i2);
        }
        public static Vector2 NearestPointToPolygon(Vector2[] points, Vector2 point, bool closed, out int index1, out int index2) {
            index1 = -1;
            index2 = -1;
            if (points.Length < 1) {
                return Vector2.zero;
            }
            

            int i;
            int prevI;
            Vector2 pos;
            if (closed) {
                i = 0;
                pos = points[prevI = points.Length - 1];
            }
            else {
                i = 1;
                pos = points[prevI = 0];
            }
            float ndisqr = float.PositiveInfinity;
            float mult = 0;
            Vector2 nearestP = Vector2.zero;
            for (; i < points.Length; i++) {
                var np = points[i];
                var nextp = NearestPointToLine(pos, np, point, out float rate);
                var dsqr = (point - nextp).sqrMagnitude;
                if (dsqr < ndisqr) {
                    index1 = prevI;
                    index2 = i;
                    mult = rate;
                    ndisqr = dsqr;
                    nearestP = nextp;
                }
                prevI = i;
                pos = np;
            }
            if (mult <= 0) {
                index2 = -1;
            }
            else if (mult >= 1) {
                index1 = index2;
                index2 = -1;
            }
            return nearestP;
        }
        public static Vector2 NearestPointToLine(Vector2 l1, Vector2 l2, Vector2 point, out float directionMultiplayer) {
            directionMultiplayer = PointToLineProjection(l1, l2, point);
            if(directionMultiplayer <= 0) {
                return l1;
            }
            if(directionMultiplayer >= 1) {
                return l2;
            }
            return l1 + (l2 - l1) * directionMultiplayer;
        }
        public static Vector3 NearestPointToLine(Vector3 l1, Vector3 l2, Vector3 point, out float directionMultiplayer) {
            directionMultiplayer = PointToLineProjection(l1, l2, point);
            if(directionMultiplayer <= 0) {
                return l1;
            }
            if(directionMultiplayer >= 1) {
                return l2;
            }
            return l1 + (l2 - l1) * directionMultiplayer;
        }
        public static float LineToPointDistanceSqr(Vector2 l1, Vector2 l2, Vector2 point, out float directionMultiplayer) {
            return (point - NearestPointToLine(l1, l2, point, out directionMultiplayer)).sqrMagnitude;
        }
        public static float LineToPointDistanceSqr(Vector3 l1, Vector3 l2, Vector3 point, out float directionMultiplayer) {
            return (point - NearestPointToLine(l1, l2, point, out directionMultiplayer)).sqrMagnitude;
        }
        public static float PointToLineProjection(Vector2 l1, Vector2 l2, Vector2 point) {
            var ldis = l2 - l1;
            var pdis = point - l1;
            var rate = Vector2.Dot(ldis, pdis) / ldis.sqrMagnitude;
            return rate;
        }
        public static float PointToLineProjection(Vector3 l1, Vector3 l2, Vector3 point) {
            var ldis = l2 - l1;
            var pdis = point - l1;
            var rate = Vector3.Dot(ldis, pdis) / ldis.sqrMagnitude;
            return rate;
        }
        public static bool PointLineRightSide(Vector2 ls, Vector2 le, Vector2 point) {
            return (le.x - ls.x) * (point.y - ls.y) - (le.y - ls.y) * (point.x - ls.x) > 0;
        }
        public static bool RayIntersectsRect(in Ray2D ray, in Rect rect, out Vector2 intersectionPoint) {
            return RayRectIntersectionHelper.RayIntersectsRect(ray, rect, out intersectionPoint);  
        }
        public static Vector2 RotateVectorByVector(Vector2 v1, Vector2 v2) {
            return new Vector2(v1.x * v2.x - v1.y * v2.y, v1.y * v2.x + v1.x * v2.y);
        }
        /// <summary>
        /// This method finds the intersection point in Line 1's space. The return value is the multiplayer of the Line 1 to the intersection point. The intersection point can be find with the following equation; l1s + (l1e - l1s) * returnValue.
        /// </summary>
        /// <param name="l1s">Start point of line 1.</param>
        /// <param name="l1e">End point of line 1.</param>
        /// <param name="l2s">Start point of line 2.</param>
        /// <param name="l2e">End point of line 2.</param>
        /// <returns></returns>
        public static float LineIntersectionRate(in Vector2 l1s, in Vector2 l1e, in Vector2 l2s, in Vector2 l2e) {
            var l2Dis = l2e - l2s;
            var l1s_l2sDis = l1s - l2s;
            var l1s_L2Rate = Vector2.Dot(l2Dis, l1s_l2sDis) / l2Dis.sqrMagnitude;
            var pOnL2 = l2s + l2Dis * l1s_L2Rate;
            var l2_L1s = pOnL2 - l1s;
            var l1Dis = l1e - l1s;
            // The following equation in comment is the correct solution. 
            // The return statement is identical to that line only it is simplified.
            // var result = l2_L1s.magnitude / (Vector2.Dot(l2_L1s, l1Dis) / l2_L1s.magnitude);
            return l2_L1s.sqrMagnitude / Vector2.Dot(l2_L1s, l1Dis);
        }

        public static Vector2 LineIntersectionPoint(in Vector2 l1s, in Vector2 l1e, in Vector2 l2s, in Vector2 l2e) {
            var rate = LineIntersectionRate(l1s, l1e, l2s, l2e);
            return l1s + (l1e - l1s) * rate;
        }
        
        /// <summary>
        /// maybe a,b,c triangle is defined in counter clock wise order???
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static bool IsInsideTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p) {
            float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
            float cCROSSap, bCROSScp, aCROSSbp;
    
            ax = c.x - b.x; ay = c.y - b.y;
            bx = a.x - c.x; by = a.y - c.y;
            cx = b.x - a.x; cy = b.y - a.y;
            apx = p.x - a.x; apy = p.y - a.y;
            bpx = p.x - b.x; bpy = p.y - b.y;
            cpx = p.x - c.x; cpy = p.y - c.y;
    
            aCROSSbp = ax * bpy - ay * bpx;
            cCROSSap = cx * apy - cy * apx;
            bCROSScp = bx * cpy - by * cpx;
    
            return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
        }
        /// <summary>
        /// A,B,C,D quad is defined in clockwise order
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static bool IsInsideQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Vector2 p) {
            return IsInsideTriangle(a, c, b, p) || IsInsideTriangle(a, d, c, p);
        }
        /// <summary>
        /// Returns signed area of the given polygon. Area is positive is points of the polygon given in clockwise direction (Reversed sign is returned when considering Shoelace formula).
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static float CalculateArea(Vector2[] polygon, int start, int count) {
            int end = start + count;
            // This implementation is implemented based on Shoelace formula. Only the result is negative.
            int prevI = end - 1;
            Vector2 prev = polygon[prevI];
            float area = 0;
            for(int i = start; i < end; i++) {
                Vector2 point = polygon[i];

                area += point.x * prev.y - prev.x * point.y;

                prev = point;
            }
            return area * 0.5f;
        }
        public static Vector2 CalculateCenterOfMass(Vector2[] polygon, int start, int count) {
            int end = start + count;
            int prevI = end - 1;
            Vector2 prev = polygon[prevI];
            Vector2 center = Vector2.zero;
            for (int i = start; i < end; i++) {
                Vector2 point = polygon[i];

                center.x += (prev.x + point.x) * (point.x * prev.y - prev.x * point.y);
                center.y += (prev.y + point.y) * (point.x * prev.y - prev.x * point.y);

                prev = point;
            }
            return center * ((1f / 6f) / CalculateArea(polygon, start, count));
        }

        public static void FindAxises(Vector3 normal, out Vector3 a1, out Vector3 a2) {
            var up = Vector3.up;
            var cross = Vector3.Cross(normal, up);
            if (cross.sqrMagnitude < 0.001f) {
                up = Vector3.right;
                cross = Vector3.Cross(normal, up);
            }
            a1 = cross;
            a2 = Vector3.Cross(normal, a1);
        }


        private struct RayRectIntersectionHelper {
            private float _nearestSquare;

            public static bool RayIntersectsRect(in Ray2D ray, in Rect rect, out Vector2 intersectionPoint) {
                RayRectIntersectionHelper h;
                h._nearestSquare = float.PositiveInfinity;
                intersectionPoint = Vector2.zero;
                Vector2 ip;
                var xRange = new Range(rect.xMin, rect.xMax);
                var yRange = new Range(rect.yMin, rect.yMax);
                bool found = false;

                if (h.HandleSegment(ray.origin, ray.direction, yRange, xRange.min, out ip)) {
                    intersectionPoint = ip;
                    found = true;
                }
                if (h.HandleSegment(ray.origin, ray.direction, yRange, xRange.max, out ip)) {
                    intersectionPoint = ip;
                    found = true;
                }
                var rOr = Reverse(ray.origin);
                var rDir = Reverse(ray.direction);

                if (h.HandleSegment(rOr, rDir, xRange, yRange.min, out ip)) {
                    intersectionPoint = Reverse(ip);
                    found = true;
                }
                if (h.HandleSegment(rOr, rDir, xRange, yRange.max, out ip)) {
                    intersectionPoint = Reverse(ip);
                    found = true;
                }
                return found;
            }
            private static Vector2 Reverse(Vector2 v) {
                return new Vector2(v.y, v.x);
            }
            public bool HandleSegment(in Vector2 origin, in Vector2 direction, in Range segmentY, float xValue, out Vector2 intersectionPoint) {
                float rate = (xValue - origin.x) / direction.x;
                intersectionPoint = origin + direction * rate;
                if (rate < 0 || float.IsInfinity(rate)) { 
                    return false;
                }
                if (!segmentY.IsInside(intersectionPoint.y)) {
                    return false;
                }
                var dSqr = (intersectionPoint - origin).sqrMagnitude;
                if(dSqr < _nearestSquare) {
                    _nearestSquare = dSqr;
                    return true;
                }
                return false;
            }
        }
    }
}