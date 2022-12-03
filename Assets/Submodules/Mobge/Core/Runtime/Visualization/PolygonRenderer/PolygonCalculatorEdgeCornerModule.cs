using System.Collections;
using System.Collections.Generic;
using Mobge;
using Mobge.Core.Components;
using UnityEngine;

namespace Mobge {
    public struct PolygonCalculatorEdgeCornerModule {
        public static PolygonCalculatorEdgeCornerModule New() {
            var p = new PolygonCalculatorEdgeCornerModule();
            p._calculationDataCache = new ExposedList<CalculationCachePoint>();
            p._calculatedLineSegments = new ExposedList<LineSegment>();
            p._edgeSpritesToDraw = new ExposedList<Sprite>();
            p._uvMapCache = new Vector2[4];
            return p;
        }

        #region Cache Data

        private ExposedList<CalculationCachePoint> _calculationDataCache;
        private ExposedList<LineSegment> _calculatedLineSegments;
        private ExposedList<Sprite> _edgeSpritesToDraw;
        private Vector2[] _uvMapCache;
        private float _polygonSkinScale;
        private Color _polygonColor;
        private PolygonVisualizer _visualizer;
        private MeshBuilder _meshBuilder;

        private enum CornerEdgeType {
            Unassigned = 0,
            TopEdge = 1,
            LeftEdge = 2,
            RightEdge = 3,
            BottomEdge = 4,
            TopLeftCorner = 5,
            TopRightCorner = 6,
            BottomLeftCorner = 7,
            BottomRightCorner = 8,
        }

        private struct CalculationCachePoint {
            private readonly ExposedList<CalculationCachePoint> _list;
            public readonly int nextPointIndex;
            public readonly int prevPointIndex;

            public readonly Vector2 position;
            public CornerEdgeType cornerEdgeType;
            public bool isCorner;
            public bool isOuterCorner;
            public bool isDummyCorner;

            public Vector2 nextVertex;
            public Vector2 outerIntersectVertex;
            public Vector2 innerIntersectVertex;
            public Vector2 prevVertex;
            public float nextEdgeStartDistance;
            public float prevEdgeStartDistance;
            public Vector2 nextEdgeStartNormal;
            public Vector2 prevEdgeEndNormal;
            public Sprite cornerSprite;

            public ref CalculationCachePoint NextPoint => ref _list.array[nextPointIndex];
            public ref CalculationCachePoint PrevPoint => ref _list.array[prevPointIndex];
            public Vector2 NextNormal => Vector2.Perpendicular(NextVector).normalized;
            public Vector2 PrevNormal => Vector2.Perpendicular(PrevVector).normalized;
            public Vector2 Normal => (PrevNormal + NextNormal).normalized;
            public float MiddleAngle => Vector2.SignedAngle(PrevVector, NextVector);
            public Vector2 NextVector => NextPoint.position - position;
            private Vector2 PrevVector => position - PrevPoint.position;
            public float DistanceToNextPoint => Vector2.Distance(position, NextPoint.position);

            public CalculationCachePoint(ExposedList<CalculationCachePoint> list, Vector2 position, int prevPointIndex,
                int nextPointIndex) {
                _list = list;
                this.position = position;
                this.prevPointIndex = prevPointIndex;
                this.nextPointIndex = nextPointIndex;

                cornerSprite = null;
                nextVertex = Vector2.zero;
                outerIntersectVertex = Vector2.zero;
                innerIntersectVertex = Vector2.zero;
                prevVertex = Vector2.zero;
                cornerEdgeType = CornerEdgeType.Unassigned;
                isOuterCorner = false;
                isCorner = false;
                isDummyCorner = false;
                nextEdgeStartDistance = 0f;
                prevEdgeStartDistance = 0f;
                nextEdgeStartNormal = Vector2.zero;
                prevEdgeEndNormal = Vector2.zero;
            }
        }

        private struct CalculationDataCacheEnumerator {
            private readonly ExposedList<CalculationCachePoint> _list;
            private bool _finished;
            private readonly int _startIndex;
            private readonly int _endIndex;
            private float _currentProgress;
            private float _currentTotalProgress;
            private readonly float _totalProgressLimit;
            private int _iterationLimit;

            public CalculationDataCacheEnumerator(ExposedList<CalculationCachePoint> list) : this() {
                _list = list;
                CurrentIndex = 0;
                _endIndex = 0;
                _iterationLimit = 5000;
                _finished = false;
                _currentProgress = 0f;
                _currentTotalProgress = 0f;
            }

            public CalculationDataCacheEnumerator(ExposedList<CalculationCachePoint> list, int startIndex, int endIndex) :
                this(list) {
                _endIndex = endIndex;
                CurrentIndex = startIndex;
            }

            public CalculationDataCacheEnumerator(ExposedList<CalculationCachePoint> list, int startIndex, int endIndex,
                float totalProgressLimit) : this(list, startIndex, endIndex) {
                _totalProgressLimit = totalProgressLimit;
            }

            public bool MoveByUnits(float desiredTravelDistance, out float actualTravelDistance) {
                if (_iterationLimit-- < 0) {
                    #if UNITY_EDITOR
                    Debug.LogError(
                        $"Iteration Limit reached at {nameof(PolygonCalculator)}.{nameof(CalculationDataCacheEnumerator)}.{nameof(MoveByUnits)}");
                    #endif
                    actualTravelDistance = 0f;
                    return false;
                }

                if (_currentTotalProgress >= _totalProgressLimit ||
                    MathExtensions.ApproximatelyClose(_currentTotalProgress, _totalProgressLimit)) {
                    actualTravelDistance = 0f;
                    return true;
                }

                var allowedTravelDistance = _totalProgressLimit - _currentProgress;
                if (desiredTravelDistance > allowedTravelDistance) {
                    desiredTravelDistance = allowedTravelDistance;
                }

                var thisSegmentLength = CurrentPoint.DistanceToNextPoint;
                var distanceToNextPoint = thisSegmentLength - _currentProgress;
                if (distanceToNextPoint > desiredTravelDistance &
                    !MathExtensions.ApproximatelyClose(distanceToNextPoint, desiredTravelDistance)) {
                    //advance progress
                    _currentProgress += desiredTravelDistance;
                    actualTravelDistance = desiredTravelDistance;
                    _currentTotalProgress += actualTravelDistance;
                }
                else {
                    //move to next point return progress by units
                    MoveToNextPoint();
                    _currentProgress = 0;
                    actualTravelDistance = distanceToNextPoint;
                    _currentTotalProgress += actualTravelDistance;
                }

                return true;
            }

            public bool ForceMoveByUnits(float distance) {
                while (true) {
                    if (_iterationLimit-- < 0) {
                        #if UNITY_EDITOR
                        Debug.LogError(
                            $"Iteration Limit reached at {nameof(PolygonCalculator)}.{nameof(CalculationDataCacheEnumerator)}.{nameof(ForceMoveByUnits)}");
                        #endif
                        return true;
                    }

                    var thisSegmentLength = CurrentPoint.DistanceToNextPoint;
                    if ((thisSegmentLength - _currentProgress) < distance) {
                        MoveToNextPoint();
                        distance -= thisSegmentLength - _currentProgress;
                        _currentProgress = 0f;
                        _currentTotalProgress += thisSegmentLength;
                    }
                    else {
                        _currentProgress = distance;
                        _currentTotalProgress += distance;
                        break;
                    }
                }

                return false;
            }

            public bool MoveToNextPoint() {
                if (_iterationLimit-- < 0) {
                    #if UNITY_EDITOR
                    Debug.LogError(
                        $"Iteration Limit reached at {nameof(PolygonCalculator)}.{nameof(CalculationDataCacheEnumerator)}.{nameof(MoveToNextPoint)}");
                    #endif
                    return false;
                }

                CurrentIndex = _list.array[CurrentIndex].nextPointIndex;
                if (CurrentIndex == _endIndex) {
                    _finished = true;
                }

                return !_finished;
            }

            public ref CalculationCachePoint CurrentPoint => ref _list.array[CurrentIndex];

            public Vector2 CurrentPosition =>
                CurrentPoint.position + (CurrentPoint.NextVector.normalized * _currentProgress);

            public Vector2 CurrentNormal => MathExtensions.ApproximatelyClose(_currentProgress, 0f)
                ? CurrentPoint.Normal
                : CurrentPoint.NextNormal;

            public int CurrentIndex { get; private set; }
            public float CurrentTotalProgress => _currentTotalProgress;
        }

        private struct LineSegment {
            private readonly ExposedList<CalculationCachePoint> _list;

            public int startIndex;
            public int endIndex;
            public CornerEdgeType edgeCornerEdgeType;
            public float length;

            public LineSegment(ExposedList<CalculationCachePoint> list) {
                _list = list;
                startIndex = 0;
                endIndex = 0;
                edgeCornerEdgeType = CornerEdgeType.Unassigned;
                length = 0f;
            }

            public float DrawStartOffset => _list.array[startIndex].nextEdgeStartDistance;
            public float DrawEndOffset => _list.array[endIndex].prevEdgeStartDistance;
            public float DrawLength => length - DrawStartOffset - DrawEndOffset;
        }

        #endregion

        public void DrawEdgesAndCorners(in Polygon polygon, PolygonVisualizer visualizer, MeshBuilder meshBuilder) {
            _visualizer = visualizer;
            _meshBuilder = meshBuilder;
            PopulateCalculationCache(in polygon);
            CalculateAllPointTypes();
            SetCornerSprites();
            CalculateAllCornerVertexes();
            CalculateLineSegments();
            EliminateTooShortLineSegments();
            DrawEdges();
            DrawCorners();
        }

        private void PopulateCalculationCache(in Polygon p) {
            _polygonSkinScale = p.skinScale;
            _calculationDataCache.ClearFast();
            int j = 0;
            if (Polygon.IsClockWise(in p)) {
                for (int i = 0; i < p.corners.Length; i++) {
                    var c = p.corners[i];
                    var prev = (j - 1 + p.corners.Length) % p.corners.Length;
                    var next = (j + 1) % p.corners.Length;
                    var calculationDataPoint = new CalculationCachePoint(
                        _calculationDataCache,
                        c.position,
                        prev,
                        next);
                    _calculationDataCache.Add(calculationDataPoint);
                    j++;
                }
            }
            else {
                for (int i = p.corners.Length - 1; i >= 0; i--) {
                    var c = p.corners[i];
                    var prev = (j - 1 + p.corners.Length) % p.corners.Length;
                    var next = (j + 1) % p.corners.Length;
                    var cp = new CalculationCachePoint(
                        _calculationDataCache,
                        c.position,
                        prev,
                        next);
                    _calculationDataCache.Add(cp);
                    j++;
                }
            }
        }

        private void CalculateAllPointTypes() {
            var e = new CalculationDataCacheEnumerator(_calculationDataCache);
            do {
                CalculatePointType(ref _calculationDataCache.array[e.CurrentIndex]);
            } while (e.MoveToNextPoint());
        }

        private void CalculatePointType(ref CalculationCachePoint point) {
            //if direction change, it is corner
            var prevVectorType = GetEdgeType(point.PrevNormal);
            var nextVectorCornerType = GetEdgeType(point.NextNormal);

            if (prevVectorType == nextVectorCornerType) {
                point.cornerEdgeType = prevVectorType;
            }
            else {
                point.isCorner = true;
                var absAngle = 180 - Mathf.Abs(point.MiddleAngle);
                if (absAngle < _visualizer.minimumCornerAngle || absAngle > _visualizer.maximumCornerAngle) {
                    point.cornerEdgeType = CornerEdgeType.Unassigned;
                    point.cornerSprite = null;
                    point.isDummyCorner = true;
                }
                else {
                    point.cornerEdgeType = GetCornerType(point.Normal);
                    point.isOuterCorner = point.MiddleAngle < 0f;
                }
            }
        }

        private void SetCornerSprites() {
            var e = new CalculationDataCacheEnumerator(_calculationDataCache);
            do {
                if (e.CurrentPoint.isCorner && !e.CurrentPoint.isDummyCorner) {
                    SetCornerSpriteForCorner(ref _calculationDataCache.array[e.CurrentIndex]);
                }
            } while (e.MoveToNextPoint());
        }

        private void SetCornerSpriteForCorner(ref CalculationCachePoint point) {
            var sprite = GetCornerSprite(_visualizer, point);
            if (sprite == null) {
                point.cornerEdgeType = CornerEdgeType.Unassigned;
                point.isDummyCorner = true;
                return;
            }

            point.cornerSprite = sprite;
        }

        private void CalculateAllCornerVertexes() {
            var e = new CalculationDataCacheEnumerator(_calculationDataCache);
            do {
                if (e.CurrentPoint.isCorner && !e.CurrentPoint.isDummyCorner) {
                    CalculateCornerVertexes(ref _calculationDataCache.array[e.CurrentIndex]);
                }
            } while (e.MoveToNextPoint());
        }

        private void CalculateCornerVertexes(ref CalculationCachePoint middlePoint) {
            var prevPoint = middlePoint.PrevPoint;
            var nextPoint = middlePoint.NextPoint;
            var prevNormal = middlePoint.PrevNormal;
            var nextNormal = middlePoint.NextNormal;
            if (!middlePoint.isOuterCorner) {
                prevNormal *= -1;
                nextNormal *= -1;
            }

            var sprite = middlePoint.cornerSprite;
            var normalDifferenceAngle = Vector2.Angle(nextNormal, prevNormal);
            var closenessToNormal = normalDifferenceAngle > 40 ? 1 : 0; // todo: this can be improved

            var spritePivotDistance = GetSpriteNextPrevPivotDistance(_visualizer, sprite, middlePoint.cornerEdgeType,
                middlePoint.isOuterCorner, _polygonSkinScale, _visualizer.globalScale);
            var sizeCenter = (spritePivotDistance.x + spritePivotDistance.y) * .5f;
            spritePivotDistance.x = Mathf.Lerp(sizeCenter, spritePivotDistance.x, closenessToNormal);
            spritePivotDistance.y = Mathf.Lerp(sizeCenter, spritePivotDistance.y, closenessToNormal);

            // inner vertex position intersection calculation
            var l1S = middlePoint.position + (-nextNormal * spritePivotDistance.y);
            var l1E = nextPoint.position + (-nextNormal * spritePivotDistance.y);
            var l2S = middlePoint.position + (-prevNormal * spritePivotDistance.x);
            var l2E = prevPoint.position + (-prevNormal * spritePivotDistance.x);
            var innerIntersect = GeometryUtils.LineIntersectionPoint(l1S, l1E, l2S, l2E);
            middlePoint.innerIntersectVertex = innerIntersect;

            // next vertex, prev vertex position calculation
            var spriteSize = GetSpriteNextPrevSize(sprite, middlePoint.cornerEdgeType, _polygonSkinScale,
                _visualizer.globalScale);
            sizeCenter = (spriteSize.x + spriteSize.y) * .5f;
            spriteSize.x = Mathf.Lerp(sizeCenter, spriteSize.x, closenessToNormal);
            spriteSize.y = Mathf.Lerp(sizeCenter, spriteSize.y, closenessToNormal);
            middlePoint.nextVertex = innerIntersect + (nextNormal * spriteSize.y);
            middlePoint.prevVertex = innerIntersect + (prevNormal * spriteSize.x);

            // outer vertex position intersection calculation
            l1S = middlePoint.position + (nextNormal * (spriteSize.y - spritePivotDistance.y));
            l1E = nextPoint.position + (nextNormal * (spriteSize.y - spritePivotDistance.y));
            l2S = middlePoint.position + (prevNormal * (spriteSize.x - spritePivotDistance.x));
            l2E = prevPoint.position + (prevNormal * (spriteSize.x - spritePivotDistance.x));
            middlePoint.outerIntersectVertex = GeometryUtils.LineIntersectionPoint(l1S, l1E, l2S, l2E);

            var prevOffsetPosition = innerIntersect + (prevNormal * spritePivotDistance.x);
            middlePoint.prevEdgeStartDistance = Vector2.Distance(middlePoint.position, prevOffsetPosition);

            var nextOffsetPosition = innerIntersect + (nextNormal * spritePivotDistance.y);
            middlePoint.nextEdgeStartDistance = Vector2.Distance(middlePoint.position, nextOffsetPosition);

            middlePoint.prevEdgeEndNormal = (middlePoint.prevVertex - middlePoint.innerIntersectVertex).normalized;
            middlePoint.nextEdgeStartNormal = (middlePoint.nextVertex - middlePoint.innerIntersectVertex).normalized;
            if (!middlePoint.isOuterCorner) {
                middlePoint.prevEdgeEndNormal *= -1f;
                middlePoint.nextEdgeStartNormal *= -1f;
            }
        }

        private void CalculateLineSegments() {
            _calculatedLineSegments.ClearFast();
            var ls = new LineSegment(_calculationDataCache);
            var e = new CalculationDataCacheEnumerator(_calculationDataCache);
            var started = false;
            var stitchNeeded = false;

            //Loop through all points making line segments
            if (!e.CurrentPoint.isCorner) {
                started = true;
                stitchNeeded = true;
                ls.startIndex = e.CurrentIndex;
                ls.edgeCornerEdgeType = GetEdgeType(e.CurrentPoint.NextNormal);
                ls.length = 0f;
            }

            do {
                if (started && e.CurrentPoint.isCorner) {
                    started = false;
                    ls.endIndex = e.CurrentIndex;
                    _calculatedLineSegments.Add(ls);
                }

                if (!started && e.CurrentPoint.isCorner) {
                    started = true;
                    ls.startIndex = e.CurrentIndex;
                    ls.edgeCornerEdgeType = GetEdgeType(e.CurrentPoint.NextNormal);
                    ls.length = 0f;
                }

                ls.length += e.CurrentPoint.DistanceToNextPoint;
            } while (e.MoveToNextPoint());

            if (started) {
                ls.endIndex = e.CurrentIndex;
                _calculatedLineSegments.Add(ls);
            }

            //if first and last segment can merge, merge it
            if (stitchNeeded) {
                _calculatedLineSegments.array[0].startIndex =
                    _calculatedLineSegments.array[_calculatedLineSegments.Count - 1].startIndex;
                _calculatedLineSegments.array[0].length +=
                    _calculatedLineSegments.array[_calculatedLineSegments.Count - 1].length;
                _calculatedLineSegments.RemoveLastFast();
            }
        }

        private void EliminateTooShortLineSegments() {
            for (int i = _calculatedLineSegments.Count - 1; i >= 0; i--) {
                var ls = _calculatedLineSegments.array[i];
                var drawLength = ls.length - ls.DrawStartOffset - ls.DrawEndOffset;
                if (drawLength < _visualizer.minimumEdgeDrawLength) {
                    MergeCorners(ref _calculationDataCache.array[ls.startIndex],
                        ref _calculationDataCache.array[ls.endIndex]);
                    _calculatedLineSegments.RemoveAt(i);
                }
            }
        }

        private void MergeCorners(ref CalculationCachePoint startCorner,
            ref CalculationCachePoint endCorner) {
            if (startCorner.isDummyCorner || endCorner.isDummyCorner) return;

            Vector2 leftPosition =
                startCorner.isOuterCorner ? startCorner.nextVertex : startCorner.innerIntersectVertex;
            leftPosition += endCorner.isOuterCorner ? endCorner.prevVertex : endCorner.innerIntersectVertex;
            leftPosition *= .5f;

            Vector2 rightPosition =
                startCorner.isOuterCorner ? startCorner.innerIntersectVertex : startCorner.nextVertex;
            rightPosition += endCorner.isOuterCorner ? endCorner.innerIntersectVertex : endCorner.prevVertex;
            rightPosition *= .5f;

            if (startCorner.isOuterCorner) {
                startCorner.nextVertex = leftPosition;
                startCorner.innerIntersectVertex = rightPosition;
            }
            else {
                startCorner.innerIntersectVertex = leftPosition;
                startCorner.nextVertex = rightPosition;
            }

            if (endCorner.isOuterCorner) {
                endCorner.prevVertex = leftPosition;
                endCorner.innerIntersectVertex = rightPosition;
            }
            else {
                endCorner.innerIntersectVertex = leftPosition;
                endCorner.prevVertex = rightPosition;
            }

            // start point, end point edge offsets has changed with this setting, plan accordingly
            var prevOffsetPosition = GeometryUtils.LineIntersectionPoint(
                startCorner.prevVertex, startCorner.innerIntersectVertex,
                startCorner.position, startCorner.PrevPoint.position);
            startCorner.prevEdgeStartDistance = Vector2.Distance(startCorner.position, prevOffsetPosition);

            var nextOffsetPosition = GeometryUtils.LineIntersectionPoint(
                endCorner.nextVertex, endCorner.innerIntersectVertex,
                endCorner.position, endCorner.NextPoint.position);
            endCorner.nextEdgeStartDistance = Vector2.Distance(endCorner.position, nextOffsetPosition);

            startCorner.prevEdgeEndNormal = (startCorner.prevVertex - startCorner.innerIntersectVertex).normalized;
            if (!startCorner.isOuterCorner) {
                startCorner.prevEdgeEndNormal *= -1f;
            }

            endCorner.nextEdgeStartNormal = (endCorner.nextVertex - endCorner.innerIntersectVertex).normalized;
            if (!endCorner.isOuterCorner) {
                endCorner.nextEdgeStartNormal *= -1f;
            }

        }



        private void DrawEdges() {
            for (int i = 0; i < _calculatedLineSegments.Count; i++) {
                DrawLineSegment(in _calculatedLineSegments.array[i]);
            }
        }

        private void DrawLineSegment(in LineSegment ls) {
            var edgeType = ls.edgeCornerEdgeType;

            var stretch = PickEdgeSpritesToDraw(ls);
            if (_edgeSpritesToDraw.Count <= 0) return;

            var e = new CalculationDataCacheEnumerator(_calculationDataCache, ls.startIndex, ls.endIndex,
                ls.length - ls.DrawEndOffset);
            e.ForceMoveByUnits(ls.DrawStartOffset);
            for (int i = 0; i < _edgeSpritesToDraw.Count; i++) {
                DrawEdgeSprite(ref e, in ls, _edgeSpritesToDraw.array[i], stretch, edgeType);
            }
        }

        private float PickEdgeSpritesToDraw(in LineSegment ls) {
            var drawLength = ls.DrawLength;

            // pick appropriate edge sprites from sprite list
            // randomize using point local position, so it draws same polygon data with same random numbers
            _edgeSpritesToDraw.ClearFast();
            var pickedSpriteLength = 0f;
            var e = new CalculationDataCacheEnumerator(_calculationDataCache, ls.startIndex, ls.endIndex);
            e.ForceMoveByUnits(ls.DrawStartOffset);
            do {
                var sprite = GetEdgeSprite(_visualizer, ls.edgeCornerEdgeType, e.CurrentPosition);
                if (sprite == null) break;
                _edgeSpritesToDraw.Add(sprite);
                var spriteLength = GetSpriteNextPrevSize(sprite, ls.edgeCornerEdgeType, _polygonSkinScale,
                    _visualizer.globalScale).x;
                if (spriteLength <= 0f) break;
                pickedSpriteLength += spriteLength;
                if (e.ForceMoveByUnits(spriteLength)) break;
            } while (pickedSpriteLength < drawLength);

            var stretch = drawLength / pickedSpriteLength;
            stretch = Mathf.Max(stretch, _visualizer.edgeSpriteMinimumStretchValue);

            return stretch;
        }

        private void DrawEdgeSprite(ref CalculationDataCacheEnumerator e,
            in LineSegment ls, Sprite sprite, float stretch, CornerEdgeType edgeCornerEdgeType) {
            var size = GetSpriteNextPrevSize(sprite, edgeCornerEdgeType, _polygonSkinScale, _visualizer.globalScale);
            var pivotDistance = GetSpriteNextPrevPivotDistance(_visualizer, sprite, edgeCornerEdgeType, true,
                _polygonSkinScale,
                _visualizer.globalScale);
            var drawLength = size.x * stretch;
            float remainingDistance = drawLength;
            var currentSpriteTravelPercent = 0f;
            var normal = GetCurrentNormal(ref e, in ls);
            _meshBuilder.vertices.Add(GetEdgeVertex(e.CurrentPosition + (-normal * pivotDistance.y)));
            _meshBuilder.vertices.Add(GetEdgeVertex(e.CurrentPosition + (normal * (size.y - pivotDistance.y))));
            _meshBuilder.uvs.Add(GetLowerSpriteSliceUVCoord(sprite, currentSpriteTravelPercent));
            _meshBuilder.uvs.Add(GetUpperSpriteSliceUVCoord(sprite, currentSpriteTravelPercent));
            while (remainingDistance > 0) {
                if (!e.MoveByUnits(remainingDistance, out var actualTravelDistance)) break;
                if (actualTravelDistance <= 0f) break;
                remainingDistance -= actualTravelDistance;
                currentSpriteTravelPercent = (drawLength - remainingDistance) / drawLength;
                var triangleIndex = _meshBuilder.vertices.Count;
                normal = GetCurrentNormal(ref e, in ls);
                _meshBuilder.vertices.Add(GetEdgeVertex(e.CurrentPosition + (-normal * pivotDistance.y)));
                _meshBuilder.vertices.Add(GetEdgeVertex(e.CurrentPosition + (normal * (size.y - pivotDistance.y))));
                _meshBuilder.uvs.Add(GetLowerSpriteSliceUVCoord(sprite, currentSpriteTravelPercent));
                _meshBuilder.uvs.Add(GetUpperSpriteSliceUVCoord(sprite, currentSpriteTravelPercent));
                _meshBuilder.AddTriangle(triangleIndex - 2, triangleIndex - 1, triangleIndex + 0);
                _meshBuilder.AddTriangle(triangleIndex + 0, triangleIndex - 1, triangleIndex + 1);
            }
        }

        private Vector2 GetCurrentNormal(ref CalculationDataCacheEnumerator e,
            in LineSegment ls) {
            var startPoint = _calculationDataCache.array[ls.startIndex];
            if (MathExtensions.ApproximatelyClose(e.CurrentTotalProgress, ls.DrawStartOffset) &&
                !startPoint.isDummyCorner) {
                return startPoint.nextEdgeStartNormal;
            }

            var endPoint = _calculationDataCache.array[ls.endIndex];
            if (MathExtensions.ApproximatelyClose(e.CurrentTotalProgress, ls.length - ls.DrawEndOffset) &&
                !endPoint.isDummyCorner) {
                return endPoint.prevEdgeEndNormal;
            }

            return e.CurrentNormal;
        }

        private void DrawCorners() {
            var e = new CalculationDataCacheEnumerator(_calculationDataCache);
            do {
                if (e.CurrentPoint.isCorner) {
                    DrawCorner(_meshBuilder, e.CurrentPoint);
                }
            } while (e.MoveToNextPoint());
        }

        private void DrawCorner(MeshBuilder mb, CalculationCachePoint point) {
            if (point.cornerEdgeType == CornerEdgeType.Unassigned || point.cornerSprite == null) {
                return;
            }

            var ind = mb.vertices.Count;

            mb.vertices.Add(GetEdgeVertex(point.outerIntersectVertex));
            mb.vertices.Add(GetEdgeVertex(point.nextVertex));
            mb.vertices.Add(GetEdgeVertex(point.prevVertex));
            mb.vertices.Add(GetEdgeVertex(point.innerIntersectVertex));

            if (point.isOuterCorner) {
                mb.AddTriangle(ind + 0, ind + 3, ind + 2);
                mb.AddTriangle(ind + 0, ind + 1, ind + 3);
            }
            else {
                mb.AddTriangle(ind + 0, ind + 2, ind + 3);
                mb.AddTriangle(ind + 0, ind + 3, ind + 1);
            }

            var sprite = point.cornerSprite;
            MapCornerUV(mb, point, sprite);
        }

        private void MapCornerUV(MeshBuilder mb, CalculationCachePoint point, Sprite sprite) {
            _uvMapCache[0] = new Vector2(sprite.rect.xMin / sprite.texture.width,
                sprite.rect.yMax / sprite.texture.height);
            _uvMapCache[1] = new Vector2(sprite.rect.xMax / sprite.texture.width,
                sprite.rect.yMax / sprite.texture.height);
            _uvMapCache[2] = new Vector2(sprite.rect.xMin / sprite.texture.width,
                sprite.rect.yMin / sprite.texture.height);
            _uvMapCache[3] = new Vector2(sprite.rect.xMax / sprite.texture.width,
                sprite.rect.yMin / sprite.texture.height);
            if (point.isOuterCorner) {
                switch (point.cornerEdgeType) {
                    case CornerEdgeType.TopLeftCorner:
                        mb.uvs.Add(_uvMapCache[0]);
                        mb.uvs.Add(_uvMapCache[1]);
                        mb.uvs.Add(_uvMapCache[2]);
                        mb.uvs.Add(_uvMapCache[3]);
                        break;
                    case CornerEdgeType.TopRightCorner:
                        mb.uvs.Add(_uvMapCache[1]);
                        mb.uvs.Add(_uvMapCache[3]);
                        mb.uvs.Add(_uvMapCache[0]);
                        mb.uvs.Add(_uvMapCache[2]);
                        break;
                    case CornerEdgeType.BottomLeftCorner:
                        mb.uvs.Add(_uvMapCache[1]);
                        mb.uvs.Add(_uvMapCache[3]);
                        mb.uvs.Add(_uvMapCache[0]);
                        mb.uvs.Add(_uvMapCache[2]);
                        break;
                    case CornerEdgeType.BottomRightCorner:
                        mb.uvs.Add(_uvMapCache[0]);
                        mb.uvs.Add(_uvMapCache[1]);
                        mb.uvs.Add(_uvMapCache[2]);
                        mb.uvs.Add(_uvMapCache[3]);
                        break;
                    default:
                        return;
                }
            }
            else {
                switch (point.cornerEdgeType) {
                    case CornerEdgeType.TopLeftCorner:
                        mb.uvs.Add(_uvMapCache[3]);
                        mb.uvs.Add(_uvMapCache[1]);
                        mb.uvs.Add(_uvMapCache[2]);
                        mb.uvs.Add(_uvMapCache[0]);
                        break;
                    case CornerEdgeType.TopRightCorner:
                        mb.uvs.Add(_uvMapCache[2]);
                        mb.uvs.Add(_uvMapCache[3]);
                        mb.uvs.Add(_uvMapCache[0]);
                        mb.uvs.Add(_uvMapCache[1]);
                        break;
                    case CornerEdgeType.BottomLeftCorner:
                        mb.uvs.Add(_uvMapCache[2]);
                        mb.uvs.Add(_uvMapCache[3]);
                        mb.uvs.Add(_uvMapCache[0]);
                        mb.uvs.Add(_uvMapCache[1]);
                        break;
                    case CornerEdgeType.BottomRightCorner:
                        mb.uvs.Add(_uvMapCache[3]);
                        mb.uvs.Add(_uvMapCache[1]);
                        mb.uvs.Add(_uvMapCache[2]);
                        mb.uvs.Add(_uvMapCache[0]);
                        break;
                    default:
                        return;
                }
            }
        }


        #region Helper Methods

        private static Sprite GetEdgeSprite(PolygonVisualizer visualizer, CornerEdgeType cornerEdgeType, Vector2 pos) {
            switch (cornerEdgeType) {
                case CornerEdgeType.TopEdge:    return GetRandomFromArray(visualizer.topEdgeSprites, pos);
                case CornerEdgeType.LeftEdge:   return GetRandomFromArray(visualizer.leftEdgeSprites, pos);
                case CornerEdgeType.RightEdge:  return GetRandomFromArray(visualizer.rightEdgeSprites, pos);
                case CornerEdgeType.BottomEdge: return GetRandomFromArray(visualizer.bottomEdgeSprites, pos);
                default:                        return null;
            }
        }

        private static Sprite GetCornerSprite(PolygonVisualizer visualizer, CalculationCachePoint point) {
            switch (point.cornerEdgeType) {
                case CornerEdgeType.TopLeftCorner:
                    return point.isOuterCorner
                        ? GetRandomFromArray(visualizer.topLeftCornerSprites, point.position)
                        : GetRandomFromArray(visualizer.topInnerLeftCornerSprites, point.position);
                case CornerEdgeType.TopRightCorner:
                    return point.isOuterCorner
                        ? GetRandomFromArray(visualizer.topRightCornerSprites, point.position)
                        : GetRandomFromArray(visualizer.topInnerRightCornerSprites, point.position);
                case CornerEdgeType.BottomLeftCorner:
                    return point.isOuterCorner
                        ? GetRandomFromArray(visualizer.bottomLeftCornerSprites, point.position)
                        : GetRandomFromArray(visualizer.bottomInnerLeftCornerSprites, point.position);
                case CornerEdgeType.BottomRightCorner:
                    return point.isOuterCorner
                        ? GetRandomFromArray(visualizer.bottomRightCornerSprites, point.position)
                        : GetRandomFromArray(visualizer.bottomInnerRightCornerSprites, point.position);
                case CornerEdgeType.Unassigned: return null;
                default:                        return null;
            }
        }

        private static Sprite GetRandomFromArray(Sprite[] array, Vector2 pos) {
            if (array.Length <= 0) return null;
            int x = (int) (pos.x * 10) + (int) (pos.y * 10);
            x = Mathf.Abs(x);
            x %= array.Length;
            return array[x];
        }

        private static CornerEdgeType GetEdgeType(Vector2 normal) {
            var normalAngle = Vector2.SignedAngle(normal, Vector3.right) + 180f;
            var val = (normalAngle + 45f) % 360f;
            if (val < 90f) {
                return CornerEdgeType.LeftEdge;
            }

            if (val < 180f) {
                return CornerEdgeType.TopEdge;
            }

            if (val < 270f) {
                return CornerEdgeType.RightEdge;
            }

            return CornerEdgeType.BottomEdge;
        }

        private static CornerEdgeType GetCornerType(Vector2 normal) {
            var normalAngle = Vector2.SignedAngle(normal, Vector3.right) + 180f;
            if (normalAngle < 90f) {
                return CornerEdgeType.TopLeftCorner;
            }

            if (normalAngle < 180f) {
                return CornerEdgeType.TopRightCorner;
            }

            if (normalAngle < 270f) {
                return CornerEdgeType.BottomRightCorner;
            }

            return CornerEdgeType.BottomLeftCorner;
        }

        private static float GetEdgeOffsetValue(PolygonVisualizer visualizer, float polygonSkinScale,
            CornerEdgeType cornerEdgeType) {
            float offset;
            switch (cornerEdgeType) {
                case CornerEdgeType.TopEdge:
                    offset = visualizer.topEdgeOffset;
                    break;
                case CornerEdgeType.LeftEdge:
                    offset = visualizer.leftEdgeOffset;
                    break;
                case CornerEdgeType.RightEdge:
                    offset = visualizer.rightEdgeOffset;
                    break;
                case CornerEdgeType.BottomEdge:
                    offset = visualizer.bottomEdgeOffset;
                    break;
                default: return 0f;
            }

            return offset;
        }

        private static Vector2 GetSpriteNextPrevPivotDistance(PolygonVisualizer visualizer, Sprite sprite,
            CornerEdgeType cornerEdgeType,
            bool isOuterCorner, float polygonSkinScale, float globalScale) {
            var sizeX = sprite.rect.size.x;
            var sizeY = sprite.rect.size.y;
            var pivotX = sprite.pivot.x;
            var pivotY = sprite.pivot.y;

            //map pivot
            float next;
            float prev;
            switch (cornerEdgeType) {
                case CornerEdgeType.TopLeftCorner:
                    if (isOuterCorner) {
                        next = sizeX - pivotX;
                        prev = pivotY;
                    }
                    else {
                        next = sizeY - pivotY;
                        prev = pivotX;
                    }

                    break;
                case CornerEdgeType.TopRightCorner:
                    if (isOuterCorner) {
                        next = pivotY;
                        prev = pivotX;
                    }
                    else {
                        next = sizeX - pivotX;
                        prev = sizeY - pivotY;
                    }

                    break;
                case CornerEdgeType.BottomLeftCorner:
                    if (isOuterCorner) {
                        next = pivotY;
                        prev = pivotX;
                    }
                    else {
                        next = pivotX;
                        prev = pivotY;
                    }

                    break;
                case CornerEdgeType.BottomRightCorner:
                    if (isOuterCorner) {
                        next = pivotX;
                        prev = sizeY - pivotY;
                    }
                    else {
                        next = pivotY;
                        prev = sizeX - pivotX;
                    }

                    break;
                default:
                    next = pivotX;
                    prev = pivotY;
                    break;
            }

            var nextPrevPivotDistance = new Vector2(next, prev);
            nextPrevPivotDistance *= (1 / sprite.pixelsPerUnit);
            if (polygonSkinScale >= .0001f) nextPrevPivotDistance *= polygonSkinScale;
            if (globalScale >= .0001f) nextPrevPivotDistance *= globalScale;

            //map offsets
            float nextOffset;
            float prevOffset;
            switch (cornerEdgeType) {
                case CornerEdgeType.TopLeftCorner:
                    if (isOuterCorner) {
                        nextOffset = -visualizer.leftEdgeOffset;
                        prevOffset = -visualizer.topEdgeOffset;
                    }
                    else {
                        nextOffset = visualizer.topEdgeOffset;
                        prevOffset = visualizer.leftEdgeOffset;
                    }

                    break;
                case CornerEdgeType.TopRightCorner:
                    if (isOuterCorner) {
                        nextOffset = -visualizer.topEdgeOffset;
                        prevOffset = -visualizer.rightEdgeOffset;
                    }
                    else {
                        nextOffset = visualizer.rightEdgeOffset;
                        prevOffset = visualizer.topEdgeOffset;
                    }

                    break;
                case CornerEdgeType.BottomLeftCorner:
                    if (isOuterCorner) {
                        nextOffset = -visualizer.bottomEdgeOffset;
                        prevOffset = -visualizer.leftEdgeOffset;
                    }
                    else {
                        nextOffset = visualizer.leftEdgeOffset;
                        prevOffset = visualizer.bottomEdgeOffset;
                    }

                    break;
                case CornerEdgeType.BottomRightCorner:
                    if (isOuterCorner) {
                        nextOffset = -visualizer.rightEdgeOffset;
                        prevOffset = -visualizer.bottomEdgeOffset;
                    }
                    else {
                        nextOffset = visualizer.bottomEdgeOffset;
                        prevOffset = visualizer.rightEdgeOffset;
                    }

                    break;
                default:
                    nextOffset = 0f;
                    prevOffset = -GetEdgeOffsetValue(visualizer, polygonSkinScale, cornerEdgeType);
                    break;
            }

            var size = GetSpriteNextPrevSize(sprite, cornerEdgeType, polygonSkinScale, globalScale);
            var offset = new Vector2(nextOffset * size.x, prevOffset * size.y);
            nextPrevPivotDistance += offset;

            return nextPrevPivotDistance;
        }

        private static Vector2 GetSpriteNextPrevSize(Sprite sprite, CornerEdgeType cornerEdgeType,
            float polygonSkinScale, float globalScale) {
            float next;
            float prev;

            switch (cornerEdgeType) {
                case CornerEdgeType.TopLeftCorner:
                    next = sprite.rect.size.x;
                    prev = sprite.rect.size.y;
                    break;
                case CornerEdgeType.TopRightCorner:
                    next = sprite.rect.size.y;
                    prev = sprite.rect.size.x;
                    break;
                case CornerEdgeType.BottomLeftCorner:
                    next = sprite.rect.size.y;
                    prev = sprite.rect.size.x;
                    break;
                case CornerEdgeType.BottomRightCorner:
                    next = sprite.rect.size.x;
                    prev = sprite.rect.size.y;
                    break;
                default:
                    next = sprite.rect.size.x;
                    prev = sprite.rect.size.y;
                    break;
            }

            var v = new Vector2(next, prev);
            v *= (1 / sprite.pixelsPerUnit);
            if (polygonSkinScale >= .0001f) v *= polygonSkinScale;
            if (globalScale >= .0001f) v *= globalScale;
            return v;
        }

        private static Vector2 GetUpperSpriteSliceUVCoord(Sprite sprite, float percent) {
            // if (percent > 0.99) {
            //     percent = 1;
            // }
            // if (percent < 0.01) {
            //     percent = 0;
            // }
            percent = Mathf.Clamp01(percent);

            return new Vector2(
                Mathf.Lerp(sprite.rect.xMax, sprite.rect.xMin, 1 - percent) / sprite.texture.width,
                sprite.rect.yMax / sprite.texture.height);
        }

        private static Vector2 GetLowerSpriteSliceUVCoord(Sprite sprite, float percent) {
            // if (percent > 0.99) {
            //     percent = 1;
            // }
            // if (percent < 0.01) {
            //     percent = 0;
            // }
            percent = Mathf.Clamp01(percent);

            return new Vector2(
                Mathf.Lerp(sprite.rect.xMax, sprite.rect.xMin, 1 - percent) / sprite.texture.width,
                sprite.rect.yMin / sprite.texture.height);
        }

        private Vector3 GetEdgeVertex(Vector2 vertexPosition) {
            if (_visualizer.joinInnerOuterAndWallMeshesIntoOneObject) {
                return new Vector3(vertexPosition.x, vertexPosition.y, _visualizer.edgeZOffset);
            }
            else {
                return new Vector3(vertexPosition.x, vertexPosition.y);
            }
        }

        #endregion Helper Methods
    }
}
