using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    public class Line3DRoadPiece : MonoBehaviour, RoadGeneratorComponent.IRoadPiece {
        [OwnComponent] public Line3D line;
        public float endOffset = 0.05f;
        public bool alignEndPoints;
        [HideInInspector] public SimpleSetup simpleSetup;

        BezierPath3D.NormalModule _normalCaluclator;

        private Matrix4x4 _lineToLocal;
        private bool _initialized;

        void Awake() {
            EnsureInit();

            if (alignEndPoints) {
                AlignEndPoints();
            }
        }

        void EnsureInit() {
            if (!_initialized) {
                _initialized = true;
                CalculateLineToLocalMatrix();
                line.EnsureInitialization();
                line.ReconstructImmediate();
            }
        }

        int RoadGeneratorComponent.IRoadPiece.EndPointCount => 2;

        Pose RoadGeneratorComponent.IRoadPiece.GetLocalEndPoint(int index) {
            EnsureInit();
            switch (index) {
                default:
                case 0: {
                        var p = BezierPose(0, endOffset, true);
                        p.position = _lineToLocal.MultiplyPoint3x4(p.position);
                        
                        p.rotation = _lineToLocal.rotation * p.rotation;
                        return p;
                    }
                case 1: {
                        var p = BezierPose(line.path.Points.Count - 1,  endOffset, false);
                        p.position = _lineToLocal.MultiplyPoint3x4(p.position);
                        p.rotation = _lineToLocal.rotation * p.rotation;
                        return p;
                    }
            }
        }
        Pose BezierPose(int index, float progress, bool reversed) {
            int i;
            var dir = line.path.EvaluateDerivative(index, 0);
            if (reversed) {
                i = index;
                dir = -dir;
            }
            else {
                i = index - 1;
                progress = 1f - progress;
            }
            var pos = line.path.Evaluate(i, progress);
            var up = line.path.EvaluateNormal(_normalCaluclator, index, 0);
            //up = Vector3.Cross(dir, up);
            return new Pose(pos, Quaternion.LookRotation(dir, up));
        }
        void CalculateLineToLocalMatrix() {
            _lineToLocal = transform.worldToLocalMatrix * line.transform.localToWorldMatrix;
        }

        public void AlignEndPoints() {
            line.AlignEndPoints();
            line.SetDirty();
            line.ReconstructImmediate();
        }

        public void OnChildDisabled() {
            if (alignEndPoints) {
                AlignEndPoints();
            }
        }

        void RoadGeneratorComponent.IRoadPiece.UpdateBezier(BezierPath3D path, int endpoint1, int endpoint2) {
            var mat = this.line.transform.localToWorldMatrix;
            path.Points.ClearFast();
            for(int i = 0; i < line.path.Points.Count; i++) {

                var p = line.path.Points.array[i];
                p.position = mat.MultiplyPoint3x4(p.position);
                p.leftControl = mat.MultiplyPoint3x4(p.leftControl);
                p.rightControl = mat.MultiplyPoint3x4(p.rightControl);

                path.Points.Add(p);
            }
            path.Points.array[0].position = path.Evaluate(0, endOffset);
            int lastI = path.Points.Count - 1;
            path.Points.array[lastI].position = path.Evaluate(lastI - 1, 1 - endOffset);
            path.controlMode = line.path.controlMode;
            path.normalAlgorithm = line.path.normalAlgorithm;
        }
        Pose RoadGeneratorComponent.IRoadPiece.SampleFromPose(float percentage, Pose pose, int endpoint1, int endpoint2) {
            if(line == null||line.path.Points.Count <= 1) {
                return pose;
            }
            var path = line.path;
            int segment = path.Points.Count - 1;
            percentage *= segment;
            int index = Mathf.FloorToInt(percentage);
            float rate = percentage - index;
            var pos = path.Evaluate(index, rate);
            var direction = path.EvaluateDerivative(index, rate);
            var normal = path.EvaluateNormal(index, rate);
            var localPose = new Pose(pos, Quaternion.LookRotation(direction, normal));
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                CalculateLineToLocalMatrix();
            }
#endif
            localPose.position = _lineToLocal.MultiplyPoint3x4(localPose.position);
            localPose.rotation = _lineToLocal.rotation * localPose.rotation;
            localPose.position.Scale(transform.localScale);
            return localPose.GetTransformedBy(pose);


        }
        protected void OnDrawGizmosSelected() {
            RoadGeneratorComponent.IRoadPiece ts = this;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.black;
            for (int i  = 0; i < ts.EndPointCount; i++) {
                var ep = ts.GetLocalEndPoint(i);
                var sideOffset = ep.rotation * Vector3.right * 0.15f;
                Gizmos.DrawLine(ep.position + sideOffset, ep.position - sideOffset);
                Gizmos.DrawLine(ep.position, ep.position + ep.rotation * Vector3.forward * 2.5f);
            }
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.white;
            CalculateLineToLocalMatrix();

        }
        [Serializable]
        public struct SimpleSetup {
            public const float c_cubicness = 0.3f;
            public bool enabled;
            public float length;
            public float deltaHeight;
            public float turnDegree;

            public void Apply(Line3D line) {
                Vector3 startDir = new Vector3(0, 0, -1);
                Vector3 start = new Vector3(0, 0, -length * 0.5f);
                float cos = Mathf.Cos(turnDegree * Mathf.Deg2Rad);
                float sin = Mathf.Sin(turnDegree * Mathf.Deg2Rad);
                Vector3 endDir = new Vector3(sin, 0, cos);
                Vector3 end = endDir * (length * 0.5f);
                end.y += deltaHeight;

                var offset = (start + end) * 0.5f;
                start -= offset;
                end -= offset;


                if (line.path.Points.Count != 2) {
                    line.path.Points.SetCount(2);
                }
                float positionOffset = length * c_cubicness;
                line.path.Points.array[0] = new BezierPath3D.Point() {
                    position = start,
                    rightControl = start - startDir * positionOffset,
                    leftControl = start + startDir * positionOffset,
                }; 
                line.path.Points.array[1] = new BezierPath3D.Point() {
                    position = end,
                    leftControl = end - endDir * positionOffset,
                    rightControl = end + endDir * positionOffset,
                };
                line.path.controlMode = BezierPath3D.ControlMode.Mirrored;
                line.SetDirty();
            }
        }
    }
}