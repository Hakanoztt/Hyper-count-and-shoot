using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    [CreateAssetMenu(menuName = "Mobge/Level Components/Basic Polygon Visualizer")]
    public class BasicPolygonVisualizer : ScriptableObject, IPolygonVisualizer
	{
        public Material material;
        private MeshBuilder _builder;
        public float uvScale = 1;
        public float uvAngle = 0;
        public NormalBender edgeNormals;
        public Mobge.Triangulator.Mode triangulationMode = Triangulator.Mode.Simple;
        private ExposedList<Corner> _corners = new ExposedList<Corner>();
        protected void OnEnable() {
            _builder = MeshBuilder.New();
        }
        public IPolygonRenderer Visualize(Polygon[] polygons, Vector3 offset, Quaternion rotation, bool final, Transform parent, Color color)
        {
            // initialization
            var tr = new GameObject("polygon").transform;
            tr.SetParent(parent, false);
            tr.localPosition = offset;
            tr.localRotation = rotation;
            MeshRenderer mr = tr.gameObject.AddComponent<MeshRenderer>();
            MeshFilter mf = tr.gameObject.AddComponent<MeshFilter>();
            UpdateVisuals(mr, mf, polygons);

            return new Renderer(tr);
        }

        public void UpdateVisuals(IPolygonRenderer obj, Polygon[] polygons)
        {
            var mr = obj.Transform.GetComponent<MeshRenderer>();
            var mf = obj.Transform.GetComponent<MeshFilter>();
            if (mr && mf) {
                UpdateVisuals(mr, mf, polygons);
            }
        }
        private void UpdateCorners(Polygon p) {
            _corners.ClearFast();
            Vector2 prev = p.corners[p.corners.Length-1].position;
            float sum = 0;
            for(int i = 0; i < p.corners.Length; i++) {
                Vector2 point = p.corners[i].position;
                sum += (point.x - prev.x) * (point.y + prev.y);
                prev = point;
            }
            if(sum > 0) { // clock wise
                for(int i = 0; i < p.corners.Length; i++) {
                    _corners.Add(p.corners[i]);
                }
            }
            else {
                for (int i = p.corners.Length-1; i >= 0; i--) {
                    _corners.Add(p.corners[i]);
                }
            }
        }
        private void UpdateVisuals(MeshRenderer mr, MeshFilter mf, Polygon[] polygons) {
            mr.sharedMaterial = material;
            var t = Triangulator.Instance;
            var mesh = mf.sharedMesh;
            for(int i = 0; i < polygons.Length; i++) {
                var p = polygons[i];
                if(p.corners == null) {
                    p.corners = new Corner[0];
                    polygons[i] = p;
                }
                if (p.corners.Length > 2) {
                    UpdateCorners(p);

                    var carr = _corners.array;
                    for (int j = 0; j < _corners.Count; j++) {
                        t.Points.Add(carr[j].position);
                    }
                    int baseVertex = _builder.CurrentVertexCount;
                    _builder.AddVertices(t.Points);
                    AddEdgeNormals(t.Points);
                    var triangles = t.Triangulate(triangulationMode);
                    _builder.AddVertices(t.ExtraPoints);
                    AddExtraNormals(t.ExtraEdges);
                    _builder.AddTriangles(triangles, baseVertex);


                }
            }
            _builder.Flush(ref mesh);
            mf.sharedMesh = mesh;
        }
        void AddEdgeNormals(ExposedList<Vector2> edgePoints) {
            int count = edgePoints.Count;
            edgeNormals.Initialize();
            for (int i = 0; i <count; i++) {
                _builder.Normals.Add(Vector3.zero);
            }
            var arr = edgePoints.array;
            int i1 = count - 2;
            int i2 = i1 + 1;
            Vector2 p1 = arr[i1];
            Vector2 p2 = arr[i2];
            for(int i3 = 0; i3 < count; i3++) {
                Vector2 p3 = arr[i3];

                var d1 = p2 - p1;
                var d2 = p3 - p2;
                var n1 = new Vector2(-d1.y, d1.x);
                var n2 = new Vector2(-d2.y, d2.x);

                var n = (n1.normalized + n2.normalized).normalized;
                _builder.Normals[i2] = edgeNormals.Bend(n);

                i2 = i3;
                p1 = p2;
                p2 = p3;
            }
        }
        void AddExtraNormals(ExposedList<Triangulator.ExtraEdgeDetails> details) {
            var ns = _builder.Normals;
            while(ns.Count < _builder.CurrentVertexCount) {
                ns.Add(new Vector3(0, 0, -1));
            }
            int ecount = details.Count;
            var arr = details.array;
            for(int i = 0; i < ecount; i++) {
                var dt = arr[i];
                ns[dt.index] = Vector3.LerpUnclamped(ns[dt.p1], ns[dt.p2], dt.lerp);
            }
        }
#if UNITY_EDITOR
		public Texture EditorVisual()
		{
			return material.mainTexture;
		}
#endif

		public class Renderer : IPolygonRenderer
        {
            private Transform _trasform;

            public Renderer(Transform tr) {
                _trasform = tr;
            }
            public Transform Transform => _trasform;

            public Color Color { get => Color.white; set { } }
        }
        private struct MeshBuilder {
            private List<Vector3> _vertices;
            private List<int> _triangles;
            private List<Vector3> _normals;
            private List<Vector2> _uvs;

            public List<Vector3> Normals => _normals;

            public int CurrentVertexCount { get => _vertices.Count; }

            public void AddVertices(ExposedList<Vector3> vertices) {
                var data = vertices.array;
                 var c = vertices.Count;
                for(int i = 0; i < c; i++) {
                    _vertices.Add(data[i]);
                }
            }
            public void AddVertices(ExposedList<Vector2> vertices) {
                var data = vertices.array;
                 var c = vertices.Count;
                for(int i = 0; i < c; i++) {
                    _vertices.Add(data[i]);
                }
            }
            public void AddTriangles(ExposedList<int> triangles, int startingOffset) {
                var data = triangles.array;
                var c = triangles.Count;
                for(int i = 0; i < c; i++) {
                    _triangles.Add(data[i] + startingOffset);
                }
            }
            public static MeshBuilder New() {
                MeshBuilder mb;
                mb._triangles = new List<int>();
                mb._vertices = new List<Vector3>();
                mb._normals = new List<Vector3>();
                mb._uvs = new List<Vector2>();
                return mb;
            }
            public void Flush(ref Mesh mesh) {
                if(mesh == null) {
                    mesh = new Mesh();
                }
                //Debug.Log("v: " + _vertices.Count + " t: "+ _triangles.Count);
                mesh.Clear(true);
                mesh.SetVertices(_vertices);
                _vertices.Clear();
                mesh.SetUVs(0, _uvs);
                _uvs.Clear();
                mesh.SetTriangles(_triangles, 0);
                _triangles.Clear();
                mesh.SetNormals(_normals);
                _normals.Clear();
                
            }
        }

        [Serializable]
        public struct NormalBender
        {
            public float z;
            private float _xyMult;

            public NormalBender(float z) : this() {
                this.z = z;
            }
            public void Initialize() {
                _xyMult = Mathf.Sqrt(1 - z * z);
            }
            public Vector3 Bend(in Vector2 normal) {
                Vector3 v;
                v.z = -z;
                v.x = normal.x * _xyMult;
                v.y = normal.y * _xyMult;
                return v;
            }
        }
    }
}