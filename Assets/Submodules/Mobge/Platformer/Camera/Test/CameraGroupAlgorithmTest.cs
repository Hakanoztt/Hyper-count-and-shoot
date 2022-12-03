using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Test {
    public class CameraGroupAlgorithmTest : MonoBehaviour
    {
        public new MeshRenderer renderer;
        public Vector3[] points;
        public Vector2Int simulationSize = new Vector2Int(512, 512);
        public int nearestPointCount = 1;
        public bool update;
        public int algorithmIndex;
        private Texture2D _tex;
        private Color[] _colors;
        private List<int> _selectedIndexes;

        private void Awake() {
            _tex = new Texture2D(simulationSize.x, simulationSize.y, TextureFormat.RGBA32, 0, true);
            renderer.material.mainTexture = _tex;
            _colors = new Color[simulationSize.x * simulationSize.y];
            _selectedIndexes = new List<int>();
        }
        private void Update() {
            if (!update) {
                return;
            }
            update = false;
            for (int i = 0; i < _colors.Length; i++) {
                float x = i % simulationSize.x;
                float y = i / simulationSize.x;
                var c = Sample(new Vector2(x, y));
                _colors[i] = new Color(c, c, c, 1);
            }
            _tex.SetPixels(_colors);
            _tex.Apply();
        }
        float Sample(Vector2 v) {
            switch (algorithmIndex) {
                default:
                case 0:
                    return new Sampler0(this).Sample(v);
                case 1:
                    return new Sampler1(this).Sample(v);
                case 2:
                    return new Sampler2(this).Sample(v);
                case 3:
                    return new Sampler3(this).Sample(v);
            }
        }
        private void UpdateSelectedIndexes(Vector2 pos, int count) {
            _selectedIndexes.Clear();
            for (int i = 0; i < count; i++) {
                _selectedIndexes.Add(Nearest(pos));
            }

        }
        private int Nearest(Vector2 pos) {
            float d2 = float.PositiveInfinity;
            int index = -1;
            for (int i = 0; i < points.Length; i++) {
                if (_selectedIndexes.Contains(i)) {
                    continue;
                }
                var dif = pos - (Vector2)points[i];
                var m2 = dif.sqrMagnitude;
                if (m2 < d2) {
                    d2 = m2;
                    index = i;
                }
            }
            return index;
        }
        public struct Sampler0
        {
            private CameraGroupAlgorithmTest _test;

            public Sampler0(CameraGroupAlgorithmTest test) {
                _test = test;
            }
            public float Sample(Vector2 pos) {
                _test.UpdateSelectedIndexes(pos, _test.nearestPointCount);
                float h = 0;
                float totalMul = 0;
                for (int i = 0; i < _test._selectedIndexes.Count; i++) {
                    var v3 = _test.points[_test._selectedIndexes[i]];
                    Vector2 v2 = v3;
                    var d = 1 / (v2 - pos).magnitude;
                    totalMul += d;
                    h += v3.z * d;
                }
                return h / totalMul;
            }
        }
        public struct Sampler1
        {
            private CameraGroupAlgorithmTest _test;

            public Sampler1(CameraGroupAlgorithmTest test) {
                this._test = test;
            }

            internal float Sample(Vector2 v) {
                _test.UpdateSelectedIndexes(v, 2);
                Vector3 p1 = _test.points[_test._selectedIndexes[0]];
                Vector3 p2 = _test.points[_test._selectedIndexes[1]];
                Vector2 pos1 = p1;
                Vector2 pos2 = p2;
                var rate = GeometryUtils.PointToLineProjection(pos1, pos2, v);
                return Mathf.LerpUnclamped(p1.z, p2.z, rate);

            }
        }
        public struct Sampler2
        {
            private CameraGroupAlgorithmTest _test;
            public Sampler2(CameraGroupAlgorithmTest test) {
                this._test = test;
            }
            internal float Sample(Vector2 v) {
                _test.UpdateSelectedIndexes(v, _test.points.Length);
                Vector3 p1 = _test.points[_test._selectedIndexes[0]];
                Vector2 pos1 = p1;
                for (int i = 1; i < _test._selectedIndexes.Count; i++) {
                    Vector3 p2 = _test.points[_test._selectedIndexes[i]];
                    Vector2 pos2 = p2;
                    var rate = GeometryUtils.PointToLineProjection(pos1, pos2, v);
                    p1 = Vector3.LerpUnclamped(p1, p2, rate);
                    pos1 = p1;
                }
                return p1.z;
            }
        }
        public struct Sampler3
        {
            private CameraGroupAlgorithmTest _test;
            public Sampler3(CameraGroupAlgorithmTest test) {
                _test = test;
            }
            internal float Sample(Vector2 v) {
                _test.UpdateSelectedIndexes(v, 1);
                var index = _test._selectedIndexes[0];
                Vector3 p1 = _test.points[_test._selectedIndexes[0]];
                Vector2 pos1 = p1;
                float totalWeight = 0;
                float value = 0;
                for(int i = 0; i < _test.points.Length; i++) {
                    if (index == i)
                        continue;
                    Vector3 p2 = _test.points[i];
                    Vector2 pos2 = p2;
                    var dsqr = GeometryUtils.LineToPointDistanceSqr(pos1, pos2, v, out float rate);
                    var weigt = 1 / Mathf.Sqrt(dsqr);
                    totalWeight += weigt;
                    value += weigt * Mathf.LerpUnclamped(p1.z, p2.z, rate);
                }
                return value / totalWeight;
            }
        }
    }
}
