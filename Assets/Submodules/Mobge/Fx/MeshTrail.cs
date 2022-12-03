using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Fx {
    
    [Serializable]
    public struct MeshTrail {
        public Gradient color;
        [NonSerialized]
        private Mesh _mesh;
        [NonSerialized]
        private Material _material;
        public float time;
        public int shadowCount;
        private ExposedQueue<Element> _positions;
        private MaterialPropertyBlock _block;
        private int _colorId;
        public void Initialize(Mesh mesh, Material material) {
            if (_positions == null) {
                _positions = new ExposedQueue<Element>();
            }
            this._mesh = mesh;
            this._material = material;
            _block = new MaterialPropertyBlock();
            _colorId = Shader.PropertyToID("_Color");
        }
        private struct Element {
            public Matrix4x4 m;
            public float time;

            public Element(in Matrix4x4 m, float time) {
                this.m = m;
                this.time = time;
            }
        }
        public void Update(in Matrix4x4 matrix) {
            var ctime = Time.time;
            float deleteTime = ctime - time;
            _positions.Enqueue(new Element(matrix, ctime));
            while (_positions.Count > 1 && _positions.array[_positions.Head].time < deleteTime) {
                _positions.Dequeue();
            }
            var e = _positions.GetReversedIndexEnumerator();
            var array = _positions.array;
            int prevIndex = _positions.TailIndex;
            float prevTime = array[prevIndex].time;

            float currentTime = ctime;
            float sampleTime = time / shadowCount;
            float colorProgressMultiplayer = shadowCount == 1 ? 1 : 1f / (shadowCount - 1);
            for (int i = 0; i < shadowCount; i++) {
                currentTime -= sampleTime;
                int nextIndex = prevIndex;
                while (prevTime > currentTime && e.MoveNext()) {
                    prevTime = array[(nextIndex = e.Current)].time;
                }
                Matrix4x4 m;
                if (prevIndex == nextIndex) {
                    m = array[nextIndex].m;
                }
                else {
                    var t1 = array[prevIndex].time;
                    var t2 = array[nextIndex].time;
                    float prog = currentTime - t1 / (t2 - t1);
                    m = MatrixLerp(array[prevIndex].m, array[nextIndex].m, prog);
                    prevIndex = nextIndex;
                }
                
                _block.SetColor(_colorId, color.Evaluate(i * colorProgressMultiplayer));
                Graphics.DrawMesh(_mesh, array[nextIndex].m, _material, 0, null, 0, _block);
            }
        }
        private static Matrix4x4 MatrixLerp(in Matrix4x4 from, in Matrix4x4 to, float time) {
            Matrix4x4 ret = new Matrix4x4();
            ret.m00 = Mathf.LerpUnclamped(from.m00, to.m00, time);
            ret.m01 = Mathf.LerpUnclamped(from.m01, to.m01, time);
            ret.m02 = Mathf.LerpUnclamped(from.m02, to.m02, time);
            ret.m03 = Mathf.LerpUnclamped(from.m03, to.m03, time);

            ret.m10 = Mathf.LerpUnclamped(from.m10, to.m10, time);
            ret.m11 = Mathf.LerpUnclamped(from.m11, to.m11, time);
            ret.m12 = Mathf.LerpUnclamped(from.m12, to.m12, time);
            ret.m13 = Mathf.LerpUnclamped(from.m13, to.m13, time);

            ret.m20 = Mathf.LerpUnclamped(from.m20, to.m20, time);
            ret.m21 = Mathf.LerpUnclamped(from.m21, to.m21, time);
            ret.m22 = Mathf.LerpUnclamped(from.m22, to.m22, time);
            ret.m23 = Mathf.LerpUnclamped(from.m23, to.m23, time);

            ret.m30 = Mathf.LerpUnclamped(from.m30, to.m30, time);
            ret.m31 = Mathf.LerpUnclamped(from.m31, to.m31, time);
            ret.m32 = Mathf.LerpUnclamped(from.m32, to.m32, time);
            ret.m33 = Mathf.LerpUnclamped(from.m33, to.m33, time);

            return ret;
        }
    }
}