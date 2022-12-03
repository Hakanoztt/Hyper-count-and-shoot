using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    [Serializable]
    public class BezierPath3D {

        public static NormalModule s_normalModule;




        [HideInInspector]
        public bool closed;

        public BezierPath3D() {
            _points = new PointList();
        }

        [SerializeField]
        private PointList _points;
        public float autoControlLength = 0.3f;
        [HideInInspector, SerializeField]
        public ControlMode controlMode = ControlMode.Free;
        
        [HideInInspector, SerializeField]
        public NormalAlgorithmType normalAlgorithm =
            NormalAlgorithmType.TangentAlgorithm;
        
        [HideInInspector, SerializeField]
        public bool flipNormals = false;
        public Vector3 constantVector = Vector3.up;
        
        [Serializable]
        public class PointList : ExposedList<Point> {
        }

        [Serializable]
        public struct Point {
            public Vector3 position;
            public Vector3 leftControl, rightControl;
            public float normalOffset;
        }
        public ExposedList<Point> Points {
            get => _points;
        }
        /// <summary>
        /// Creates a Segment enumerator with the path
        /// </summary>
        /// <param name="defaultMovement">Default movement length, will be used if MoveNext called without a parameter.</param>
        /// <param name="roundIndexToRange">Modulate the index when index gets out of scopes and thus never stop moving.
        /// Might cuz infinite array if the value is true and the moveNext function of the returned enumerator is used in loop.</param>
        /// <returns></returns>
        public SegmentEnumerator GetEnumerator(float defaultMovement) {
            return new SegmentEnumerator(this, defaultMovement);
        }
        
        public SegmentEnumerator GetEnumerator(float defaultMovement, int index, float progress) {
            return new SegmentEnumerator(this, defaultMovement, index, progress);
        }
        
        public void UpdateControlsForAuto() {
            if (_points.Count < 2) {
                return;
            }
            Vector3 lastCenter;
            if (closed) {
                lastCenter = _points.Last.position;
            }
            else {
                lastCenter = _points.array[0].position + (_points.array[0].position - _points.array[1].position) * 0.01f;
            }
            var arr = _points.array;
            int i = 0;
            for (; i < _points.Count - 1; i++) {
                UpdateControlsForAuto(ref arr[i], lastCenter, arr[i + 1].position);
                lastCenter = arr[i].position;
            }

            if (closed) {
                UpdateControlsForAuto(ref arr[i], lastCenter, arr[0].position);
            }
            else {
                UpdateControlsForAuto(ref arr[i], lastCenter, arr[i].position + (arr[i].position - lastCenter) * 0.01f);
            }
        }
        private void UpdateControlsForAuto(ref Point p, in Vector3 prev, in Vector3 next) {
            var magPrev = (prev - p.position).magnitude;
            var magNext = (next - p.position).magnitude;
            var dir = (-(prev - p.position) / magPrev + (next - p.position) / magNext).normalized;
            p.leftControl = p.position - magPrev * dir * autoControlLength;
            p.rightControl = p.position + magNext * dir * autoControlLength;
        }
        public static Vector3 EvaluateCurve(BezierPoints points, float t) {
            var mt = 1 - t;
            var t2 = t * t;
            var t3 = t2 * t;
            var mt2 = mt * mt;
            var mt3 = mt2 * mt;
            //return Vector3.LerpUnclamped(a1,a2,t);
            return mt3 * points.a1 + 3 * mt2 * t * points.c1 + 3 * mt * t2 * points.c2 + t3 * points.a2;
        }
        public static Vector3 EvaluateCurve(Vector3 a1, Vector3 c1, Vector3 c2, Vector3 a2, float t) {
            return EvaluateCurve(new BezierPoints(a1, c1, c2, a2), t);
        }
        public static Vector3 EvaluateDerivative(in BezierPoints points, float t) {
            //t = Mathf.Clamp01 (t);
            var mt = 1 - t;
            var t2 = t * t;
            var mt2 = mt * mt;
            //return -3*mt2*a1 + -6*mt*t *c1 + 3*mt2*c1 + -3*t2*c2 + 6 * mt * t * c2 + 3 * t2 * a2;
            return 3 * mt2 * (points.c1 - points.a1) + 6 * mt * t * (points.c2 - points.c1) + 3 * t2 * (points.a2 - points.c2);
        }
        public static Vector3 EvaluateCurveSecondDerivative (in BezierPoints points, float t) {
            return 6 * (1 - t) * (points.c2 - 2 * points.c1 + points.a1) + 6 * t * (points.a2 - 2 * points.c2 + points.c1);
        }
        private static float EstimateCurveLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
            float controlNetLength = (p0 - p1).magnitude + (p1 - p2).magnitude + (p2 - p3).magnitude;
            float estimatedCurveLength = (p0 - p3).magnitude + controlNetLength / 2f;
            return estimatedCurveLength;
        }
        public struct BezierPoints {
            public Vector3 a1, c1, c2, a2;
            public float normalOffset1, normalOffset2;

            public BezierPoints(Vector3 a1, Vector3 c1, Vector3 c2, Vector3 a2, float normalOffset1 = 0, float normalOffset2 = 0) {
                this.a1 = a1;
                this.c1 = c1;
                this.c2 = c2;
                this.a2 = a2;
                this.normalOffset1 = normalOffset1;
                this.normalOffset2 = normalOffset2;

            }
        }
        void GetBezierPoints(int index, out BezierPoints points) {
            var arr = Points.array;
            int count = _points.Count;
            Point p1, p2;
            int index1 = index + 1;
            if (index1 >= count) {
                if (index >= count) {
                    p1 = arr[index - count];
                }
                else {
                    p1 = arr[index];
                }
                p2 = arr[index1 - count];
            }
            else {
                p1 = arr[index];
                p2 = arr[index1];
            }
            points.a1 = p1.position;
            points.a2 = p2.position;
            points.c1 = p1.rightControl;
            points.c2 = p2.leftControl;
            points.normalOffset1 = p1.normalOffset;
            points.normalOffset2 = p2.normalOffset;
        }
        public Vector3 Evaluate(int index, float progress) {
            GetBezierPoints(index, out var bp);
            return EvaluateCurve(bp, progress);
        }
        public Vector3 EvaluateDirection(int index, float progress) {
            return EvaluateDerivative(index, progress).normalized;
        }
        public Vector3 EvaluateDerivative(int index, float progress) {
            GetBezierPoints(index, out var bp);
            return EvaluateDerivative(bp, progress);
        }
        public Vector3 EvaluateSecondDerivative(int index, float progress) {
            GetBezierPoints(index, out var bp);
            return EvaluateCurveSecondDerivative(bp, progress);
        }
        public Vector3 EvaluateNormal(int index, float progress) {
            return s_normalModule.GetNormal(this, index, progress);
        }
        public Vector3 EvaluateNormal(in NormalModule module, int index, float progress) {
            return module.GetNormal(this, index, progress);
        }
        public float EvaluateNormalOffset(int index, float progress) {
            GetBezierPoints(index, out var ps);
            
            var n = Mathf.LerpAngle(ps.normalOffset1, ps.normalOffset2, progress);
            return n;
        }

        public BezierPath3D Clone() {
            var newpath = new BezierPath3D();
            
            if(newpath._points == null) newpath._points = new PointList();
            newpath._points.SetCountFast(_points.Count);
            for (int i = 0; i < _points.Count; i++)
            {
                newpath._points.array[i] = _points.array[i];
            }
            newpath._points.lastSweepTime = _points.lastSweepTime;
            
            newpath.closed = closed;
            newpath.controlMode = controlMode;
            newpath.flipNormals = flipNormals;
            newpath.normalAlgorithm = normalAlgorithm;
            newpath.autoControlLength = autoControlLength;
            return newpath;
        }

        public struct SegmentEnumerator : IEnumerator<SegmentInfo> {
            private readonly BezierPath3D _path;
            private int _index;
            private float _progress;
            private readonly float _movement;
            private bool _initialized;

            public int PointCount => _path.Points.Count;

            /// <summary>
            ///
            /// </summary>
            /// <param name="path"></param>
            /// <param name="defaultMovement">Default movement length, will be used if MoveNext called without a parameter.</param>
            /// <param name="roundIndexToRange">Modulate the index when index gets out of scopes and thus never stop moving.
            /// Might cuz infinite array if the value is true and the moveNext function of the returned enumerator is used in loop.</param>
            public SegmentEnumerator(BezierPath3D path, float defaultMovement) {
                _path = path;
                _index = 0;
                _progress = 0;
                _movement = defaultMovement;
                Leftover = 0f;
                _initialized = false;
                if (path.Points == null || path.Points.Count <= 1) {
                    _index = 10;
                }
            }
            public void SetCurrentProgress(int index, float progress) {
                _index = index;
                _progress = progress;
            }
            public SegmentEnumerator(BezierPath3D path, float defaultMovement, int index, float progress) {
                _path = path;
                _index = index;
                _progress = progress;
                _movement = defaultMovement;
                Leftover = 0f;
                _initialized = false;
                if (path.Points == null || path.Points.Count <= 1) {
                    _index = 10;
                }
            }
            public Vector3 CurrentPoint => _path.Evaluate(_index, _progress);
            public Vector3 CurrentDirection => _path.EvaluateDirection(_index, _progress);
            public Vector3 CurrentDerivative => _path.EvaluateDerivative(_index, _progress);
            public Vector3 CurrentSecondDerivative => _path.EvaluateSecondDerivative(_index, _progress);

            public Vector3 CurrentNormal => s_normalModule.GetNormal(_path, _index, _progress);
            public float CurrentNormalOffset => _path.EvaluateNormalOffset(_index, _progress);
            public float Leftover { get; set; }
            public SegmentInfo Current => new SegmentInfo(_index, _progress);
            object IEnumerator.Current => new SegmentInfo(_index, _progress);

            public void Dispose() { }
            /// <summary>
            /// Move forward on the path with the defaultMovement.
            /// </summary>
            /// <returns></returns>
            public bool MoveNext() {
                return MoveForward(_movement);
            }
            /// <summary>
            /// Move forward or backward according to the given vector value.
            /// </summary>
            /// <param name="movement"></param>
            /// <returns></returns>
            public bool MoveNext(float movement) {
                if (movement >= 0) return MoveForward(movement);
                return MoveBackward(movement * -1);
            }
            private bool TryIncrementIndex() {
                if (_path.closed) {
                    if (_index >= _path.Points.Count - 1) {
                        return false;
                    }
                }
                else {
                    if (_index >= _path.Points.Count - 2) {
                        return false;
                    }
                }
                _index++;
                return true;
            }
            private bool TryDecrementIndex() {
                if (_index <= 0) {
                    return false;
                }
                _index--;
                return true;
            }
            /// <summary>
            /// Move backward on the path. Progress on a segment is calculated according to the length, homogeneous speed along the path.  
            /// </summary>
            /// <param name="movementLength">Length of the movement, assumed that the given value is positive.</param>
            /// <returns></returns>
            public bool MoveForward(float movementLength) {
                if (!_initialized) {
                    _initialized = true;
                    return true;
                }
                while (true) {
                    float currentLength = CurrentSegmentLength();
                    float leftToTraverse = currentLength * (1 - _progress);
                    if (leftToTraverse > movementLength) {
                        leftToTraverse -= movementLength;
                        Leftover = 0f;
                        _progress = (currentLength - leftToTraverse) / currentLength;
                        return true;
                    }
                    else {
                        movementLength -= leftToTraverse;
                        if (!TryIncrementIndex()) {
                            Leftover = movementLength;
                            _progress = 1f;
                            return false;
                        }
                        _progress = 0f;
                    }
                }
            }
            /// <summary>
            /// Move backward on the path. Progress on a segment is calculated according to the length, homogeneous speed along the path.  
            /// </summary>
            /// <param name="movementLength">Length of the movement, assumed that the given value is positive.</param>
            /// <returns></returns>
            public bool MoveBackward(float movementLength) {
                if (!_initialized) {
                    _initialized = true;
                    return true;
                }
                while (true) {
                    float currentLength = CurrentSegmentLength();
                    float leftToTraverse = currentLength * _progress;
                    if (leftToTraverse > movementLength) {
                        leftToTraverse -= movementLength;
                        Leftover = 0f;
                        _progress = leftToTraverse / currentLength;
                        return true;
                    }
                    else {
                        movementLength -= leftToTraverse;
                        if (!TryDecrementIndex()) {
                            _progress = 0f;
                            Leftover = movementLength;
                            return false;
                        }
                        _progress = 1f;
                    }
                }
            }
            /// <summary>
            /// Moves forward on the path. Speed is vary with the length of the segment. Use MoveForward for homogeneous speed.
            /// </summary>
            /// <param name="percent"></param>
            /// <returns></returns>
            public bool MoveForwardByPercent(float percent) {
                if (!_initialized) {
                    _initialized = true;
                    return true;
                }
                while (true) {
                    var leftToTraverse = 1f - _progress;
                    if (leftToTraverse > percent) {
                        _progress += percent;
                        Leftover = 0f;
                        return true;
                    }
                    else {
                        percent -= leftToTraverse;
                        if (!TryIncrementIndex()) {
                            Leftover = percent;
                            _progress = 1f;
                            return false;
                        }
                        _progress = 0f;
                    }
                }
            }


            /// <summary>
            /// Move accurate version of move forward. Only works for relativly small values like (speed * delta_time).
            /// </summary>
            /// <param name="amount">Specified amount to move. Can take negative values.</param>
            /// <returns></returns>
            public bool MoveBezierBySmallAmount(float amount) {
                if(amount < 0) {
                    return MoveBezierBackwardBySmallAmount(-amount);
                }
                var der = CurrentDerivative;
                float derMag = der.magnitude;
                var movePercentage = amount / derMag;
                bool result = MoveForwardByPercent(movePercentage);
                Leftover = Leftover * derMag;
                return result;
            }

            private bool MoveBezierBackwardBySmallAmount(float amount) {
                var der = CurrentDerivative;
                float derMag = der.magnitude;
                var movePercentage = amount / derMag;
                var result = MoveBackwardByPercent(movePercentage);
                Leftover = Leftover * derMag;
                return result;
            }

            /// <summary>
            /// Moves backward on the path. Speed is vary with the length of the segment. Use MoveBackward for homogeneous speed.
            /// </summary>
            /// <param name="percent"></param>
            /// <returns></returns>
            public bool MoveBackwardByPercent(float percent) {
                if (!_initialized) {
                    _initialized = true;
                    return true;
                }
                while (true) {
                    var leftToTraverse = _progress;
                    if (leftToTraverse > percent) {
                        _progress -= percent;
                        Leftover = 0f;
                        return true;
                    }
                    else {
                        percent -= leftToTraverse;
                        if (!TryDecrementIndex()) {
                            Leftover = percent;
                            _progress = 0f;
                            return false;
                        }
                        _progress = 1f;
                    }
                }
            }
            public void Reset() {
                this = new SegmentEnumerator(_path, _movement);
            }
            
            /// <summary>
            /// Rolls the enumerator to the end of the path.
            /// </summary>
            public void RollForth()
            {
                _index = _path._points.Count - 1;
                if(!_path.closed)
                    _index -= 1;
                _progress = 1;
            }
            
            private float CurrentSegmentLength() {
                var arr = _path.Points.array;
                var p1 = arr[_index];
                Point p2;
                if(_index + 1 == _path._points.Count) {
                    p2 = arr[0];
                }
                else{
                    p2 = arr[_index + 1];
                }
                float distance = EstimateCurveLength(p1.position, p1.rightControl, p2.leftControl, p2.position);
                return distance;
            }
            /// <summary>
            /// Calculates the total length of the path and resets the enumerator afterwards.
            /// </summary>
            /// <returns></returns>
            public float CalculateTotalLength() {
                var upperLimit = _path.closed ? _path._points.Count : _path._points.Count - 1;
                float totalLength = 0;
                for ( _index = 0; _index < upperLimit; _index++) {
                    totalLength += CurrentSegmentLength();
                }
                Reset();
                return totalLength;
            }
        }

        public enum NormalAlgorithmType {
            None = 0,
            ConstantVector = 1,
            TangentAlgorithm = 2,
            IterativeReflectionAlgorithm = 3,
        }
        public struct NormalModule {
            

            private const int CacheDensity = 20; //cached points per index

            private ExposedList<Vector3> _normalsCache;
            
            private BezierPath3D _cachedPath;


            //private void EnsureInitialization() {
            //    if (_normalsCache == null) {
            //        _normalsCache = new ExposedList<Vector3>(CacheDensity);
            //    }
            //}

            public Vector3 GetNormal(BezierPath3D path, int index, float progress) {
                if (path.normalAlgorithm == NormalAlgorithmType.None) return default;
                var normal = GetNormalFromAlgorithm(path, index, progress);
                return RotateNormalForUserAngles(path, index, progress, normal);
            }

            private Vector3 RotateNormalForUserAngles(BezierPath3D path, int index, float progress, Vector3 normal) {
                float angle = path.EvaluateNormalOffset(index, progress);
                var tangent = path.EvaluateDirection(index, progress);
                var rot = Quaternion.AngleAxis (angle, tangent);
                return (rot * normal * ((path.flipNormals) ? -1 : 1)).normalized;
            }

            private Vector3 GetNormalFromAlgorithm(BezierPath3D path, int index, float progress) {
                switch (path.normalAlgorithm) {
                    case NormalAlgorithmType.None:
                        return Vector3.up;
                    case NormalAlgorithmType.ConstantVector:
                        return path.constantVector;
                    case NormalAlgorithmType.TangentAlgorithm:
                        return EvaluateTangentAlgorithm(path, index, progress);
                    case NormalAlgorithmType.IterativeReflectionAlgorithm:
                        return EvaluateIterativeReflectionAlgorithm(path, index, progress);
                    default:
                        return default;
                }
            }

            private Vector3 EvaluateTangentAlgorithm(BezierPath3D path, int index, float progress) {
                // -> Tangent Direction Algorithm
                // on the tangent and vector3.up plane, get normal of tangent
                var tangent = path.EvaluateDirection(index, progress);
                var planeNormal = Vector3.Cross(tangent, path.constantVector);
                return Vector3.Cross(planeNormal, tangent).normalized;
            }

            public void ClearCache() {
                _cachedPath = null;
            }
            
            private void EnsureCacheOfIterativeReflectionAlgorithm(BezierPath3D path) {
                if (_cachedPath == path) {
                    return;
                }
                _cachedPath = path;
                
                var lastRotationAxis = Vector3.up;
                if (_normalsCache == null) {
                    _normalsCache = new ExposedList<Vector3>();
                }
                else {
                    _normalsCache.ClearFast();
                }
                var e = path.GetEnumerator(1f / CacheDensity);
                e.MoveForward(0);
                var currentTangent = e.CurrentDirection;
                var currentPoint = e.CurrentPoint;
                var firstTangent = currentTangent;
                var previousTangent = firstTangent;
                var previousPoint = currentPoint;
                _normalsCache.Add(Vector3.Cross(lastRotationAxis, currentTangent).normalized);
                while (e.MoveForwardByPercent(1f / CacheDensity)) {
                    // First reflection
                    currentPoint = e.CurrentPoint;
                    currentTangent = e.CurrentDirection;
                    var offset = (currentPoint - previousPoint);
                    var sqrDst = offset.sqrMagnitude;
                    var lastRotationReflectionOverOffset = lastRotationAxis - offset * 2 / sqrDst * Vector3.Dot(offset, lastRotationAxis);
                    var localTangentReflectionOverOffset = previousTangent - offset * 2 / sqrDst * Vector3.Dot(offset, previousTangent);
                    
                    // Second reflection
                    var v2 = currentTangent - localTangentReflectionOverOffset;
                    var c2 = Vector3.Dot(v2, v2);
                    var finalRot = lastRotationReflectionOverOffset - v2 * 2 / c2 * Vector3.Dot(v2, lastRotationReflectionOverOffset);
                    
                    var n = Vector3.Cross(finalRot, currentTangent).normalized;
                    lastRotationAxis = finalRot;
                    _normalsCache.Add(n);
                    previousPoint = currentPoint;
                    previousTangent = currentTangent;
                }
                e.Dispose();

                // Apply correction for 3d normals along a closed path
                if (path.closed) {
                    // Get angle between first and last normal (if zero, they're already lined up, otherwise we need to correct)
                    float normalsAngleErrorAcrossJoin = Vector3.SignedAngle (_normalsCache.array[_normalsCache.Count - 1], _normalsCache.array[0], firstTangent);
                    // Gradually rotate the normals along the path to ensure start and end normals line up correctly
                    if (Mathf.Abs (normalsAngleErrorAcrossJoin) > 0.1f) // don't bother correcting if very nearly correct
                    {
                        for (int i = 1; i <  _normalsCache.Count; i++) {
                            float t = (i / (_normalsCache.Count - 1f));    
                            float angle = normalsAngleErrorAcrossJoin * t;
                            var tangent = path.EvaluateDirection(i / CacheDensity, ((float)i / CacheDensity) % 1);
                            var rot = Quaternion.AngleAxis (angle, tangent);
                            _normalsCache.array[i] = (rot * _normalsCache.array[i]).normalized;
                            // NormalsCache.array[i] = (rot * NormalsCache.array[i] * ((path.flipNormals) ? -1 : 1)).normalized;
                        }
                    }
                }
            }

            public Vector3 EvaluateIterativeReflectionAlgorithm(BezierPath3D path, int index, float progress) {
                EnsureCacheOfIterativeReflectionAlgorithm(path);
                
                // get closest 2 cached points, interpolate between them
                int cacheIndex1 = (index * CacheDensity) + (int)Mathf.Floor(progress * CacheDensity);
                int cacheIndex2 = (index * CacheDensity) + (int)Mathf.Ceil(progress * CacheDensity);
                var f = (progress * CacheDensity) % 1;
                var normal = (_normalsCache.array[cacheIndex1] * (1 - f)) + (_normalsCache.array[cacheIndex2] * (f));
                return normal.normalized;
            }

        }

        public struct SegmentInfo {
            public float progress;
            public int index;

            public SegmentInfo(int index, float progress) {
                this.progress = progress;
                this.index = index;
            }
        }
        public enum ControlMode { Aligned = 2, Mirrored = 1, Free = 3, Automatic = 0 };
    }
}