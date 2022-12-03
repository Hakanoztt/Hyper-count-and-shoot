using System;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Mobge {
    public class EBezierPath {
        private const float c_pointRadiusMultiplayer = 0.3f;
        private const float c_controlRadiusMultiplayer = 0.22f;
        private const float c_selectionRadiusMultiplayer = 0.3f;
        private const float c_maxInsertDistance = 1f;
        private static ArcHandle _anchorAngleHandle = new ArcHandle ();
        private BezierPath3D _path;
        private EditorTools _sceneTools;
        private readonly Selection _selection;
        private bool _edited = false;
        
        private bool Edited {
            get {
                if (!_edited) return false;
                _edited = false;
                return true;
            }
            set => _edited = value;
        }
        public EBezierPath() {
            _selection = new Selection(this);
            InitTools();
        }
        private void InitTools() {
            _sceneTools = new EditorTools();
            _sceneTools.AddTool(new EditorTools.Tool("select") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0,
                },
                onPress = _selection.Press,
                onDrag = _selection.OnDrag,
                onRelease = _selection.Release,
            });
            _sceneTools.AddTool(new EditorTools.Tool("select additive") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0,
                    modifiers = EventModifiers.Shift,
                },
                onPress = _selection.PressAdditive,
                onRelease = _selection.ReleaseAdditive,
            });
            _sceneTools.AddTool(new EditorTools.Tool("add remove point") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0,
                    modifiers = EventModifiers.Control,
                },
                onPress = AddRemovePress,
                onUpdate = AddRemoveUpdate,
                onRelease = AddRemoveRelease,
            });
        }
        private bool AddRemovePress() {
            _selection.dragging = true;
            var segment = FindAddRemovePoint(out bool add, out Vector3 position);
            if (add) {
                _path.Points.Insert(segment.index, new BezierPath3D.Point() {
                    position = position,
                    leftControl = position,
                    rightControl = position,
                });
                _selection.SingleSelection = new PointId(segment.index,PointType.Point);
            }
            else{
                _path.Points.RemoveAt(segment.index);
                _selection.ClearSelection();
            }
            Edited = true;
            return true;
        }
        private void AddRemoveUpdate() {
            if(_path.controlMode == BezierPath3D.ControlMode.Automatic) {
                _selection.OnDrag();
                return;
            }
            var se = _selection.SingleSelection;
            if(se.index >= 0){
                var ray = LocalMouseRay;
                var center = _path.Points.array[se.index].position;
                var point = NearestPointOnRay(ray, center);
                _path.Points.array[se.index].rightControl = point;
                _path.Points.array[se.index].leftControl = 2 * center - point;
                Edited = true;
            }
        }
        private void AddRemoveRelease() {
            _selection.dragging = false;
            _selection.ClearSelection();
        }
        private void DrawAddRemovePoint() {
            if (Event.current.type == EventType.Repaint) 
            {
                FindAddRemovePoint(out bool add, out Vector3 position);
                var color = add ? Color.green : Color.red;
                var size = GetSelectRadius(position) * c_pointRadiusMultiplayer;
                Handles.color = color;
                Handles.SphereHandleCap(0, position, Quaternion.identity, size, EventType.Repaint);
                SceneView.lastActiveSceneView.Repaint();
            }
        }
        private BezierPath3D.SegmentInfo FindAddRemovePoint(out bool add, out Vector3 position) {
            var p = FindPointUnderMouse(false);
            if(p.index >= 0) {
                add = false;
                position = _path.Points.array[p.index].position;
                return new BezierPath3D.SegmentInfo(p.index, 0);
            }
            var r = LocalMouseRay;
            if(_path.Points.Count <= 1){
                add = true;
                position = NearestPointOnRay(r, Vector3.zero);
                return new BezierPath3D.SegmentInfo(0,0);
            }
            var e = _path.GetEnumerator(1f);
            float minDistanceSqr = float.PositiveInfinity;
            BezierPath3D.SegmentInfo segment = new BezierPath3D.SegmentInfo(0, 0f);
            e.MoveForwardByPercent(0);
            position = e.CurrentPoint;
            do {
                var cp = e.CurrentPoint;
                var dsqr = RayToPointDistanceSqr(r, cp);
                if(dsqr < minDistanceSqr) {
                    position = cp;
                    minDistanceSqr = dsqr;
                    segment = e.Current;
                }
            }
            while (e.MoveForwardByPercent(0.1f));
            segment.index++;
            add = true;
            var maxInsertDistance = c_maxInsertDistance * GetSelectRadius(position);
            if(maxInsertDistance * maxInsertDistance < minDistanceSqr) {
                segment.index = _path.Points.Count;
                var lastPoint = segment.index == 0 ? Vector3.zero : _path.Points.Last.position;
                position = NearestPointOnRay(r, lastPoint);
            }
            return segment;
        }
        private void SetPath(BezierPath3D path) {
            if(_path != path) {
                _path = path;
            }
        }
        public bool OnInspectorGUI(BezierPath3D path) {
            EditorGUI.BeginChangeCheck();
            SetPath(path);
            path.Points.Trim();
            EditorGUI.BeginChangeCheck();
            path.controlMode = (BezierPath3D.ControlMode)EditorGUILayout.EnumPopup("mode", path.controlMode);
            path.normalAlgorithm = (BezierPath3D.NormalAlgorithmType)EditorGUILayout.EnumPopup("normal algorithm", path.normalAlgorithm);
            if(path.normalAlgorithm == BezierPath3D.NormalAlgorithmType.ConstantVector) {
                path.constantVector = EditorGUILayout.Vector3Field("constant vector", path.constantVector);
            }
            path.flipNormals = EditorGUILayout.Toggle("flip normals", path.flipNormals);
            path.closed = EditorGUILayout.Toggle("closed", path.closed);
            if(EditorGUI.EndChangeCheck()) {
                var arr = _path.Points.array;
                int count = _path.Points.Count;
                switch(path.controlMode) {
                    case BezierPath3D.ControlMode.Aligned:
                        for(int i = 0; i < count; i++) {
                            var val = arr[i];
                            UpdateNodeForAligned(ref val.rightControl, val.position, val.leftControl);
                            arr[i] = val;
                        }
                        break;
                    case BezierPath3D.ControlMode.Mirrored:
                        for(int i = 0; i < count; i++) {
                            var val = arr[i];
                            UpdateNodeForMirrored(ref val.rightControl, val.position, val.leftControl);
                            arr[i] = val;
                        }
                        break;
                    case BezierPath3D.ControlMode.Automatic:
                        _path.UpdateControlsForAuto();
                        break;
                }
            }
            return EditorGUI.EndChangeCheck();
        }
        private static void UpdateNodeForAligned(ref Vector3 controlPoint, in Vector3 center, in Vector3 otherControl) {
            controlPoint = NearestPointOnRay(new Ray(center, otherControl-center), controlPoint);
        }
        private static void UpdateNodeForMirrored(ref Vector3 controlPoint, in Vector3 center, in Vector3 otherControl) {
            controlPoint = 2 * center - otherControl;
        }
        private void UpdateNodeForCurrentMode(ref Vector3 controlPoint, in Vector3 center, in Vector3 otherControl) {
            switch(_path.controlMode) {
                case BezierPath3D.ControlMode.Aligned:
                UpdateNodeForAligned(ref controlPoint, center, otherControl);
                break;
                case BezierPath3D.ControlMode.Mirrored:
                UpdateNodeForMirrored(ref controlPoint, center, otherControl);
                break;
            }
        }
        public bool OnSceneGUI(BezierPath3D path) {
            SetPath(path);
            if(Event.current.type == EventType.Repaint){
                DrawBezier();
            }
            _selection.DoMoveHandle();
            _selection.DoNormalOffsetHandle();
            if((Event.current.modifiers & EventModifiers.Control) != 0 && !_selection.dragging){
                DrawAddRemovePoint();
            }
            var t = Event.current.type;
            bool edited = Edited || _selection.Edited || t == EventType.Used || t == EventType.MouseUp;
            if(edited) {
                if(_path.controlMode == BezierPath3D.ControlMode.Automatic){
                    _path.UpdateControlsForAuto();
                }
                BezierPath3D.s_normalModule.ClearCache();
            }
            _sceneTools.OnSceneGUI();
            edited = Edited || _selection.Edited || t == EventType.Used || t == EventType.MouseUp;
            return edited;
        }
        private void DrawBezier() {
            if(_path.Points.Count <= 1){
                return;
            }
            DrawBezierLine(_path);
            var arr = _path.Points.array;
            _selection.DrawSelection();
            for(int i = 0; i < _path.Points.Count; i++) {
                var node = arr[i];
                Handles.color = ColorMultiplier(_selection.IsInsideRect(node.position) ? new Color(1, 1, 1) : new Color(0.8f, 0.8f, 0.8f), new PointId(i, PointType.Point));
                DrawController(node.position, c_pointRadiusMultiplayer);

                if(_path.controlMode != BezierPath3D.ControlMode.Automatic) {
                    Handles.color = ColorMultiplier(new Color(0.8f,0.8f,0), new PointId(i, PointType.LeftControl));
                    DrawController(node.leftControl, c_controlRadiusMultiplayer);
                    Handles.DrawLine(node.position, node.leftControl);
                    
                    Handles.color = ColorMultiplier(new Color(0.8f,0.8f,0), new PointId(i, PointType.RightControl));
                    DrawController(node.rightControl, c_controlRadiusMultiplayer);
                    Handles.DrawLine(node.position, node.rightControl);
                }
            }
        }
        public void DrawBezierLine(BezierPath3D path) {
            SetPath(path);
            if(_path.Points.Count <= 1){
                return;
            }
            var e = _path.GetEnumerator(0.3f);
            e.MoveForward(0);
            var current = e.CurrentPoint;
            while(e.MoveNext()){
                var p = e.CurrentPoint;
                Handles.DrawLine(current, p);
                var normal = e.CurrentNormal;
                Handles.DrawLine(p, p + normal);
                current = p;
            }
        }
        private Color ColorMultiplier(Color color, PointId id) {
            if(_selection.IsSelected(id)){
                return color;
            }
            color.r *= 0.7f;
            color.g *= 0.7f;
            color.b *= 0.7f;
            return color;
        }
        private static void DrawController(Vector3 position, float sizeMultiplier) {
            Handles.SphereHandleCap(0, position, Quaternion.identity, sizeMultiplier * GetSelectRadius(position), Event.current.type);
        }
        private static Ray ToLocal(Ray ray) {
            var i = Handles.matrix.inverse;
            var o = i.MultiplyPoint(ray.origin);
            var d = i.MultiplyVector(ray.direction);
            return new Ray(o, d);
        }
        private static Ray LocalMouseRay {
            get {
                var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                return ToLocal(ray);
            }
        }
        private static Ray LocalCameraNormalRay {
            get {
                var transform = Camera.current.transform;
                var cameraNormalOrigin = transform.position;
                var cameraNormalDirection = transform.forward;
                var ray = new Ray(cameraNormalOrigin, cameraNormalDirection);
                return ToLocal(ray);
            }
        }
        private PointId FindPointUnderMouse(bool includeControls = true) {
            var r = LocalMouseRay;
            var points = _path.Points;
            var arr = points.array;
            for (int i = 0; i < points.Count; i++) {
                var p = arr[i];
                if (includeControls) {
                    if (Crosses(r, p.leftControl)) {
                        return new PointId(i, PointType.LeftControl);
                    }
                    if (Crosses(r, p.rightControl)) {
                        return new PointId(i, PointType.RightControl);
                    }
                }
                if (Crosses(r, p.position)) {
                    return new PointId(i, PointType.Point);
                }
            }
            return new PointId(-1, PointType.Point);
        }
        private static bool Crosses(in Ray r, in Vector3 point) {
            var radius = GetSelectRadius(point) * c_selectionRadiusMultiplayer;
            return RayToPointDistanceSqr(r, point) <= radius * radius;
        }
        private static Vector3 NearestPointOnRay(in Ray r, in Vector3 point) {
            var l1 = r.origin;
            var l2 = r.origin + r.direction;
            var rate = GeometryUtils.PointToLineProjection(l1, l2, point);
            var nPoint = l1 + (l2 - l1) * rate;
            return nPoint;
        }
        private static float RayToPointDistanceSqr(in Ray r, in Vector3 point) {
            return (NearestPointOnRay(r,point) - point).sqrMagnitude;
        }
        private static float GetSelectRadius(Vector3 point) {
            return HandleUtility.GetHandleSize(point);
        }

        public int SingleSelectedIndex
        {
            get
            {
                return _selection.SingleSelection.index;
            }
        }

        private class Selection {
            private static List<int> _tempSelection = new List<int>();
            private PointType _type;
            private HashSet<int> _points;
            private PointId _pressPoint;
            private readonly EBezierPath _path;
            private EditorSelectionRect _rect;
            public bool dragging = false;
            private Vector2 _mouseStartPosition;
            private bool _edited;
            public bool Edited {
                get {
                    if (!_edited) return false;
                    _edited = false;
                    return true;
                }
            }
            public void DoMoveHandle() {
                SanitizeSelection();
                if(_points.Count <= 0 || dragging) return;
                var center = GetSelectionCenter();
                var newPoint = Handles.DoPositionHandle(center, Quaternion.identity);
                ShiftSelection(newPoint - center);
            }
            public void DoNormalOffsetHandle() {
                SanitizeSelection();
                if (_points.Count != 1 || dragging) return;
                var e = _points.GetEnumerator();
                e.MoveNext();
                var point = _path._path.Points.array[e.Current];
                var tangent = _path._path.EvaluateDirection(e.Current, 0f);
                _anchorAngleHandle.angle = point.normalOffset;
                var normal = Vector3.Cross(tangent, Vector3.up);
                var handleMatrix = Matrix4x4.TRS (
                    point.position,
                    Quaternion.LookRotation (normal, tangent),
                    .5f * HandleUtility.GetHandleSize(point.position) * Vector3.one
                );
                using (Scopes.HandlesMatrix(handleMatrix)) {
                     _anchorAngleHandle.DrawHandle();
                     _path._path.Points.array[e.Current].normalOffset = (_anchorAngleHandle.angle + 360f) % 360f;
                }
            }
            private void SanitizeSelection() {
                var e = _points.GetEnumerator();
                while (e.MoveNext()) {
                    var i = e.Current;
                    if(i >= _path._path.Points.Count) {
                        _tempSelection.Add(i);
                    }
                }
                for(int i = 0; i < _tempSelection.Count; i++) {
                    _points.Remove(_tempSelection[i]);
                }
                _tempSelection.Clear();
            }
            public void DrawSelection(){
                if(_rect.IsActive){
                    _rect.DrawScene();
                    SceneView.lastActiveSceneView.Repaint();
                }
            }
            public bool IsInsideRect(Vector3 point) {
                return _rect.ContainsPoint(Handles.matrix.MultiplyPoint3x4( point));
            }
            private Vector3 GetSelectionCenter(){
                switch(_type) {
                    case PointType.LeftControl:
                    return _path._path.Points.array[SingleSelection.index].leftControl;
                    case PointType.RightControl:
                    return _path._path.Points.array[SingleSelection.index].rightControl;
                    default:
                    case PointType.Point:
                    {
                        var e = _points.GetEnumerator();
                        int count = 0;
                        Vector3 p = Vector3.zero;
                        while(e.MoveNext()) {
                            count++;
                            p += _path._path.Points.array[e.Current].position;
                        }
                        return p / count;
                    }
                }
            }
            private void SnapToNearestPlane(ref Vector3 controlPoint, Vector3 point) {
                var m = Event.current.modifiers;
                if (!m.HasFlag(EventModifiers.Control)) {
                    return;
                }
                var dif = controlPoint - point;
                Vector3 p;
                p.x = Mathf.Abs(dif.x);
                p.y = Mathf.Abs(dif.y);
                p.z = Mathf.Abs(dif.z);
                if(p.x < p.y) {
                    if(p.x < p.z) {
                        controlPoint.x = point.x;
                    }
                    else {
                        controlPoint.z = point.z;
                    }
                }
                else {
                    if(p.y < p.z) {
                        controlPoint.y = point.y;
                    }
                    else {
                        controlPoint.z = point.z;
                    }
                }
            }
            private void ShiftSelection(Vector3 amount) {
                var arr = _path._path.Points.array;
                BezierPath3D.Point val;
                var sindex = SingleSelection.index;
                if (sindex >= 0) {
                    val = _path._path.Points.array[sindex];
                }
                else {
                    val = new BezierPath3D.Point();
                }
                switch (_type) {
                    case PointType.LeftControl:
                        val.leftControl += amount;
                        SnapToNearestPlane(ref val.leftControl, val.position);
                        _path.UpdateNodeForCurrentMode(ref val.rightControl, val.position, val.leftControl);

                        break;
                    case PointType.RightControl:
                        val.rightControl += amount;
                        SnapToNearestPlane(ref val.rightControl, val.position);
                        _path.UpdateNodeForCurrentMode(ref val.leftControl, val.position, val.rightControl);
                        break;
                    default:
                    case PointType.Point: {
                            var e = _points.GetEnumerator();
                            while (e.MoveNext()) {
                                var v = e.Current;
                                var vv = arr[v];
                                vv.position += amount;
                                vv.rightControl += amount;
                                vv.leftControl += amount;
                                arr[v] = vv;
                            }
                            sindex = -1;
                        }
                        break;
                }
                if (sindex >= 0) {
                    arr[SingleSelection.index] = val;
                }
            }
            public Selection(EBezierPath path) {
                _path = path;
                _points = new HashSet<int>();
            }
            internal bool Press() {
                dragging = true;
                _mouseStartPosition = Event.current.mousePosition;
                bool returnValue = _points.Count > 0;
                ClearSelection();
                _pressPoint = _path.FindPointUnderMouse(_path._path.controlMode != BezierPath3D.ControlMode.Automatic);
                if (_pressPoint.index >= 0) {
                    SingleSelection = _pressPoint;
                    return true;
                }
                return returnValue;
            }
            public void OnDrag() {
                if (_points.Count != 1) return;
                var center = GetSelectionCenter();
                //Drag with just left click
                //project point to camera normal
                var cameraNormal = LocalCameraNormalRay;
                var projectionPoint = NearestPointOnRay(cameraNormal, center);
                //Get Point, project point, camera normal ray plane
                var plane = new Plane(cameraNormal.direction, projectionPoint);
                //get mouse ray intersect point to said plane
                var mouseRay = LocalMouseRay;
                if (plane.Raycast(mouseRay, out float enter)) {
                    var newPoint = mouseRay.GetPoint(enter);
                    ShiftSelection(newPoint - center);
                    _edited = true;
                }
            }
            public void Release() {
                dragging = false;
                var currentMousePosition = Event.current.mousePosition;
                if (Vector2.Distance(currentMousePosition, _mouseStartPosition) > 10f) {
                    ClearSelection();
                }
            }
            
            internal bool PressAdditive() {
                _pressPoint = _path.FindPointUnderMouse(_path._path.controlMode != BezierPath3D.ControlMode.Automatic);
                if (_pressPoint.index >= 0 && _pressPoint.type == PointType.Point) {
                    _points.Add(_pressPoint.index);
                }
                _rect.OnPress();
                return true;
            }
            public bool IsSelected(PointId id) {
                return id.type == _type && _points.Contains(id.index);
            }
            internal void ReleaseAdditive() {
                if (_rect.IsActive) {
                    if(_type != PointType.Point) {
                        _points.Clear();
                    }
                    var points = _path._path.Points;
                    var arr = points.array;
                    for (int i = 0; i < points.Count; i++) {
                        if (_rect.ContainsPoint(Handles.matrix.MultiplyPoint3x4(arr[i].position))) {
                            _points.Add(i);
                        }
                    }
                    _type = PointType.Point;
                }
                _rect.OnRelease();
            }
            internal void ClearSelection() {
                _points.Clear();
            }
            public PointId SingleSelection {
                get {
                    if(_points.Count == 1) {
                        var e = _points.GetEnumerator();
                        e.MoveNext();
                        return new PointId(e.Current, _type);
                    }
                    return new PointId(-1, PointType.Point);
                }
                set {
                    ClearSelection();
                    _points.Add(value.index);
                    _type = value.type;
                }
            }

            
        }
        private struct PointId {
            public int index;
            public PointType type;
            public PointId(int index, PointType type) {
                this.index = index;
                this.type = type;
            }
        }
        private enum PointType : byte{
            Point = 0,
            LeftControl = 1,
            RightControl = 2,
        }
        private enum SelectionMode {
            Point,
            ControlPoint,
        }
    }
}