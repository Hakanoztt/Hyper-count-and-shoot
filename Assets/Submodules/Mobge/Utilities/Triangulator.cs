using UnityEngine;
using System.Collections.Generic;
using System;

namespace Mobge {
    public class Triangulator
    {
        private static Triangulator _instance;
        public static Triangulator Instance {
            get {
                if (_instance == null) {
                    _instance = new Triangulator();
                }
                return _instance;
            }
        }
        private TriangulatorWithMidPoints _midPointTriangulator;
        /// <summary>
        /// Instantiates <see cref="Triangulator"/> class. Use <see cref="Instance"/> unless you need to use this class inside multiple threads.
        /// </summary>
        /// <returns></returns>
        public static Triangulator New() {
            return new Triangulator();
        }
        private ExposedList<Vector2> m_points = new ExposedList<Vector2>();
        private ExposedList<Vector2> m_extraPoints = new ExposedList<Vector2>();
        private ExposedList<ExtraEdgeDetails> m_extraEdges = new ExposedList<ExtraEdgeDetails>();
        private ExposedList<int> m_linkedList = new ExposedList<int>();
        private ExposedList<int> m_indices = new ExposedList<int>();
        private ExposedList<int> m_temp = new ExposedList<int>();
        private Triangulator () {
        }
        public ExposedList<Vector2> Points => m_points;
        public ExposedList<Vector2> ExtraPoints => m_extraPoints;
        public ExposedList<ExtraEdgeDetails> ExtraEdges => m_extraEdges;
        public ExposedList<int> Triangulate(Vector2[] values) {
            
            var temp = m_points.array;
            m_points.SetArray(values, values.Length);
            TriangulateDirect(Mode.Simple);
            m_points.ClearFast();
            m_points.SetArray(temp, 0);
            return m_indices;
        }
        /// <summary>
        /// Uses property <see cref="Points"/> as point values.
        /// </summary>
        public ExposedList<int> Triangulate(Mode mode = Mode.Simple) {
            TriangulateDirect(mode);
            m_points.ClearFast();
            return m_indices;
        }
        private void TriangulateDirect(Mode mode) {
            m_indices.ClearFast();
            m_extraPoints.ClearFast();
            m_extraEdges.ClearFast();


            int n = m_points.Count;
            if (n < 3)
                return;
            switch (mode) {
                default:
                case Mode.Simple:
                    TriangulateBasic();
                    break;
                case Mode.GenerateMidpoints:
                    _midPointTriangulator.Triangulate(this, false);
                    break;
                case Mode.GenerateConnectedMidpoints:
                    _midPointTriangulator.Triangulate(this, true);
                    break;
            }

            return;
        }
        public static bool IsConcav(in Vector2 d1_2, in Vector2 d3_2) {
            return d1_2.y * d3_2.x > d1_2.x * d3_2.y;
        }
        private void TriangulateBasic() {

            int n = m_points.Count;
            m_temp.SetCountFast(n);
            var V = m_temp.array;
            if (Area() > 0) {
                for (int v = 0; v < n; v++)
                    V[v] = v;
            }
            else {
                for (int v = 0; v < n; v++)
                    V[v] = (n - 1) - v;
            }

            int nv = n;
            int count = 2 * nv;
            for (int v = nv - 1; nv > 2;) {
                if ((count--) <= 0)
                    return;

                int u = v;
                if (nv <= u)
                    u = 0;
                v = u + 1;
                if (nv <= v)
                    v = 0;
                int w = v + 1;
                if (nv <= w)
                    w = 0;

                if (Snip(u, v, w, nv, V)) {
                    int a, b, c, s, t;
                    a = V[u];
                    b = V[v];
                    c = V[w];
                    m_indices.Add(a);
                    m_indices.Add(b);
                    m_indices.Add(c);
                    for (s = v, t = v + 1; t < nv; s++, t++)
                        V[s] = V[t];
                    nv--;
                    count = 2 * nv;
                }
            }
            m_indices.Reverse();
        }
        private void InitLinkListFromPoints() {
            m_linkedList.SetCountFast(Points.Count);
            int countMinus1 = m_linkedList.Count - 1;
            var marr = m_linkedList.array;
            for(int i = 0;i < countMinus1; i++) {
                marr[i] = i + 1;
            }
            marr[countMinus1] = 0;
        }
    
        private float Area () {
            int n = m_points.Count;
            float A = 0.0f;
            var points = m_points.array;
            for (int p = n - 1, q = 0; q < n; p = q++) {
                Vector2 pval = points[p];
                Vector2 qval = points[q];
                A += pval.x * qval.y - qval.x * pval.y;
            }
            return (A * 0.5f);
        }
    
        private bool Snip (int u, int v, int w, int n, int[] V) {
            int p;
            var points = m_points.array;
            Vector2 A = points[V[u]];
            Vector2 B = points[V[v]];
            Vector2 C = points[V[w]];
            if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
                return false;
            for (p = 0; p < n; p++) {
                if ((p == u) || (p == v) || (p == w))
                    continue;
                Vector2 P = points[V[p]];
                if (GeometryUtils.IsInsideTriangle(A, B, C, P))
                    return false;
            }
            return true;
        }
        public enum Mode
        {
            Simple,
            GenerateMidpoints,
            GenerateConnectedMidpoints,
        }
        private struct TriangulatorWithMidPoints
        {
            private Vector2 d1_2, d3_2;
            private ExposedList<int> links;
            private ExposedList<Vector2> points;
            private ExposedList<ExtraEdgeDetails> extraEdges;
            private ExposedList<int> indices;
            private bool connectMidPoints;
            private Vector2[] pts;
            private int[] ll;
            private int initialCount;

            public void Triangulate(Triangulator t, bool connectMidPoints) {
                t.InitLinkListFromPoints();
                links = t.m_linkedList;
                ll = links.array;
                points = t.m_points;
                extraEdges = t.m_extraEdges;
                pts = points.array;
                indices = t.m_indices;
                this.connectMidPoints = connectMidPoints;

                initialCount = points.Count;

                TriangulateRecursive(0, 0);
                var newCount = points.Count;
                for(int i = initialCount; i < newCount; i++) {
                    t.m_extraPoints.Add(pts[i]);
                }
            }
#if UNITY_EDITOR
            private void DumpPoints() {
                var a = pts;
                Array.Resize(ref a, this.points.Count);
                var ptspts = Mobge.Serialization.BinarySerializer.Instance.Serialize(a.GetType(), a);
                var path = @"C:\Users\ferha\Downloads\temp";
                System.IO.File.WriteAllBytes(path, ptspts.data);
            }
            private int DebugCountLoop(int index) {
                int i = index;
                int count = 0;
                do {
                    count++;
                    i = this.links.array[i];
                } while (index != i && count < 100);
                return count;
            }
            static bool stop = false;
#endif
            private void TriangulateRecursive(int listHead, int depth) {
                if(depth == 30) {
                    return;
                }
                int i2 = ll[listHead];
                int i3 = ll[i2];
                int startI2 = i2;
                Vector2 p1, p2, p3;
                p1 = pts[listHead];
                p2 = pts[i2];
                p3 = pts[i3];
                do { //We use do-while for link lists because there is no link list with 0 elements.
#if UNITY_EDITOR
                    if (stop) {
                        return;
                    }
#endif
                    d1_2 = p1 - p2;
                    d3_2 = p3 - p2;
                    if (IsConcav(d1_2, d3_2)) {
                        var bisector2 = CalculateBisector();
                        if (bisector2.sqrMagnitude >= 0.00001f) {
                            int iAcross = FindPointAccross(i2, p2, p3, bisector2);
                            if (iAcross >= 0) {
                                if (connectMidPoints) {
                                    int middleIndex = points.Count;
                                    points.Add((pts[iAcross] + pts[i2]) * 0.5f);
                                    pts = points.array;
                                    links.Add(i2);
                                    ll = links.array;


                                    // lets prepare 2 loops for triangulate polygon further.
                                    // prepare first loop;
                                    int oldAcross = ll[iAcross];
                                    ll[iAcross] = middleIndex;
                                    TriangulateRecursive(i2, depth + 1);
                                    // now prepare second loop;
                                    ll[iAcross] = oldAcross;
                                    ll[i2] = middleIndex;
                                    ll[middleIndex] = iAcross;
                                    TriangulateRecursive(i2, depth + 1);
                                    ll[i2] = i3;
                                }
                                else {
                                    // lets prepare 2 loops for triangulate polygon further.
                                    // prepare first loop;
                                    int oldAcross = ll[iAcross];
                                    ll[iAcross] = i2;
                                    TriangulateRecursive(i2, depth + 1);
                                    // now prepare second loop;
                                    ll[iAcross] = oldAcross;
                                    ll[i2] = iAcross;
                                    TriangulateRecursive(i2, depth + 1);
                                    ll[i2] = i3;
                                }
                                // we let recursive methods handle triangulation, we dont need to do any triangulation here, so we can finish execution of this method.
                                return;
                            }
                        }
                    }
                    i2 = i3;
                    i3 = ll[i3];
                    if (i2 == startI2) {
                        break;
                    }
                    p1 = p2;
                    p2 = p3;
                    p3 = pts[i3];

                }
                while (true);

                { // triangulate convex with center point
                    // Since we did not find any concav point, we can triangulate the polygon.
                    //Vector2 middlePoint = CalculateCenterOfMass(i2);
                    //if (!float.IsNaN(middlePoint.x)) {

                    //    int middleIndex = points.Count;

                    //    int iStart = i2;
                    //    //string s = iStart + " " + i2 + " ";
                    //    do {
                    //        //  s += i3 + " ";
                    //        indices.Add(middleIndex);
                    //        indices.Add(i2);
                    //        indices.Add(i3);
                    //        i2 = i3;
                    //        i3 = ll[i3];
                    //    } while (i2 != iStart);

                    //    points.Add(middlePoint);
                    //    pts = points.array;
                    //    links.Add(-1);
                    //    ll = links.array;

                    //}


                    //Debug.Log(s);
                }
                { // triangulate convex with center point
                    // Since we did not find any concav point, we can triangulate the polygon.
                    int middleIndex = points.Count;
                    Vector2 middlePoint = Vector2.zero;

                    int iStart = i2;
                    int count = 0;
                    //string s = iStart + " " + i2 + " ";
                    do {
                        //  s += i3 + " ";
                        indices.Add(middleIndex);
                        indices.Add(i2);
                        indices.Add(i3);
                        i2 = i3;
                        i3 = ll[i3];
                        count++;
                        middlePoint += pts[i2];
                    } while (i2 != iStart);

                    points.Add(middlePoint / count);
                    pts = points.array;
                    links.Add(-1);
                    ll = links.array;
                    //Debug.Log(s);
                }
            }
            private float CalculateArea(int start) {
                int current = ll[start];
                int stop = current;

                Vector2 prev = pts[start];
                float area = 0;
                do {

                    Vector2 point = pts[current];
                    area += point.x * prev.y - prev.x * point.y;

                    prev = point;
                    current = ll[current];


                } while (current != stop);
                return area * 0.5f;
            }
            private Vector2 CalculateCenterOfMass(int start) {
                int current = ll[start];
                int stop = current;
                Vector2 prev = pts[start];
                Vector2 center = Vector2.zero;
                do {
                    Vector2 point = pts[current];

                    center.x += (prev.x + point.x) * (point.x * prev.y - prev.x * point.y);
                    center.y += (prev.y + point.y) * (point.x * prev.y - prev.x * point.y);
                    prev = point;
                    current = ll[current];
                } while (current != stop);
                var area = CalculateArea(start);
                
                return center * ((1f / 6f) / area);
            }
            private Vector2 CalculateBisector() {
                float ms1 = d1_2.sqrMagnitude;
                float ms2 = d3_2.sqrMagnitude;
                var dmid = d1_2 * Mathf.Sqrt(ms2 / ms1);
                return -dmid - d3_2;
            }
            private int FindPointAccross(int i2, in Vector2 tp2, in Vector2 tp3, in Vector2 bisector2) {
                int index1 = ll[i2];
                int index2 = ll[index1];
                // lines that we are searcing must not be adjacent to the current point, 
                // because the bisector cannot intersect thoose adjacent lines.
                Vector2 p1 = tp3;
                Vector2 p2 = pts[index2];
                float minDistanceSqr = float.PositiveInfinity;
                var bisectorTip = tp2 + bisector2;
                Vector2 point = Vector2.zero;
                ExtraEdgeDetails dt = new ExtraEdgeDetails();
                dt.p1 = -1;
                do {
                    var rate = GeometryUtils.LineIntersectionRate(p1, p2, tp2, bisectorTip);
                    if(0 <= rate && 1 >= rate) {
                        var iP = p1 + (p2 - p1) * rate;
                        var dif = iP - tp2;
                        var rel = Vector2.Dot(bisector2, dif);
                        if (rel >= 0) {
                            var sqrMag = dif.sqrMagnitude;
                            if (sqrMag < minDistanceSqr) {
                                minDistanceSqr = sqrMag;
                                dt.p1 = index1;
                                point = iP;
                                dt.p2 = index2;
                                dt.lerp = rate;
                            }
                        }
                    }
                    index1 = index2;
                    index2 = ll[index2];
                    if (index2 == i2) { // we stop when our line is adjacent to starting point
                        break;
                    }
                    p1 = p2;
                    p2 = pts[index2];
                }
                while (true);
                if (dt.p1 == -1) {
                    return -1;
                }
                int ret = points.Count;
                dt.index = ret;
                extraEdges.Add(dt);
                points.Add(point);
                pts = points.array;
                links.Add(ll[dt.p1]);
                ll = links.array;
                ll[dt.p1] = ret;
                return ret;
                //p1 = pts[minIndex];
                //int minIndex2 = ll[minIndex];
                //p2 = pts[minIndex2];
                //// pick the point that is further
                //if(GeometryUtils.PointToLineProjection(tp2, bisectorTip, p1) > GeometryUtils.PointToLineProjection(tp2, bisectorTip, p2)) {
                //    return minIndex;
                //}
                //else {
                //    return minIndex2;
                //}
            }
        }

        public struct ExtraEdgeDetails
        {
            public int index;
            public int p1, p2;
            public float lerp;
        }
    }
}