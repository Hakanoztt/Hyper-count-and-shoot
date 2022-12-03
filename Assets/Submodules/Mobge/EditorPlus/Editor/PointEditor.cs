using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace Mobge {
    public class PointEditor<T> where T : new() {
        public delegate void UpdateValue(ref T t, Vector3 position);
        //public PlaneFeatures planeFeatures = new PlaneFeatures(new Plane(new Vector3(0, 0, 1), 0), PlaneMode.Static);
        private readonly EditorSelectionQueue<int> _selectionQueue;
        private EditorTools _editorTools;
        private T[][] _polygons;
        private readonly Func<T, Vector3> _toVector;
        private readonly UpdateValue _updateValue;
        public Func<Vector3, Vector3> HandleMoveCenter { get; set; }
        private readonly VisualSettings _visualSettings;
        private readonly MultiSelectModule _multiSelectModule;
        private readonly RightClickModule _rightClickModule;
        private readonly MoveModule _moveModule;
        private readonly MoveCenterModule _moveCenterModule;
        private readonly HashSet<int> _selection;
        private Vector2[] _tempPoints;
        private bool _justAdded = false;

        private static readonly T[][] WrapperPolygonList = new T[1][];
        private bool _edited;

        public Action<int> OnRightClick;
        public Func<int, T> NewPoint;

        private bool Edited {
            get {
                if (!_edited) return false;
                _edited = false;
                return true;
            }
        }
        public PointEditor(Func<T, Vector3> toVector, UpdateValue updateValue, VisualSettings visualSettings) {
            _toVector = toVector;
            _updateValue = updateValue;
            _selection = new HashSet<int>();
            _moveModule = new MoveModule(this);
            _multiSelectModule = new MultiSelectModule(this);
            _moveCenterModule = new MoveCenterModule(this);
            _rightClickModule = new RightClickModule(this);
            EnsureTools();
            _selectionQueue = new EditorSelectionQueue<int>();
            _visualSettings = visualSettings;
        }
        public PointEditor(Func<T, Vector3> toVector, UpdateValue updateValue) : this(toVector, updateValue, DefaultVisualSettings) { }
        public int SelectedPolygon { get; set; } = 0;
        public HashSet<int> Selection => _selection;
        public int SelectedCornerConunt => _selection.Count;
        private void EnsureTools() {
            if (_editorTools != null) return;
            _editorTools = new EditorTools();
            _editorTools.AddTool(new EditorTools.Tool("move center") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0,
                    modifiers = EventModifiers.Alt,
                },
                onPress = _moveCenterModule.Press,
                onDrag = _moveCenterModule.Drag,
            });
            _editorTools.AddTool(new EditorTools.Tool("move corner") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0
                },
                onPress = _moveModule.Press,
                onDrag = _moveModule.Drag,
            });
            _editorTools.AddTool(new EditorTools.Tool("select unselect corner") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0,
                    modifiers = EventModifiers.Shift,
                },
                onPress = _multiSelectModule.SingleSelectPress,
            });
            _editorTools.AddTool(new EditorTools.Tool("multi select corner") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0,
                    modifiers = EventModifiers.Shift,
                },
                onPress = _multiSelectModule.Press,
                onDrag = _multiSelectModule.Drag,
                onRelease = _multiSelectModule.Release,
                onUpdate = _multiSelectModule.Update,
            });
            _editorTools.AddTool(new EditorTools.Tool("select polygon") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0
                },
                onPress = PolygonSelect
            });
            _editorTools.AddTool(new EditorTools.Tool("add remove point") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0,
                    modifiers = AddRemoveModifier
                },
                onPress = AddRemovePointPress,
                onDrag = _moveModule.Drag,
                onRelease = AddRemovePointRelease,
            });
            _editorTools.AddTool(new EditorTools.Tool("remove selected points") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.D,
                },
                onPress = RemoveSelectedPoints,
            });
            _editorTools.AddTool(new EditorTools.Tool("right click on point") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 1,
                },
                onPress = _rightClickModule.Press,
                onRelease = _rightClickModule.Release
            });




        }
        private bool AddRemovePointPress() {
            if (!FindAddRemovePoint(out var point, out var add, out var index)) {
                _selection.Clear();
                return false;
            }
            if (add) {
                var t = NewPoint == null ? new T() : NewPoint(index);
                _selection.Clear();
                _updateValue(ref t, point);
                ArrayUtility.Insert(ref _polygons[SelectedPolygon], index, t);
                _selection.Add(index);
                _justAdded = true;
                _moveModule.Press();
            }
            else {
                ArrayUtility.RemoveAt(ref _polygons[SelectedPolygon], index);
                _selection.Remove(index);
            }
            _edited = true;
            return true;
        }
        private void AddRemovePointRelease() {
            _justAdded = false;
        }
        private bool RemoveSelectedPoints() {
            if (SelectedPolygon < 0 || SelectedPolygon >= _polygons.Length) return false;
            if (_selection.Count <= 0) return false;
            var p = _polygons[SelectedPolygon];
            for (int i = p.Length; i >= 0; i--) {
                if (_selection.Contains(i)) {
                    ArrayUtility.RemoveAt(ref _polygons[SelectedPolygon], i);
                }
            }
            _selection.Clear();
            _edited = true;
            return true;
        }
        private static EventModifiers AddRemoveModifier => EventModifiers.Control;
        private int FindMovePoint(out Vector3 mousePos, out Vector3 moveStartPos) {
            //mousePos = MousePos;
            if (TryGetSelectedPolygon(out var polygon)) {
                if(FindNearestPointToMouse(out int index, polygon, out Ray mRay)) {
                    moveStartPos = _toVector(polygon[index]);
                    mousePos = NearestPointOnRay(mRay, moveStartPos);
                    return index;
                }
            }
            moveStartPos = Vector2.zero;
            mousePos = Vector3.zero;
            return -1;
        }
        /// <summary>
        /// Finds index of the nearest point to the mouse. Returns true if nearest point is within the selection radius.
        /// </summary>
        /// <param name="nearestI"></param>
        /// <param name="polygon"></param>
        /// <returns></returns>
        private bool FindNearestPointToMouse(out int nearestI, T[] polygon, out Ray localMouseRay) {
            localMouseRay = LocalMouseRay;
            float nearestSqr = float.PositiveInfinity;
            nearestI = -1;
            Vector3 nearestPoint = Vector3.zero;
            for (int i = 0; i < polygon.Length; i++) {
                var cp = _toVector(polygon[i]);
                var disSqr = (NearestPointOnRay(localMouseRay, cp) - cp).sqrMagnitude;
                if (disSqr > nearestSqr) continue;
                nearestSqr = disSqr;
                nearestI = i;
                nearestPoint = cp;
            }
            float selectR = GetSelectRadius(nearestPoint);
            return nearestSqr < selectR * selectR;
        }
        private static Vector3 NearestPointOnRay(in Ray r, in Vector3 point) {
            var l1 = r.origin;
            var l2 = r.origin + r.direction;
            var rate = GeometryUtils.PointToLineProjection(l1, l2, point);
            var nPoint = l1 + (l2 - l1) * rate;
            return nPoint;
        }
        private static float GetSelectRadius(Vector3 point) {
            return HandleUtility.GetHandleSize(point) * 0.25f;
        }
        private bool TryGetSelectedPolygon(out T[] polygon) {
            var ps = _polygons;
            if (SelectedPolygon < 0 || SelectedPolygon >= ps.Length) {
                polygon = null;
                return false;
            }
            polygon = ps[SelectedPolygon];
            return true;
        }
        private static void Repaint() {
            SceneView.RepaintAll();
        }
        private Vector3 MousePos2D {
            get {
                var ray = _editorTools.MouseRay;
                var i = Handles.matrix.inverse;
                var o = i.MultiplyPoint(ray.origin);
                var d = i.MultiplyVector(ray.direction);
                return o + d * -o.z / d.z;
            }
        }
        private Vector3 GetMousePos3D(T[] polygon) {

            var r = LocalMouseRay;
            if(polygon == null || polygon.Length == 0) {
                return NearestPointOnRay(r, Vector3.zero);
            }
            Vector3 total = Vector3.zero;
            for (int i = 0; i < polygon.Length; i++) {

                var p = _toVector(polygon[i]);
                total += p;
            }
            return NearestPointOnRay(r, total / (float)polygon.Length);
        }
        private bool PolygonSelect() {
            Vector2 mousePos = MousePos2D;
            for (int i = 0; i < _polygons.Length; i++) {
                var p = _polygons[i];
                if (ContainsPoint(p, mousePos)) {
                    _selectionQueue.AddCandidate(i);
                }
            }
            if (!_selectionQueue.SelectOne(out var ns)) {
                return false;
            }
            SelectedPolygon = ns;
            Repaint();
            _selection.Clear();
            return true;
        }
        private bool FindAddRemovePoint(out Vector3 point, out bool add, out int index) {
            if (!TryGetSelectedPolygon(out var polygon)) {
                point = Vector3.zero;
                add = false;
                index = -1;
                return false;
            }
            if (polygon.Length < 2) {
                point = GetMousePos3D(polygon);
                add = true;
                index = 0;
                return true;
            }
            if (FindNearestPointToMouse(out int nearestI, polygon, out _)) {
                add = false;
                index = nearestI;
                point = _toVector(polygon[nearestI]);
                return true;
            }
            switch (_visualSettings.mode) {
                default:
                case Mode.OpenPath:
                case Mode.Path:
                    var mousePos = MousePos2D;
                    var points = ToVector2(polygon);
                    point = GeometryUtils.NearestPointToPolygon(points, mousePos, _visualSettings.mode == Mode.Path, out int index1, out int index2);
                    add = index2 >= 0;
                    index = add ? index2 : index1;
                    return true;
                case Mode.Point:
                    add = true;
                    index = polygon.Length;
                    point = GetMousePos3D(polygon);
                    return true;
            }
        }
        private Vector2[] ToVector2(T[] points) {
            if (_tempPoints == null || _tempPoints.Length != points.Length) {
                _tempPoints = new Vector2[points.Length];
            }
            for (int i = 0; i < points.Length; i++) {
                _tempPoints[i] = _toVector(points[i]);
            }
            return _tempPoints;
        }
        private float NearestPointRate(Vector3 l1, Vector3 l2, Vector3 p, out Vector3 intersectionPoint) {
            var dir = l2 - l1;
            var l1Dis = p - l1;
            var dot = Vector2.Dot(dir, l1Dis);
            var rate = dot / dir.sqrMagnitude;
            intersectionPoint = rate * dir + l1;
            return rate;
        }
        private bool ContainsPoint(T[] polygon, Vector2 point) {
            if (polygon.Length < 3) {
                return false;
            }
            bool intersects = false;
            var prev = _toVector(polygon[polygon.Length - 1]);
            for (int i = 0; i < polygon.Length; i++) {
                var c = _toVector(polygon[i]);
                if (prev.y > point.y != c.y > point.y) {
                    var per = (point.y - prev.y) / (c.y - prev.y);
                    var x = prev.x + per * (c.x - prev.x);
                    if (x > point.x) {
                        intersects = !intersects;
                    }
                }
                prev = c;
            }
            return intersects;
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
        private static Ray LocalMouseRay {
            get {
                var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                return ToLocal(ray);
            }
        }
        private static Ray ToLocal(Ray ray) {
            var i = Handles.matrix.inverse;
            var o = i.MultiplyPoint(ray.origin);
            var d = i.MultiplyVector(ray.direction);
            return new Ray(o, d);
        }
        //private Vector3 MousePos {
        //    get {
        //        if(_sele)
        //        if (.Count != 1) return;
        //        var center = GetSelectionCenter();
        //        var cameraNormal = LocalCameraNormalRay;
        //        var projectionPoint = NearestPointOnRay(cameraNormal, center);
        //        var plane = new Plane(cameraNormal.direction, projectionPoint);
        //        var mouseRay = LocalMouseRay;


        //        //var ray = _editorTools.MouseRay;
        //        //var i = Handles.matrix.inverse;
        //        //var o = i.MultiplyPoint(ray.origin);
        //        //var d = i.MultiplyVector(ray.direction);
        //        //return o + d *-o.z/d.z;
        //    }
        //}

        public void OnInspectorGUI(ref int selectedPolygon, int polygonCount, bool enabled) {
            if (enabled) {
                string[] options = new string[polygonCount];
                for (int i = 0; i < options.Length; i++) {
                    options[i] = i.ToString();
                }
                selectedPolygon = EditorGUILayout.Popup("selected polygon", selectedPolygon, options);
            }
        }
        public bool OnSceneGUI(ref T[] polygons, bool enabled = true) {
            WrapperPolygonList[0] = polygons;
            SelectedPolygon = 0;
            bool b = OnSceneGUI(WrapperPolygonList, enabled);
            polygons = WrapperPolygonList[0];
            return b;
        }
        public bool OnSceneGUI(T[][] polygons, bool enabled = true) {
            _polygons = polygons;
            if (_polygons == null) return false;
            if (enabled) {
                if (TryGetSelectedPolygon(out _)) {
                    DrawHandles();
                }
                _editorTools.OnSceneGUI();
            }
            for (int i = 0; i < _polygons.Length; i++) {
                var p = _polygons[i];
                if (p == null) {
                    p = new T[0];
                    _polygons[i] = p;
                }
                bool selected = i == SelectedPolygon;
                var color = selected ? new Color(0.35f, 0.75f, 1) : new Color(0.25f, 0.5f, 1);
                if (_visualSettings.mode == Mode.Path || _visualSettings.mode == Mode.OpenPath) {
                    if (p.Length > 1) {
                        int countIndex;
                        int startIndex;
                        if(_visualSettings.mode == Mode.OpenPath) {
                            startIndex = 0;
                            countIndex = 1;
                        }
                        else {
                            startIndex = p.Length - 1;
                            countIndex = 0;
                        }
                        var prevC = _toVector(p[startIndex]);
                        for (int j = countIndex; j < p.Length; j++) {
                            var c = _toVector(p[j]);
                            Handles.color = Color.black;
                            Handles.DrawAAPolyLine(_visualSettings.lineWidth + _visualSettings.outlineWidth, prevC, c);
                            Handles.color = color;
                            Handles.DrawAAPolyLine(_visualSettings.lineWidth, prevC, c);
                            prevC = c;
                        }
                    }
                }
                if (enabled && i == SelectedPolygon) {
                    for (int j = 0; j < p.Length; j++) {
                        var c = _toVector(p[j]);
                        var state = PointState.Normal;
                        if (_selection.Contains(j)) {
                            state = PointState.Selected;
                        }
                        if (_multiSelectModule.rect.ContainsPoint(c)) {
                            state = PointState.Highlighted;
                        }
                        if (FindNearestPointToMouse(out int index, p, out _) && index == j) {
                            state = PointState.Highlighted;
                            if (Event.current.modifiers == AddRemoveModifier && !_justAdded) {
                                state = PointState.Remove;
                            }
                        }
                        DrawPoint(c, state);
                    }
                    if (Event.current.modifiers == AddRemoveModifier && FindAddRemovePoint(out var point, out var add, out _) && add) {
                        DrawPoint(point, PointState.Add);
                    }
                }
            }
            SceneView.RepaintAll();
            Handles.color = Color.white;
            return Edited;
        }
        private void DrawHandles() {
            if (_selection.Count < 2) return;
            var center = GetSelectionCenter();
            var newPosition = Handles.DoPositionHandle(center, Quaternion.identity);
            SetSelectionCenter(newPosition);
        }
        private Vector3 GetSelectionCenter() {
            if(SelectedPolygon<0 || SelectedPolygon >= _polygons.Length) {
                return Vector3.zero;
            }
            Vector3 total = Vector3.zero;
            foreach (var item in _selection) {
                if (item >= _polygons[SelectedPolygon].Length) continue;
                var t = _polygons[SelectedPolygon][item];
                var v = _toVector(t);
                total += v;
            }
            return total / _selection.Count;
        }
        private void SetSelectionCenter(Vector3 newPosition) {
            var diff = newPosition - GetSelectionCenter();
            foreach (var item in _selection) {
                if (item >= _polygons[SelectedPolygon].Length) continue;
                var t = _polygons[SelectedPolygon][item];
                var v = _toVector(t);
                _updateValue(ref _polygons[SelectedPolygon][item], v + diff);
            }
            _edited = true;
        }
        public struct VisualSettings {
            public float lineWidth;
            public float outlineWidth;
            public Mode mode;
        }
        public enum Mode {
            Path = 0,
            Point,
            OpenPath,
        }
        private static VisualSettings DefaultVisualSettings {
            get {
                VisualSettings vs;
                vs.lineWidth = 2;
                vs.outlineWidth = 1;
                vs.mode = Mode.Path;
                return vs;
            }
        }
        private static void DrawPoint(Vector3 point, PointState state) {
            var size = GetSelectRadius(point) * 0.5f;
            switch (state) {
                default:
                case PointState.Normal:
                    Handles.color = new Color(0, 0, 1, 1);
                    break;
                case PointState.Add:
                    Handles.color = new Color(0, 1, 0, 1);
                    break;
                case PointState.Remove:
                    Handles.color = new Color(1, 0, 0, 1);
                    break;
                case PointState.Selected:
                    Handles.color = new Color(0, 1, 1, 1);
                    break;
                case PointState.Highlighted:
                    Handles.color = new Color(1, 1, 1, 1);
                    break;
            }
            const float scale = 1;
            //Handles.DrawSolidDisc(point, Vector3.forward, scale * size);
            Handles.SphereHandleCap(0, point, Quaternion.identity, size * scale, Event.current.type);
        }
        private enum PointState {
            Normal,
            Add,
            Remove,
            Selected,
            Highlighted,
        }
        private class MoveModule {
            private readonly PointEditor<T> _editor;
            private Vector3 _mp;
            private int _pressCorner;
            private HashSet<int> Selection => _editor._selection;
            public MoveModule(PointEditor<T> editor) {
                _editor = editor;
            }
            internal bool Press() {
                Selection.Clear();
                if (!_editor.TryGetSelectedPolygon(out var polygon)) { return false; }
                _pressCorner = _editor.FindMovePoint(out _mp, out _);
                if (_pressCorner < 0) return false;
                Selection.Add(_pressCorner);
                return true;
            }
            internal void Drag() {
                if (!_editor.TryGetSelectedPolygon(out var polygon)) return;
                if (Selection.Count != 1) return;
                var mousePos = NearestPointOnRay(LocalMouseRay, _editor.GetSelectionCenter());
                var d = mousePos - _mp;
                foreach (var item in Selection) {
                    if (polygon.Length <= item) continue;
                    var c = _editor._toVector(polygon[item]);
                    _editor._updateValue(ref polygon[item], c + d);
                }
                _mp = mousePos;
                Repaint();
                _editor._edited = true;
            }
        }
        private class RightClickModule {
            private PointEditor<T> _editor;
            private int _pressIndex;

            public RightClickModule(PointEditor<T> editor) {
                _editor = editor;
            }
            public bool Press() {
                if (_editor.OnRightClick == null) {
                    return false;
                }
                if (!_editor.TryGetSelectedPolygon(out T[] polygon)) {
                    return false;
                }
                if (!_editor.FindNearestPointToMouse(out int index, polygon, out _)) {
                    return false;
                }
                _pressIndex = index;
                return true;
            }
            public void Release() {
                if (!_editor.TryGetSelectedPolygon(out T[] polygon)) {
                    return;
                }
                if (!_editor.FindNearestPointToMouse(out int index, polygon, out _)) {
                    return;
                }
                if (index == _pressIndex) {
                    _editor.OnRightClick(index);
                }
            }
        }
        private class MultiSelectModule {
            private readonly PointEditor<T> _editor;
            public EditorSelectionRect rect;
            private int _selectedCorner;

            private HashSet<int> Selection => _editor._selection;
            public MultiSelectModule(PointEditor<T> pointEditor) {
                this._editor = pointEditor;
            }

            internal bool SingleSelectPress() {

                _selectedCorner = _editor.FindMovePoint(out _, out _);
                if (_selectedCorner < 0) return false;
                if (Selection.Contains(_selectedCorner)) {
                    Selection.Remove(_selectedCorner);
                }
                else {
                    Selection.Add(_selectedCorner);
                }
                return true;
            }

            internal bool Press() {
                rect.OnPress();
                return true;
            }

            internal void Drag() {
                //Selection.Clear();
                if(_editor.TryGetSelectedPolygon(out var p)) {
                    for(int i = 0; i < p.Length; i++) {
                        if (rect.ContainsPoint(Handles.matrix.MultiplyPoint3x4( _editor._toVector(p[i])))) {
                            Selection.Add(i);
                        }
                    }
                }
            }

            internal void Release() {
                rect.OnRelease();
            }

            internal void Update() {
                rect.DrawScene();
            }
        }
        private class MoveCenterModule {
            private readonly PointEditor<T> _editor;
            private Vector3 _mp;
            public MoveCenterModule(PointEditor<T> pointEditor) {
                this._editor = pointEditor;
            }
            public bool Press() {
                if (_editor.HandleMoveCenter == null) {
                    return false;
                }
                _mp = _editor.MousePos2D;
                return true;
            }
            public void Drag() {
                var nmp = _editor.MousePos2D;
                var dif = nmp - _mp;
                var pdif = _editor.HandleMoveCenter(dif);
                for (int i = 0; i < _editor._polygons.Length; i++) {
                    var p = _editor._polygons[i];
                    for (int j = 0; j < p.Length; j++) {
                        var c = _editor._toVector(p[j]) - pdif;
                        _editor._updateValue(ref p[j], c);
                    }
                }
                _editor._edited = true;
            }
        }
        
    }
}
