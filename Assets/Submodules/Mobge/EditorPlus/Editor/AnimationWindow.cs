using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Mobge {

    public class AnimationWindow {
        private const float c_frameInterval = 1f/60f;
        private const float c_nodeWidth = 6f;
        private const int c_rowHeight = 30;
        private const int c_space = 4;
        private const int c_timerLineMode = 5;
        private const float c_scaleHandleWidth = 4;
        private GUIStyle _labelSkin;
        private Action<Curve[], float> _dataChanged;
        private Func<string, float> _getCurrentValue;
        public Rect position;
        private EditorTools _tools;
        private Rect _rLabel, _rRow;
        private float _y;
        private float _time;
        private Rect _timeRect;
        private Rect _timeArea;
        private float _endTime;
        private float _startTime;
        private float _totalTime;
        private Curve[] _curves;
        private Action _updateVisuals;
        private ExposedList<Keys> _keys = new ExposedList<Keys>();
        private SelectionRect _selectionRect;
        private Vector2 _mouseStart;
        private bool _editing;
        public float Time => _time;
        private bool _firstTime;
        private float _yTop;
        private float _yBottom;
        private Rect _totalTimeRect;
        private bool _dragged;
        private List<KFPath> _pressedPaths;
        private bool _addMode;
        private KeyGroups _keyGroups;
        private ScaleHandler _scaleHandler;
        private List<KFPath> _tempPaths;
        public AnimationWindow() {
            _tempPaths = new List<KFPath>();
            _pressedPaths = new List<KFPath>();
            _selectionRect.Init(this);
            _keyGroups = new KeyGroups(this);
            _scaleHandler = new ScaleHandler(this);
            _tools = new EditorTools();
            _tools.AddTool(new EditorTools.Tool("move time") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0
                },
                onPress = TimePress,
                onDrag = TimeDrag,
                onRelease = TimeRelease,
            });
            _tools.AddTool(new EditorTools.Tool("move nodes") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0,
                },
                onPress = () => MovePress(false),
                onDrag = MoveDrag,
                onRelease = MoveRelease,
            });
            _tools.AddTool(new EditorTools.Tool("add and move nodes") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0,
                    modifiers = EventModifiers.Shift
                },
                onPress = () => MovePress(true),
                onDrag = MoveDrag,
                onRelease = MoveRelease,
            });
            _tools.AddTool(new EditorTools.Tool("scale") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0,
                },
                onPress = _scaleHandler.Press,
                onDrag = _scaleHandler.Drag,
                onRelease = _scaleHandler.Release,
            });
            _tools.AddTool(new EditorTools.Tool("move total time") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0,
                },
                onPress = TotalTimePress,
                onDrag = TotalTimeDrag,
                onRelease = TotalTimeRelease,
            });

            _labelSkin = new GUIStyle();
            _labelSkin.alignment = TextAnchor.MiddleLeft;
        }
        public void UpdatePosition() {
            var pos = Event.current.mousePosition;
            pos.x -= 200;
            pos.y += EditorGUIUtility.singleLineHeight * 0.5f;
            Rect r = new Rect(pos, new Vector2(600, 300));
            position = r;
            _endTime = -1;
            _startTime = -1;
            _firstTime = true;
        }
        private void Repaint() {
            SceneView.RepaintAll();
        }
        private bool TotalTimePress()
        {
            var mp = Event.current.mousePosition;
            if(_totalTimeRect.Contains(mp)) {
                
                _mouseStart = mp;
                _editing = true;
                return true;
            }
            return false;
        }

        private void TotalTimeDrag()
        {
            //var time = GetTimeFromMouse
            float start = GetTimeFromPos(_mouseStart.x);
            float target = GetTimeFromMouse();
            _totalTime = target;
            Repaint();

        }
        private void TotalTimeRelease()
        {
            _editing = false;
            UpdateCurves();
            UpdateVisuals();
        }


        public void OnGUI(Curve[] curves, Action<Curve[], float> dataChanged, Action updateVisuals, Func<string, float> getCurrentValue, ref float totalTime) {
            _dataChanged = dataChanged;
            _getCurrentValue = getCurrentValue;
            _curves = curves;
            _updateVisuals = updateVisuals;
            position = UnityEngine.GUI.Window(243, position, DrawGUI, "Movement");
            if(!_editing) {
                _totalTime = totalTime;
                _keys.ClearFast();
                UpdateCurveCount(curves.Length);
                for(int i = 0; i < curves.Length; i++) {
                    FillKeyFrames(i, curves[i].curve);
                }
                if(_endTime < 0) {
                    _endTime = Mathf.Max(1.5f, _totalTime * 1.5f);
                    _startTime = 0;
                }
                totalTime = _totalTime;
                if(_firstTime) {
                    _firstTime = false;
                    UpdateVisuals();
                }
            }
        }
        private void FillKeyFrames(int index, AnimationCurve curve) {
            var target = _keys.array[index].values;
            target.ClearFast();
            var keys = curve.keys;
            float time = 0;
            for(int i = 0; i < keys.Length; i++) {
                var kf = new KF(keys[i], index);
                target.Add(kf);
                time = kf.time;
            }
            _totalTime = Mathf.Max(_totalTime, time);
            FixKeys(index);
        }
        private void FixKeys(int index) {
            var target = _keys.array[index].values;
            Array.Sort(target.array, 0, target.Count);
            var arr = target.array;
            int frame = -1;
            for(int i = 0; i < target.Count; ) {
                var cf = arr[i].Frame;
                if(cf == frame) {
                    target.RemoveAt(i);
                }
                else {
                        i++;
                }
                frame = cf;
            }
        }
        private string Format(float f, float minStep) {
            return string.Format("{0:0.###}", f);
        }
        private bool TimePress() {
            var mouseStart = Event.current.mousePosition;
            if(_timeArea.Contains(mouseStart)) {
                TimeDrag();
                return true;
            }
            return false;
        }
        private void TimeDrag() {
            _time = GetTimeFromMouse();
            UpdateVisuals();
            Repaint();
            //GUI.FocusControl(null);
        }
        private void TimeRelease() {
            GUI.FocusControl(null);
            Repaint();
        }
        private bool TryFindKF(Vector2 position, out KFPath path) {
            var keysArr = _keys.array;
            for (int i = 0; i < _keys.Count; i++) {
                var kfs = keysArr[i];
                var a = kfs.area;
                a.xMin -= c_nodeWidth;
                if (a.Contains(position)) {
                    var kfarr = kfs.values.array;
                    for (int j = 0; j < kfs.values.Count; j++) {
                        var x = GetPosForTime(kfs.area, kfarr[j].time);
                        if (position.x > x - c_nodeWidth * 0.5f && position.x < x + c_nodeWidth * 0.5f) {
                            path = new KFPath(i, j);
                            return true;
                        }
                    }
                }
            }

            path = new KFPath(-1, -1);
            return false;
        }

        public void StartEdit() {
            _editing = true;
            _selectionRect.PrepareForOperation();
        }
        public void EndEdit() {
            _editing = false;
            GUI.FocusControl(null);
        }
        private bool MovePress(bool addMode) {
            _mouseStart = Event.current.mousePosition;
            
            _addMode = addMode;
            _dragged = false;
            if (TryFindKF(_mouseStart, out KFPath path)) {
                _pressedPaths.Clear();
                _pressedPaths.Add(path);

                UpdateSelection(_pressedPaths, addMode);
                UpdateTimeFromPressed();
                StartEdit();
                return true;
            }
            if(_keyGroups.TryFindGroup(_mouseStart, out KeyGroups.Group group)) {
                _pressedPaths.Clear();
                _pressedPaths.AddRange(group.keys);
                UpdateSelection(_pressedPaths, addMode);
                UpdateTimeFromPressed();
                StartEdit();
                return true;
            }
            return false;
        }
        void UpdateTimeFromPressed() {
            if(_pressedPaths.Count > 0) {
                if (TryGetKF(_pressedPaths[0], out KF kf)) {
                    _time = kf.time;
                }
            }
        }
        void UpdateSelection(List<KFPath> paths, bool addMode) {
            for(int i = 0; i < paths.Count; i++) {
                if (!_selectionRect.Contains(paths[i])) {
                    if (!addMode) {
                        _selectionRect.Clear();
                    }
                    _selectionRect.AddRange(paths);
                    return;
                }
            }
        }
        private void MoveDrag() {
            var mp = Event.current.mousePosition;
            var dif = mp - _mouseStart;
            _selectionRect.TimeOffset = dif.x * (_endTime - _startTime) / _rRow.width;
            
            UpdateVisuals();
            _dragged = true;
            Repaint();
        }
        private void MoveRelease() {
            if (!_dragged && !_addMode) {
                _selectionRect.Clear();
                _selectionRect.AddRange(_pressedPaths);
            }
            UpdateTimeFromPressed();
            _editing = false;
            UpdateCurves();
            UpdateVisuals();
            EndEdit();
        }
        private void UpdateCurves() {
            
            if(_dataChanged != null) {
                for(int i = 0; i < _keys.Count; i++) {
                    FixKeys(i);
                    var c = _keys.array[i];
                    var kfs = c.values.array;
                    var keys = new Keyframe[c.values.Count];
                    for(int j = 0; j < keys.Length; j++) {
                        keys[j] = kfs[j].keyFrame;
                    }
                    _curves[i].curve.keys = keys;
                }
                _dataChanged(_curves, _totalTime);
                
            }
        }
        private void UpdateVisuals() {
            if(_updateVisuals!=null) {
                _updateVisuals();
            }
        }
        private void DrawTimer(in Rect rect) {
            _timeArea = rect;
            float width = rect.width;
            float minStep = FindMinStep(_endTime - _startTime, out int labelOpaqueMode, out bool frameMode, out float opacity);
            if(minStep == 0) {
                return;
            }
            float time = _startTime;
            int c = 0;
            int labelMode = frameMode ? 6 : (int)c_timerLineMode;
            //EditorGUI.DrawRect(rect, Color.black);
            float bottom = rect.yMax;
            while(time < _endTime) {
                float op;
                float x = GetPosForTime(rect, time);
                float height;
                float extraHeight = 0;
                if(c % labelMode == 0) {
                    height = rect.height * 0.5f;
                    string l;
                    if(frameMode) {
                        l = TimeIndex(time) + "f";
                    }
                    else {
                        l = Format(time, minStep);
                    }
                    op = 1;
                    extraHeight = position.height - bottom - 10;
                    float textOpacity = 1;
                    if(c%(labelOpaqueMode) != 0) {
                        textOpacity = opacity;
                    }
                    var cc = GUI.contentColor;
                    GUI.contentColor = new Color(cc.r,cc.g, cc.b,textOpacity);
                    GUI.Label(new Rect(x - 15, rect.y, 30, rect.height * 0.5f), l, _labelSkin);
                    GUI.contentColor = cc;
                }
                else {
                    height = rect.height * 0.25f;
                    extraHeight = position.height - bottom- 10;
                    op = opacity;
                }
                EditorGUI.DrawRect(new Rect(x - 0.5f, bottom - height , 1.0f, height+extraHeight), new Color(0.5f, 0.5f, 0.5f, op));
                c++;
                time += minStep;
            }
            {
                float timeX = GetPosForTime(rect, _time);
                float height = rect.height * 0.5f;
                _timeRect = new Rect(timeX - c_nodeWidth * 0.5f, rect.yMax - height, c_nodeWidth, height);
                EditorGUI.DrawRect(_timeRect, new Color(1, 0, 0, 0.5f));
            }
        }
        private float TimeIndex(float time) {
            return Mathf.RoundToInt(time / c_frameInterval);
        }
        private float GetPosForTime(in Rect rect, float time) {
            return rect.x + (time - _startTime) * rect.width / (_endTime - _startTime);
        }
        private float GetTimeFromMouse() {
            return GetTimeFromPos(Event.current.mousePosition.x);
        }
        private float GetTimeFromPos(float posx) {
            float dif = posx - _timeArea.x;
            var d = _endTime - _startTime;
            float time = d * dif / _timeArea.width;
            return LimitTime(time) + _startTime;
        }
        private float LimitTime(float time) {
            return TimeIndex(Mathf.Max(time, 0)) * c_frameInterval;
        }
        private float FindMinStep(float time, out int labelOpaqueMode, out bool frameMode, out float opacity) {
            float rawMin = time * (1f / 15f);
            var l = Mathf.Log(rawMin, c_timerLineMode);
            float floor = Mathf.Floor(l);
            var timeStep = Mathf.Pow(c_timerLineMode, floor);
            float frameLimit = rawMin * 10;
            if(frameLimit < 1) {
                labelOpaqueMode = 10;
                frameMode = true;
                if(frameLimit < 0.5f) {
                    opacity = 1;
                }
                else {
                    opacity = 1 - (frameLimit - 0.5f) * 2;
                }
                return c_frameInterval;
            }
            frameMode = false;
            var timeStep5 = timeStep * 5;
            opacity = 1 - (l - floor);
            // if(timeStep5 < rawMin) {
            //     longStickMode = 2;
            //     return timeStep5;
            // }
            labelOpaqueMode = (int)(c_timerLineMode * c_timerLineMode);
            return timeStep;
        }
        private void InitRects() { 
            var labelWidth = EditorGUIUtility.labelWidth;
            _y = EditorGUIUtility.singleLineHeight + c_space;
            _rLabel = new Rect(c_space, _y, labelWidth - c_space - 2*c_space, c_rowHeight);
            float labelXMax = _rLabel.xMax + 2 * c_space;
            _rRow = new Rect(labelXMax + c_space, _y, position.width - labelXMax - 2 * c_space, c_rowHeight);
        }
        private void NextLine() {
            _y += c_rowHeight + c_space;
            _rRow.y = _y;
            _rLabel.y = _y;
        }
        private void DrawCombinedCurve(in Rect rect) {
            _keyGroups.DrawRects(rect);
        }
        private void DrawCurve(in Rect rect, int index) {
            var keys = _keys.array[index];
            _keys.array[index].area = rect;
            var kfs = keys.values.array;
            int count = keys.values.Count;

            for(int i = 0; i < count; i++) {
                var t = kfs[i].time;
                if(t >= _startTime && t <= _endTime) {
                    bool selected = _selectionRect.Contains(new KFPath(index, i));
                    DrawCurveNode(rect, t, Color.gray, selected);
                }
            }
        }
        private void DrawTotalTime() {
            var posx = GetPosForTime(_rRow, _totalTime);
            _totalTimeRect = new Rect(posx - c_nodeWidth * 0.5f, _yTop, c_nodeWidth, _yBottom - _yTop);
            EditorGUI.DrawRect(_totalTimeRect, new Color(0.5f, 1, 0.5f, 0.4f));
        }
        private void DrawRowLine(float y) {
            EditorGUI.DrawRect(new Rect(c_space, y - c_space * 0.5f - 1f, position.width - c_space * 2f, 2f), new Color(0.65f,0.65f,0.65f,1f));
        }
        private Rect DrawCurveNode(in Rect rect, float time, Color color, bool selected) {
            float x = GetPosForTime(rect, time);
            var r = new Rect(x - c_nodeWidth * 0.5f, rect.y, c_nodeWidth, rect.height);
            EditorGUI.DrawRect(r, color);
            if (selected) {
                var r2 = r;
                r2.position += new Vector2(1, 1);
                r2.size -= new Vector2(2, 2);
                EditorGUI.DrawRect(r2, color*2);
            }
            return r;
        }
        private void AddKF(int index) {
            var curve = _curves[index];
            var value = _getCurrentValue(curve.name);
            _keys.array[index].AddKey(index, _time, value);
        }
        //private void RemoveKF(int index) {
        //    _keys.array[index].RemoveKF(_time);
        //}
        private void RemoveSelectionKF() {
            var sel = _selectionRect.GetSelection();
            while (sel.MoveNext()) {
                _tempPaths.Add(sel.Current);
            }
            _selectionRect.Clear();
            _tempPaths.Sort(CompareReverse);
            for(int i = 0; i < _tempPaths.Count; i++) {
                var pt = _tempPaths[i];
                if (TryGetKF(pt, out KF kf)) {
                    _keys.array[pt.curve].values.RemoveAt(pt.index);
                }
            }
            _tempPaths.Clear();
        }
        private int CompareReverse(KFPath p1, KFPath p2) {
            return p2.index - p1.index;
        }
        //private bool HasKF() {
        //    for(int i = 0; i < _keys.Count; i++) {
        //        if(_keys.array[i].HasKey(_time)) {
        //            return true;
        //        }
        //    }
        //    return false;
        //}
        private void DrawMenu(in Rect rect) {
            Rect r = rect;
            r.width = r.height;
            if(GUI.Button(r, "+")) {
                for(int i = 0; i < _keys.Count; i++) {
                    AddKF(i);
                }
                UpdateCurves();
            }
            r.x += r.width + c_space;
            var e = GUI.enabled;
            GUI.enabled = e && _selectionRect.Count > 0;
            if(GUI.Button(r, "-")) {
                RemoveSelectionKF();
                UpdateCurves();
            }
            GUI.enabled = e;
            r.x += r.width + c_space;
            EditorGUI.BeginChangeCheck();
            r.width = 120;
            EditorGUIUtility.labelWidth = 80;
            _startTime = Mathf.Max(0, EditorGUI.FloatField(r, "start", _startTime));
            r.x += r.width + c_space;
            _endTime = Mathf.Max(0, EditorGUI.FloatField(r, "end", _endTime));
            r.x += r.width + c_space;
            if(EditorGUI.EndChangeCheck()) {
                UpdateVisuals();
            }
        }
        private void DrawGUI(int id) {
            var comp = _endTime;
            InitRects();
            DrawMenu(_rLabel);
            NextLine();
            DrawTimer(_rRow);
            NextLine();
            _yTop = _rLabel.y;
            DrawCombinedCurve(_rRow);
            NextLine();
            for (int i = 0; i < _curves.Length; i++) {
                DrawRowLine(_rRow.y);
                if (RowLabelField(i)) {
                    UpdateCurves();
                    UpdateVisuals();
                }
                DrawCurve(_rRow, i);
                NextLine();
            }
            DrawRowLine(_rRow.y);
            NextLine();
            _yBottom = _rLabel.y;
            DrawTotalTime();
            _scaleHandler.DrawVisuals();

            _tools.OnSceneGUI();
            UnityEngine.GUI.DragWindow();
        }
        private bool RowLabelField(int index) {
            var c = _curves[index];
            var lw = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 80;
            var e = GUI.enabled;
            var keys = _keys.array[index];
            int k = keys.IndexOf(_time);
            GUI.enabled = k >= 0 && e;
            EditorGUI.BeginChangeCheck();
            var rect = _rLabel;
            var nVal = EditorGUI.FloatField(_rLabel, c.name, c.curve.Evaluate(_time));
            var B = EditorGUI.EndChangeCheck();
            if(B) {
                keys.values.array[k].keyFrame.value = nVal;
            }
            EditorGUIUtility.labelWidth = lw;
            GUI.enabled = e;
            return B;
        }
        private struct KF : IComparable<KF> {
            public Keyframe keyFrame;
            private int _curveIndex;
            public float time {
                get => keyFrame.time;
            }
            public void SetTime(float time) {
                keyFrame.time = time;
            }
            public KF(Keyframe keyFrame, int curveIndex) {
                this.keyFrame = keyFrame;
                _curveIndex = curveIndex;
            }

            int IComparable<KF>.CompareTo(KF other)
            {
                return Frame - other.Frame;
            }

            public int Frame {
                get {
                    return Mathf.RoundToInt(time / c_frameInterval);
                }
            }
        }
        public struct Curve {
            public AnimationCurve curve;
            public string name;
            public Curve(AnimationCurve curve, string name) {
                this.curve = curve;
                this.name = name;
            }
        }
        private struct KFPath {
            public int curve, index;
            public KFPath(int curve, int index) {
                this.curve = curve;
                this.index = index;
            }
        }
        private struct Keys {
            public ExposedList<KF> values;
            public Rect area;
            private static KF _searchKey;
            public void Ensure() {
                if(values == null) {
                    values = new ExposedList<KF>();
                }
            }
            public void AddKey(int curveIndex, float time, float value) {
                int index = IndexOf(time);
                if(index >= 0) {
                    values.array[index].keyFrame.value = value;
                }
                else {
                    index = ~index;
                    Keyframe k = new Keyframe(time, value);
                    KF kf = new KF(k, curveIndex);
                    values.Insert(index, kf);
                }
            }
            public bool HasKey(float time) {
                return IndexOf(time) >= 0;
            }
            public int IndexOf(float time) {
                _searchKey.SetTime(time);
                return Array.BinarySearch(values.array, 0, values.Count, _searchKey);
            }

            internal void RemoveKF(float time)
            {
                int index = IndexOf(time);
                if(index >= 0) {
                    values.RemoveAt(index);
                }
            }
        }
        private void UpdateCurveCount(int count) {
            var oldCount = _keys.Count;
            _keys.SetCountFast(count);
            var arr = _keys.array;
            for(int i = oldCount; i < count; i++) {
                arr[i].Ensure();
            }
        }
        private KF this[KFPath index]
        {
            get {
                return _keys.array[index.curve].values.array[index.index];
            }
        }
        private bool TrySetTime(KFPath path, float time) {
            if (!TryGetKF(path, out KF kf)) {
                return false;
            }
            var kfs = _keys.array[path.curve].values;
            kfs.array[path.index].SetTime(time);
            return true;
        }
        private bool TryGetKF(KFPath path, out KF kf) {
            if (_keys.Count <= path.curve) {
                kf = default(KF);
                return false;
            }
            var kfs = _keys.array[path.curve].values;
            if (kfs.Count <= path.index) {
                kf = default(KF);
                return false;
            }
            kf = kfs.array[path.index];
            return true;
        }
        private class KeyGroups: IComparer<KeyGroups.Group> {
            private AnimationWindow _window;
            private ExposedList<Group> _groups;
            public Rect area;

            public int Count => _groups.Count;

            internal void DrawRects(Rect rect) {
                area = rect;
                _groups.ClearFast();
                var keys = _window._keys;
                for(int i = 0; i < keys.Count; i++) {
                    var a = keys.array[i];
                    for(int j = 0; j < a.values.Count; j++) {
                        AddKey(new KFPath(i, j), a.values.array[j].time);
                    }
                }
                var ga = _groups.array;
                for(int i = 0; i < _groups.Count; i++) {
                    var g = ga[i];
                    if(g.time >= _window._startTime && g.time <= _window._endTime) {
                        var selected = IsSelected(g);
                        ga[i].rect = _window.DrawCurveNode(rect, g.time, Color.gray, selected);
                    }
                }
                
            }
            private bool IsSelected(Group g) {
                for(int i = 0; i < g.keys.Count; i++) {
                    if (!_window._selectionRect.Contains(g.keys[i])) {
                        return false;
                    }
                }
                return true;
            }
            private void AddKey(KFPath path, float time) {
                var array = _groups.array;
                for(int i = 0; i < _groups.Count; i++) {
                    if(Mathf.Approximately(time, array[i].time)) {
                        array[i].keys.Add(path);
                        return;
                    }
                }
                var index = _groups.AddFast();
                array = _groups.array;
                array[index].time = time;
                if (array[index].keys == null) {
                    array[index].keys = new List<KFPath>();
                }
                else {
                    array[index].keys.Clear();
                }
                array[index].keys.Add(path);
            }

            int IComparer<Group>.Compare(Group x, Group y) {
                throw new NotImplementedException();
            }

            internal bool TryFindGroup(Vector2 position, out Group group) {
                var a = area;
                a.xMin -= c_nodeWidth;
                if (a.Contains(position)) {
                    for (int i = 0; i < _groups.Count; i++) {
                        if (_groups.array[i].rect.Contains(position)) {
                            group = _groups.array[i];
                            return true;
                        }
                    }
                }
                group = default;
                return false;
            }

            internal KeyGroups(AnimationWindow animationWindow) {
                _window = animationWindow;
                _groups = new ExposedList<Group>();
            }
            public struct Group {
                public List<KFPath> keys;
                public Rect rect;
                public float time;
            }
        }
        private class ScaleHandler {
            private AnimationWindow _window;
            private Rect _left, _right;
            private float _minTime, _maxTime;
            private bool _enabled;
            private float _originTime;
            private float _startMouseTime;
            public ScaleHandler(AnimationWindow window) {
                _window = window;
            }
            public void DrawVisuals() {
                var s = _window._selectionRect.GetSelection();
                float ymin = float.PositiveInfinity;
                float ymax = float.NegativeInfinity;
                _minTime = float.PositiveInfinity;
                _maxTime = float.NegativeInfinity;
                float xmin = float.PositiveInfinity, xmax = float.NegativeInfinity;
                while (s.MoveNext()) {
                    var c = s.Current;
                    var area = _window._keys.array[c.curve].area;
                    var areaMax = area.yMax;
                    if (areaMax > ymax) {
                        ymax = areaMax;
                    }
                    var areaMin = area.yMin;
                    if (areaMin < ymin) {
                        ymin = areaMin;
                    }
                    _window.TryGetKF(c, out KF kf);
                    float time = kf.time;
                    if (_minTime > time) {
                        _minTime = time;
                        xmin = _window.GetPosForTime(area, time) - 0.5f * c_nodeWidth;
                    }
                    if (_maxTime < time) {
                        _maxTime = time;
                        xmax = _window.GetPosForTime(area, time) + 0.5f * c_nodeWidth;
                    }
                }
                _enabled = !Mathf.Approximately(_minTime, _maxTime);
                if (!_enabled) {
                    return;
                }
                _left = new Rect(xmin - c_space - c_scaleHandleWidth, ymin, c_scaleHandleWidth, ymax - ymin);
                _right = new Rect(xmax + c_space, ymin, c_scaleHandleWidth, ymax - ymin);

                //Debug.Log(_left + " ---------- " + _right);
                EditorGUI.DrawRect(_left, new Color(0.5f, 0.5f, 1f, 1f));
                EditorGUI.DrawRect(_right, new Color(0.5f, 0.5f, 1f, 1f));
            }
            public bool Press() {
                if (!_enabled) {
                    return false;
                }
                _window._mouseStart = Event.current.mousePosition;
                if (_left.Contains(_window._mouseStart)) {
                    _originTime = _maxTime;
                    _startMouseTime = _window.GetTimeFromMouse();
                    _window.StartEdit();
                    return true;
                }
                if (_right.Contains(_window._mouseStart)) {
                    _originTime = _minTime;
                    _startMouseTime = _window.GetTimeFromMouse();
                    _window.StartEdit();
                    return true;
                }
                return false;
            }

            public void Drag() {
                //var mp = Event.current.mousePosition;
                float mouseTime = _window.GetTimeFromMouse();
                float scale = (mouseTime - _originTime) / (_startMouseTime - _originTime);
                //Debug.Log(scale + " " + _originTime);
                _window._selectionRect.Scale(_originTime, scale);
                _window.Repaint();
                _window.UpdateVisuals();
            }
            public void Release() {
                _window.EndEdit();
                _window.UpdateVisuals();
                _window.UpdateCurves();
            }
        }
        private struct SelectionRect {
            private AnimationWindow _window;
            private Dictionary<KFPath, float> _selection;
            private Rect _rect;
            private bool _isOpen;
            public int Count => _selection.Count;
            private float _lastOffset;
            private List<KFPath> _tempKeys;

            //private Operation _lastOperation;
            
            //enum Operation {
            //    None = 0,
            //    Offset,
            //    Scale,
            //}
            public Dictionary<KFPath, float>.KeyCollection.Enumerator GetSelection() {
                return _selection.Keys.GetEnumerator();
            }
            public bool Contains(KFPath path) {
                return _selection.ContainsKey(path);
            }
            public KFPath SingleSelection { 
                set {
                    _selection.Clear();
                    _selection[value] = _window[value].time;
                }
            }
            public void AddRange(List<KFPath> paths) {
                for(int i = 0; i < paths.Count; i++) {
                    _selection[paths[i]] = _window[paths[i]].time;
                }
            }
            internal void Add(KFPath path) {
                _selection.Add(path, _window[path].time);
            }
            public bool SelectAll() {
                bool refresh = false;
                int count = 0;
                for(int i = 0; i < _window._keys.Count && !refresh; i++) {
                    var keys = _window._keys.array[i];
                    for(int j = 0; j < keys.values.Count; j++) {
                        count++;
                        if(!_selection.ContainsKey(new KFPath(i, j))) {
                            refresh = true;
                            break;
                        }
                    }
                }
                if(!refresh && count != _selection.Count) {
                    refresh = true;
                }
                if(refresh) {
                    _selection.Clear();
                    for(int i = 0; i < _window._keys.Count; i++) {
                        var keys = _window._keys.array[i];
                        var arr = keys.values.array;
                        for(int j = 0; j < keys.values.Count; j++) {
                            KFPath p = new KFPath(i, j);
                            var time = arr[j].time;
                            _selection.Add(p, time);
                        }
                    }
                }
                return refresh;
            }
            public void PrepareForOperation() {
                var e = _selection.Keys.GetEnumerator();
                while (e.MoveNext()) {
                    var c = e.Current;
                    _tempKeys.Add(c);
                }
                for(int i = 0; i < _tempKeys.Count; i++) {
                    var c = _tempKeys[i];
                    if (_window.TryGetKF(c, out KF kf)) {
                        _selection[c] = kf.time;
                    }
                }
                _tempKeys.Clear();
            }
            //private void UpdateCurrentOperation(Operation op) {
            //    if (op != _lastOperation) {
            //        _lastOperation = op;
            //        UpdateStoredTimes();
            //    }
            //}
            public float TimeOffset { 
                set {
                    //UpdateCurrentOperation(Operation.Offset);
                    foreach(var p in _selection) {
                        _window.TrySetTime(p.Key, _window.LimitTime(p.Value + value));
                    }
                }
            }

            internal void Init(AnimationWindow window)
            {
                _window = window;
                _selection = new Dictionary<KFPath, float>();
                _tempKeys = new List<KFPath>();
            }

            internal void Scale(float origin, float scale) {
                //UpdateCurrentOperation(Operation.Scale);
                foreach (var p in _selection) {
                    _window.TrySetTime(p.Key, _window.LimitTime((p.Value - origin) * scale + origin));
                }
            }

            internal void Clear() {
                _selection.Clear();
            }

            private struct Node {
                public KFPath path;
                public float startTime;
            }
        }
    }
}