using Mobge.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    public struct RoadTracker : IEnumerator<Pose> {
        public RoadGeneratorComponent.Data roadGenerator;
        private int _currentItem;
        private BezierPath3D _bezierPath;
        private BezierPath3D.SegmentEnumerator _enumerator;

        public bool IsValid => roadGenerator != null;
        public int CurrentItemIndex => _currentItem;


        public Pose Current => new Pose(_enumerator.CurrentPoint, Quaternion.LookRotation(_enumerator.CurrentDirection, _enumerator.CurrentNormal));
        public Vector3 CurrentPosition => _enumerator.CurrentPoint;

        public Vector3 CurrentDerivative => _enumerator.CurrentDerivative;
        public Vector3 CurrentSecondDerivative => _enumerator.CurrentSecondDerivative;

        public float CurrentPercentage {
            get {
                var si = _enumerator.Current;
                return (si.index + si.progress) / (_bezierPath.Points.Count - 1);
            }
        }
        public RoadTracker(RoadGeneratorComponent.Data roadGenerator) {
            this.roadGenerator = roadGenerator;
            _bezierPath = new BezierPath3D();
            _bezierPath.Points.Clear();
            _bezierPath.Points.Add(new BezierPath3D.Point());
            _bezierPath.Points.Add(new BezierPath3D.Point());
            _bezierPath.closed = false;
            _bezierPath.normalAlgorithm = BezierPath3D.NormalAlgorithmType.TangentAlgorithm;
            _currentItem = -1;
            _enumerator = default;
            SwitchToNextItem();
        }
        public RoadTracker (RoadGeneratorComponent.Data roadGenerator, int pieceIndex, float piecePercentage) {
            this = new RoadTracker(roadGenerator);
            SetLocation(pieceIndex, piecePercentage);
        }
        public void Update(RoadGeneratorComponent.Data roadGenerator, int pieceIndex, float piecePercentage) {
            if(_bezierPath == null) {
                this = new RoadTracker(roadGenerator, pieceIndex, piecePercentage);
            }
            else {
                if (this.roadGenerator != roadGenerator) {
                    this.roadGenerator = roadGenerator;
                    _currentItem = -1;
                }
                
                SetLocation(pieceIndex, piecePercentage);
            }
        }
        private void SetLocation(int pieceIndex, float piecePercentage) {
            JumpToItem(pieceIndex);
            int segmentCount = _bezierPath.Points.Count - 1;
            piecePercentage *= segmentCount;
            int index = Mathf.FloorToInt(piecePercentage);
            float percentage = piecePercentage - index;
            _enumerator = _bezierPath.GetEnumerator(0, index, percentage);
        }
        public bool MoveNext() {
            return MoveForward(1f);
        }
        public bool MoveForward(float amount) {
            float leftover = amount;
            while (!_enumerator.MoveForward(leftover)) {
                leftover = _enumerator.Leftover;
                if (!SwitchToNextItem()) {
                    return false;
                }
            }
            return true;
        }
        public float AproximateProgress {
            get {
                var segment = _enumerator.Current;
                float pathPercentage = (segment.index + segment.progress) / _bezierPath.Points.Count;
                float passedItems = _currentItem + pathPercentage;
                float totalLength = roadGenerator.items.Length;
                return passedItems / totalLength;
            }
        }
        public bool MoveForwardByPercent(float amount) {
            float leftover = amount;
            while (!_enumerator.MoveForwardByPercent(leftover)) {
                leftover = _enumerator.Leftover;
                if (!SwitchToNextItem()) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Move accurate version of move forward. Only works for relativly small values like (speed * delta_time).
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public bool MoveForwardSmallAmount(float amount) {

            float leftover = amount;
            while (!_enumerator.MoveBezierBySmallAmount(leftover)) {
                leftover = _enumerator.Leftover;
                if (!SwitchToNextItem()) {
                    return false;
                }
            }
            return true;
        }
        public bool MoveBackwardSmallAmount(float amount) {
            float leftover = amount;
            //bool reversed = false;
            //Vector3 startPos = CurrentPosition;
            while (!_enumerator.MoveBezierBySmallAmount(-leftover)) {
                leftover = _enumerator.Leftover;
                //Debug.Log("le: " + amount + " -> " + leftover);
                if (!SwitchToPreviousItem()) {
                    return false;
                }
                _enumerator.SetCurrentProgress(_enumerator.PointCount - 2, 0.99f);
                //reversed = true;
            }

            //if (reversed) {
            //    Debug.Log(startPos + " -> " + CurrentPosition);
            //    Debug.Break();
            //}
            return true;
        }
        private void JumpToItem(int index) {

            if (_currentItem == index) {
                return;
            }

            //Debug.Log(_currentItem + " -> " + index + " : " + roadGenerator.items[index].id + " : " + Time.time);
            _currentItem = index;
            var item = roadGenerator.items[_currentItem];
            var prefabRef = roadGenerator.prefabReferences[item.id];

            var pieceTr = item.instance.transform;


            if (item.instance is RoadGeneratorComponent.IRoadPiece rp) {
                rp.UpdateBezier(_bezierPath, 0, 1);
            }
            else {
                var pose = new Pose(pieceTr.position, pieceTr.rotation);
                var scl = prefabRef.res.transform.localScale;
                var startPoint = TransformPose(prefabRef.StartPose, pose, scl);
                var endPoint = TransformPose(prefabRef.EndPose, pose, scl);
                startPoint.rotation = RoadGeneratorComponent.Data.Reverse(startPoint.rotation);
                RoadGeneratorComponent.UpdateBezier(_bezierPath, startPoint, endPoint, roadGenerator.defaultCubicness);
                //Debug.Log("def used");
            }

            _enumerator = _bezierPath.GetEnumerator(0f);
            _enumerator.MoveForward(0f);
        }

        private Pose TransformPose(Pose pose, in Pose parentPose, in Vector3 parentScl) {
            pose.position.Scale(parentScl);
            return pose.GetInverseTransformedBy(parentPose);
        }

        private bool SwitchToNextItem() {
            if (_currentItem == roadGenerator.items.Length - 1) return false;
            JumpToItem(_currentItem + 1);
            return true;
        }
        private bool SwitchToPreviousItem() {
            if (_currentItem == 0) return false;
            JumpToItem(_currentItem - 1);
            return true;
        }
        public void Reset() { }
        object IEnumerator.Current => Current;


        public void Dispose() {
            _enumerator.Dispose();
        }
    }
}