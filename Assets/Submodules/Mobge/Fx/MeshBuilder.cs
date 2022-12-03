using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public class MeshBuilder
    {
        public List<Vector3> vertices;
        public List<Vector3> normals;
        public List<int> triangles {
            get => subTriangles.array[_subMeshIndex];
        }
        public ExposedList<List<int>> subTriangles;
        public List<Color> colors;
        public List<Vector2> uvs;
        public List<BoneWeight> boneWeights;
        public List<Matrix4x4> bindposes;
        private int _subMeshIndex = 0;
        public MeshBuilder() {
            vertices = new List<Vector3>();
            normals = new List<Vector3>();
            colors = new List<Color>();
            boneWeights = new List<BoneWeight>();
            uvs = new List<Vector2>();
            bindposes = new List<Matrix4x4>();

            subTriangles = new ExposedList<List<int>>();
            subTriangles.Add(new List<int>());
        }
        public void FillValues(Mesh mesh) {
            Clear();
            mesh.GetVertices(vertices);
            mesh.GetNormals(normals);
            mesh.GetColors(colors);
            mesh.GetUVs(0, uvs);
            mesh.GetBoneWeights(boneWeights);
            mesh.GetBindposes(bindposes);


            var sbcount = mesh.subMeshCount;
            mesh.GetTriangles(subTriangles.array[0], 0);
            for (int i = 1; i < sbcount; i++) {
                MoveToNextSubMesh();
                mesh.GetTriangles(subTriangles.array[i], i);
            }
        }
        public void FillValues(MeshBuilder meshBuilder) {
            Clear();

            var vertices = meshBuilder.vertices;

            var normals = meshBuilder.normals;
            var hasNormals = normals.Count > 0;

            var colors = meshBuilder.colors;
            var hasColors = colors.Count > 0;

            var uvs = meshBuilder.uvs;
            var hasUVS = uvs.Count > 0;

            var boneWeights = meshBuilder.boneWeights;
            var hasBoneWeights = boneWeights.Count > 0;

            var bindposes = meshBuilder.bindposes;
            var hasBindposes = boneWeights.Count > 0;

            for (int i = 0; i < vertices.Count; i++) {
                this.vertices.Add(vertices[i]);
                if (hasNormals) this.normals.Add(normals[i]);
                if (hasColors) this.colors.Add(colors[i]);
                if (hasUVS) this.uvs.Add(uvs[i]);
                if (hasBoneWeights) this.boneWeights.Add(boneWeights[i]);
                if (hasBindposes) this.bindposes.Add(bindposes[i]);
            }

            var subTriangles = meshBuilder.subTriangles;
            var subMeshCount = subTriangles.Count;
            this.subTriangles.SetCount(subMeshCount);
            for (int i = 0; i < subMeshCount; i++) {
                var triangles = subTriangles.array[i];
                var fillTriangle = this.subTriangles.array[i];
                if (fillTriangle == null) {
                    fillTriangle = new List<int>();
                    this.subTriangles.array[i] = fillTriangle;
                }
                for (int j = 0; j < triangles.Count; j++) {
                    fillTriangle.Add(triangles[j]);
                }
            }
        }
        public void Clear() {
            vertices.Clear();
            normals.Clear();
            colors.Clear();
            boneWeights.Clear();
            bindposes.Clear();
            uvs.Clear();

            for (int i = 0; i < subTriangles.Count; i++) {
                subTriangles.array[i].Clear();
            }
            subTriangles.SetCountFast(1);
            _subMeshIndex = 0;
        }
        public void BuildMesh(Mesh mesh) {
            mesh.MarkDynamic();
            var vCount = vertices.Count;
            mesh.Clear(true);
            if (vCount == 0) {
                return;
            }
            mesh.SetVertices(vertices);
            if (normals.Count == vCount) {
                mesh.SetNormals(normals);
            }
            if (colors.Count == vCount) {
                mesh.SetColors(colors);
            }
            if (uvs.Count == vCount) {
                mesh.SetUVs(0, uvs);
            }
            if (boneWeights.Count == vCount) {
                mesh.boneWeights = boneWeights.ToArray();
            }
            if (bindposes.Count != 0) {
                mesh.bindposes = bindposes.ToArray();
            }
            mesh.subMeshCount = subTriangles.Count;
            for (int i = 0; i < subTriangles.Count; i++) {
                mesh.SetTriangles(subTriangles.array[i], i);
            }
        }
        public void AddTriangle(int i1, int i2, int i3) {
            var triangles = subTriangles.array[_subMeshIndex];
            triangles.Add(i1);
            triangles.Add(i2);
            triangles.Add(i3);
        }
        public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) {
            var currentVertexCount = vertices.Count;
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            AddTriangle(currentVertexCount, currentVertexCount + 1, currentVertexCount + 2);
        }
        public int MoveToNextSubMesh() {
            return MoveToSpecificSubMesh(_subMeshIndex + 1);
        }
        public int MoveToSpecificSubMesh(int index) {
            _subMeshIndex = index;
            subTriangles.SetCountFast(Mathf.Max(subTriangles.Count, index + 1));
            for (int i = 0; i < subTriangles.Count; i++) {
                if (subTriangles.array[i] == null) {
                    subTriangles.array[i] = new List<int>();
                }
            }
            return _subMeshIndex;
        }
        private void AddTo<T>(List<T> source, List<T> target, int index) {
            if (source.Count == 0) {
                return;
            }
            target.Add(source[index]);
        }
        private void SetFrom<T>(List<T> target, List<T> source, int sourceIndex, int targetIndex) {
            if (target.Count == 0) {
                return;
            }
            target[targetIndex] = source[sourceIndex];
        }
        public void AddFrom(MeshBuilder source, int index) {
            AddTo(source.vertices, vertices, index);
            AddTo(source.normals, normals, index);
            AddTo(source.colors, colors, index);
            AddTo(source.uvs, uvs, index);
            AddTo(source.boneWeights, boneWeights, index);
        }
        public void SetFrom(MeshBuilder source, int sourceIndex, int targetIndex) {
            SetFrom(vertices, source.vertices, sourceIndex, targetIndex);
            SetFrom(normals, source.normals, sourceIndex, targetIndex);
            SetFrom(colors, source.colors, sourceIndex, targetIndex);
            SetFrom(uvs, source.uvs, sourceIndex, targetIndex);
            SetFrom(boneWeights, source.boneWeights, sourceIndex, targetIndex);
        }
        public Bounds CalculateBounds() {
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity); ;
            foreach (Vector3 v in vertices) {
                if (v.x < min.x) {
                    min.x = v.x;
                }
                if (v.x > max.x) {
                    max.x = v.x;
                }
                if (v.y < min.y) {
                    min.y = v.y;
                }
                if (v.y > max.y) {
                    max.y = v.y;
                }
                if (v.z < min.z) {
                    min.z = v.z;
                }
                if (v.z > max.z) {
                    max.z = v.z;
                }
            }
            return new Bounds
            {
                min = min,
                max = max
            };
        }
    }
}