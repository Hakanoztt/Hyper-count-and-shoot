using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static Mobge.LineRendererPlus;

namespace Mobge {
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class Line3D : MonoBehaviour
    {
        private static MeshBuilder _meshBuilder = new MeshBuilder();
        private static MeshBuilder _meshData = new MeshBuilder();
        private static ExposedList<int> _vertexOrder = new ExposedList<int>();
        private static ExposedList<int> _vertexReverseMap = new ExposedList<int>();
        private static Comparer _comparer = new Comparer();
        private class Comparer : IComparer<int> {
            public List<Vector3> vertices;
            public int Compare(int x, int y)
            {
                var v1 = vertices[x].x;
                var v2 = vertices[y].x;
                if(v1 < v2){
                    return -1;
                }
                if(v1 > v2) {
                    return 1;
                }
                return 0;
            }
        }
        public BezierPath3D path;
        private MeshCollider _collider;

        [SerializeField, HideInInspector] private MeshRenderer _mr;
        public MeshRenderer Renderer{
            get {
                EnsureInitialization();
                return _mr;
            }
        }
        [SerializeField, HideInInspector] private MeshFilter _mf;
        public MeshFilter MeshFilter {
            get {
                EnsureInitialization();
                return _mf;
            }
        }

        [SerializeField, HideInInspector] private Mesh _mesh;
        public Mesh Mesh {
            get => _mesh;
            set{
                if(_mesh != value) {
                    _mesh = value;
                    enabled = true;
                    _isDirty = true;
                }
            }
        }
        [SerializeField, HideInInspector] private Vector3 _meshScale = Vector3.one;
        public Vector3 MeshScale {
            get => _meshScale;
            set {
                value = Vector3.Max(new Vector3(0.01f, 0.01f, 0.01f), value);
                if(_meshScale != value) {
                    _meshScale = value;
                    _isDirty = true;
                    enabled = true;
                }

            }
        }
        [SerializeField] [HideInInspector] private Mode _mode;
        public Mode TileMode {
            get => _mode;
            set {
                if(_mode != value) {
                    _mode = value;
                    _isDirty = true;
                    enabled = true;
                }
            }
        }
        [SerializeField][HideInInspector] private Direction _direction;
        public Direction TileDirection {
            get => _direction;
            set {
                if (_direction != value) {
                    _direction = value;
                    _isDirty = true;
                    enabled = true;
                }
            }
        }
        private bool _isDirty = true;
        public bool reconstructOnAwake = true;
        protected void Awake() {
            EnsureInitialization();
            enabled = false;
            if (reconstructOnAwake){
                Reconstruct();
            }
        }
        public void EnsureInitialization() {
            if(_mr == null){
                _mr = GetComponent<MeshRenderer>();
                _mf = GetComponent<MeshFilter>();
            }
        }
        public void ReconstructImmediate() {
            Update();
        }
        public void SetDirty() {
            _isDirty = true;
        }
        protected void Update() {
            if(_isDirty) {
                _isDirty = false;
                Reconstruct();
            }
#if UNITY_EDITOR
            if (Application.isPlaying) {
                enabled = false;
            }
#else
            enabled = false;
#endif
        }
        void Reconstruct() {
            if(path == null || path.Points.Count < 2) {
                _mr.enabled = false;
                return;
            }
            if(_mesh == null) {
                return;
            }

            _collider = GetComponent<MeshCollider>();
            if (!_mr.enabled) {
                _mr.enabled = true;
            }
            _meshData.FillValues(_mesh);
            switch(_direction) {
                case Direction.X:
                    break;
                case Direction.Y:
                    for (int i = 0; i < _meshData.vertices.Count; i++) {
                        var v = _meshData.vertices[i];
                        (v.y, v.x) = (v.x, -v.y);
                        _meshData.vertices[i] = v;
                    }
                    for (int i = 0; i < _meshData.normals.Count; i++) {
                        var v = _meshData.normals[i];
                        (v.y, v.x) = (v.x, -v.y);
                        _meshData.normals[i] = v;
                    }
                    break;
                case Direction.Z:
                    for (int i = 0; i < _meshData.vertices.Count; i++) {
                        var v = _meshData.vertices[i];
                        (v.z, v.x) = (v.x, -v.z);
                        _meshData.vertices[i] = v;
                    }
                    for (int i = 0; i < _meshData.normals.Count; i++) {
                        var v = _meshData.normals[i];
                        (v.z, v.x) = (v.x, -v.z);
                        _meshData.normals[i] = v;
                    }
                    break;
            }
            EnsureInitialization();
            var bounds = _meshData.CalculateBounds();
            if (bounds.extents.x <= 0){
                return;
            }

            bool hasNormals = _meshData.normals != null && _meshData.normals.Count > 0;

            InitializeVertexMap();
            MeshEnumerator vertexEnumerator = new MeshEnumerator(bounds, _meshScale);
            var e = path.GetEnumerator(1f);
            e.MoveForward(0);
            Axis a = new Axis(e.CurrentDirection, e.CurrentNormal);
            float totalLength = 0;
            do {
                vertexEnumerator.MoveNext();
                var cd = vertexEnumerator.CurrentDistance;
                if (cd > 0) {
                    if (!e.MoveForward(cd)) {
                        break;
                    }
                    totalLength += cd;
                    a = new Axis(e.CurrentDirection, e.CurrentNormal);
                }
                var v = vertexEnumerator.LastVertex;
                vertexEnumerator.LastVertex = e.CurrentPoint + v.y * a.up + v.z * a.right;
                if (hasNormals) {
                    var n = vertexEnumerator.LastNormal;
                    vertexEnumerator.LastNormal = LocalToWorld(a, n);
                }
            } while(true);
            vertexEnumerator.Finish();

            var targetMesh = _mf.sharedMesh;
            if(targetMesh == null) {
                targetMesh = new Mesh();
                _mf.sharedMesh = targetMesh;
            }
            _meshBuilder.BuildMesh (targetMesh);
            _meshBuilder.Clear();
            _meshData.Clear();

            if(_collider != null) {
                _collider.sharedMesh = targetMesh;
            }
        }

        public static Vector3 LocalToWorld(Axis a, Vector3 local) {
            return local.z * a.right + local.y * a.up + local.x * a.forward;
        }

        private struct MeshEnumerator {
            public int _nextIndex;
            private float _lastX;
            private float _xStart;
            private Bounds _meshBounds;
            private Vector3 _scale;
            private int _lastValidCount;

            public float CurrentDistance { get; private set; }
            public Vector3 Up { get; private set; }
            public Vector3 Right { get; private set; }
            public MeshEnumerator(Bounds meshBounds, Vector3 scale) : this() {
                _meshBounds = meshBounds;
                _scale = scale;
                Reset();
            }

            public void MoveNext() {
                _lastValidCount = _meshBuilder.vertices.Count;
                _meshBuilder.AddFrom(_meshData, _vertexOrder.array[_nextIndex]);
                var point = _meshData.vertices[_vertexOrder.array[_nextIndex]];
                point = Vector3.Scale(point, _scale);
                LastVertex = point;
                CurrentDistance = point.x - _lastX;
                _lastX = point.x;


                _nextIndex++;
                if (_nextIndex == _vertexOrder.Count) {
                    _lastValidCount = _meshBuilder.vertices.Count;
                    FillTriangles();
                    Reset();
                }
            }
            private void Reset() {
                _xStart = _meshBounds.min.x * _scale.x;
                _nextIndex = 0;
                _lastX = _xStart;
                _lastValidCount = 0;
            }
            public Vector3 LastVertex {
                get {
                    var v = _meshBuilder.vertices;
                    return v[v.Count - 1];
                }
                set {
                    var v = _meshBuilder.vertices;
                    v[v.Count - 1] = value;
                }
            }
            public Vector3 LastNormal {
                get {
                    var v = _meshBuilder.normals;
                    return v[v.Count - 1];
                }
                set {
                    var v = _meshBuilder.normals;
                    v[v.Count - 1] = value;
                }
            }

            private void FillTriangles() {
                int subMeshCount = _meshData.subTriangles.Count;
                // Debug.Log("sub mesh count: " + subMeshCount);
                for (int j = 0; j < subMeshCount; j++) {
                    var trs = _meshData.subTriangles.array[j];
                    _meshBuilder.MoveToSpecificSubMesh(j);
                    var builderTris = _meshBuilder.triangles;
                    var count = _meshBuilder.vertices.Count;
                    int offset = count - _nextIndex;
                    var mArr = _vertexReverseMap.array;
                    for (int i = 0; i < trs.Count;) {
                        int i1 = offset + mArr[trs[i++]];
                        int i2 = offset + mArr[trs[i++]];
                        int i3 = offset + mArr[trs[i++]];
                        if (i1 < _lastValidCount && i2 < _lastValidCount && i3 < _lastValidCount) {
                            builderTris.Add(i1);
                            builderTris.Add(i2);
                            builderTris.Add(i3);
                        }
                    }
                }

            }

            internal void Finish() {

                FillTriangles();
            }
        }

        private void InitializeVertexMap(){
            _vertexOrder.SetCountFast(_meshData.vertices.Count);
            for(int i = 0; i < _meshData.vertices.Count; i++) {
                _vertexOrder.array[i] = i;
            }
            _comparer.vertices = _meshData.vertices;
            Array.Sort(_vertexOrder.array, 0, _vertexOrder.Count, _comparer);
            _vertexReverseMap.SetCountFast(_meshData.vertices.Count);
            var voArr = _vertexOrder.array;
            var vmArr = _vertexReverseMap.array;
            for(int i = 0; i < _vertexOrder.Count; i++) {
                vmArr[voArr[i]] = i;
            }
        }

        public void AlignEndPoints() {
            var lastPointIndex = path.Points.Count - 1;

            path.Points.array[0].leftControl.y = path.Points.array[0].rightControl.y = path.Points.array[0].position.y;
            path.Points.array[lastPointIndex].leftControl.y = path.Points.array[lastPointIndex].rightControl.y = path.Points.array[lastPointIndex].position.y;
        }
        public enum Mode {
            Tile = 0,
            Stretch = 1,
        }
        public enum Direction {
            X = 0,
            Y = 1,
            Z = 2
        }
        private void OnDrawGizmos() {
            if (!Application.isPlaying) {
                ReconstructImmediate();
            }
        }
    }
}