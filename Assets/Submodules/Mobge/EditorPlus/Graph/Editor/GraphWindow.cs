using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Text;

namespace Mobge.Graph {
    public class GraphWindow : EditorWindow {

        private static ExposedList<float> s_tempData = new ExposedList<float>();
        private static StringBuilder s_string = new StringBuilder();

        [MenuItem("Mobge/Graph Window")]
        public static void Init() {
            GetWindow<GraphWindow>("Graph Window");
        }

        public GraphDataManager graphManager;
        private EditorFoldGroups _groups;
        private EditorTools _tools;
        private HashSet<GraphPanel> _visiblePanels;

        private float MinGraphHeight => 30;
        private float ExtenderThickness => 5;
        private float DefaultGraphHeight => 200;

        private bool _isGuiInitialized = false;
        private GUIStyle _skinLabelRight;

        private GUIContent _preciseLocationLabel;
        private Vector2 _scroll;

        private void OnEnable() {
            _groups = new EditorFoldGroups(EditorFoldGroups.FilterMode.NoFilter);
            _visiblePanels = new HashSet<GraphPanel>();
            _preciseLocationLabel = new GUIContent();

            _tools = new EditorTools();
            _tools.AddTool(new EditorTools.Tool("drag view") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0,
                },
                //onPress
            });
        }

        protected void OnGUI() {

            if (!_isGuiInitialized) {
                _isGuiInitialized = true;


                _skinLabelRight = new GUIStyle(GUI.skin.label);
                _skinLabelRight.alignment = TextAnchor.MiddleRight;
                _skinLabelRight.normal.textColor = Color.white;
            }

            if (graphManager == null) {
                graphManager = GraphDataManager.Instance;
            }
            _visiblePanels.Clear();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _groups.GuilayoutField(CreateGroups);
            EditorGUILayout.EndScrollView();
            if (Event.current.type == EventType.Repaint) {
                DrawPreciseLocation();
            }
        }

        private void DrawPreciseLocation() {
            //if (_tools.ActiveTool == null) 
            {
                //EditorGUI.DrawRect(new Rect(Event.current.mousePosition, new Vector2(10f, 10f)), Color.gray);
                this.Repaint();
                var en = _visiblePanels.GetEnumerator();
                var mouseLocation = Event.current.mousePosition;
                while (en.MoveNext()) {
                    var value = en.Current;
                    if (value.TryFindClosestSample(mouseLocation, out int sampleIndex, out float xValue)) {
                        // Debug.Log(mouseLocation + " " + sampleIndex + " " + xValue);
                        s_tempData.SetCountFast(value.data.ColumnCount);
                        value.data.GetRow(sampleIndex, s_tempData.array);
                        float uniformX = value.horizontalRange.ToUniform(xValue);
                        float yValue = value.data.GetData(sampleIndex, 0);
                        float uniformY = 1f - value.verticalRange.ToUniform(yValue);
                        Vector2 graphPosition = value.graphRect.UniformToRect(new Vector2(uniformX, uniformY));
                        _preciseLocationLabel.text = "x: " + xValue + "\ny:" + yValue;
                        var labelSize = GUI.skin.label.CalcSize(_preciseLocationLabel);
                        graphPosition.y -= labelSize.y;
                        Rect labelRect = new Rect(graphPosition, labelSize);

                        EditorGUI.DrawRect(labelRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                        EditorGUI.LabelField(labelRect, _preciseLocationLabel);
                        this.Repaint();
                    }
                }
                en.Dispose();
            }
        }

        private void CreateGroups(EditorFoldGroups.Group obj) {
            obj.AddChild("options", () => {
                if(GUILayout.Button("clear data")) {
                    this.graphManager.Graphs.Clear();
                    _groups.Refresh();
                }
                if (GUILayout.Button("refresh")) {
                    _groups.Refresh();
                }
            });

            var g = graphManager.Graphs;
            foreach (var pair in g) {
                var graph = pair.Key;
                if (pair.Value.ColumnCount > 0 && pair.Value.RowCount > 0) {
                    obj.AddChild(graph, () => {
                        DrawGraph(obj, pair.Value);
                        for(int i = 0; i < pair.Value.ColumnCount; i++) {
                            var ci = pair.Value.ColumnInfos[i];
                            EditorGUILayout.LabelField("column " + i, "ordered: " + ci.Ordered);
                        }
                    });
                }
            }
        }

        private void DrawGraph(EditorFoldGroups.Group obj, GraphData data) {
            var g = _groups.GetObject<GraphPanel>("panel", null);
            if (g == null) {
                g = new GraphPanel(this, DefaultGraphHeight);
                _groups.SetObject("panel", g);
            }
            var rect = GUILayoutUtility.GetRect(1f, g.height);
            if (Event.current.type == EventType.Repaint) {
                g.DrawGraph(data, rect);
                rect.y = rect.yMax;
                rect.height = this.ExtenderThickness;
                g.splitRect = rect;
                _visiblePanels.Add(g);
            }
        }
        private static Range CalculateRange(GraphData data, int columnIndex) {
            int rowCount = data.RowCount;
            Range r;
            r.min = data.GetData(0, columnIndex);
            r.max = r.min;
            for (int i = 1; i < rowCount; i++) {
                float sample = data.GetData(i, columnIndex);
                if(r.min > sample) {
                    r.min = sample;
                }
                else if(r.max < sample) {
                    r.max = sample;
                }
            }
            return r;
        }
        private struct Range {
            public float min, max;
            public float ToUniform(float value) {
                return (value - min) / (max - min);
            }
            public float Scale(float uniformValue) {
                return uniformValue * (max - min) + min;
            }
        }
        private class GraphPanel {
            public float height;
            private GraphWindow window;
            public Rect splitRect;
            public Rect graphRect;
            public GraphData data;
            public Range horizontalRange, verticalRange;
            public Color BackgroundColor => Color.black;
            public Color AxisColor => new Color(1, 0, 0, 0.5f);
            public Color GraphColor => new Color(0, 1, 1);
            public Color LabelColor => new Color(1, 1, 1);

            public float VerticalLabelSpace => 50f;
            public float HorizontalLabelSpace => 200f;
            public float TextOffset => 5f;

            public RectOffset GraphOffset {
                get {
                    return new RectOffset(100, 15, 15, 50);
                }
            }
            public Vector2 LabelPerOffset {
                get {
                    return new Vector2();
                }
            }

            public GraphPanel(GraphWindow window, float height) {
                this.height = height;
                this.window = window;
            }

            public bool TryFindClosestSample(Vector2 worldPosition, out int sampleIndex, out float xValue) {
                if (!graphRect.Contains(worldPosition)) {
                    sampleIndex = -1;
                    xValue = 0;
                    return false;
                }
                var normalized = graphRect.RectToUniform(worldPosition);

                // EditorGUI.DrawRect(new Rect( graphRect.UniformToRect(normalized), new Vector2(10,10)), Color.red);
                
                int index;
                if (data.HorizontalColumnIndex < 0) {
                    int rowCount = data.RowCount;
                    index = Mathf.RoundToInt(rowCount * normalized.x);
                    index = Mathf.Min(index, rowCount-1);
                    xValue = index;
                }
                else {
                    index = data.FindClosestIndex(data.HorizontalColumnIndex, this.horizontalRange.Scale(normalized.x));
                    xValue = data.GetData(index, data.HorizontalColumnIndex);
                }
                sampleIndex = index;
                return true;
            }


            public void DrawGraph(GraphData data, Rect rect) {
                var defHandlesMatrix = Handles.matrix;
                var defHandlesColor = Handles.color;
                Handles.matrix = GUI.matrix;

                this.data = data;

                s_tempData.SetCountFast(data.ColumnCount);
                Range vRange = CalculateRange(data, 0);
                verticalRange = vRange;


                Range hRange;
                if (data.HorizontalColumnIndex < 0) {
                    hRange.min = 0;
                    hRange.max = data.RowCount;
                }
                else {
                    hRange = CalculateRange(data, data.HorizontalColumnIndex);
                }
                horizontalRange = hRange;

                var offset = GraphOffset;
                graphRect = offset.Remove(rect);

                // draw background
                EditorGUI.DrawRect(rect, BackgroundColor);

                // draw bars
                Handles.color = this.GraphColor;
                int rowCount = data.RowCount;
                for (int i = 0; i < rowCount; i++) {
                    data.GetRow(i, s_tempData.array);
                    Vector2 value;
                    value.y = s_tempData.array[0];
                    if(data.HorizontalColumnIndex < 0) {
                        value.x = i;
                    }
                    else {
                        value.x = s_tempData.array[data.HorizontalColumnIndex];
                    }

                    value.x = hRange.ToUniform(value.x);
                    value.y = 1 - vRange.ToUniform(value.y);

                    var coordinate = graphRect.UniformToRect(value);
                    Handles.DrawLine(coordinate, new Vector3(coordinate.x, graphRect.yMax, 0));
                }

                // draw axis
                Handles.color = AxisColor;
                Handles.DrawLine(graphRect.TopLeft(), graphRect.BottomLeft());
                Handles.DrawLine(graphRect.BottomRight(), graphRect.BottomLeft());

                // draw vertical labels
                Handles.color = Color.white;
                float labelH = EditorGUIUtility.singleLineHeight;
                for (float f = graphRect.yMax; f >= graphRect.yMin; f -= VerticalLabelSpace) {
                    Rect labelRect = new Rect(rect.xMin, f - labelH * 0.5f, this.GraphOffset.left - this.TextOffset, labelH);
                    var uniform = graphRect.RectToUniform(new Vector2(graphRect.xMin, f));
                    uniform.y = 1 - uniform.y;
                    var value = vRange.Scale(uniform.y);
                    GUI.Label(labelRect, value.ToString(), window._skinLabelRight);
                    Handles.DrawLine(new Vector3(this.graphRect.xMin, f, 0), new Vector3(this.graphRect.xMin - this.TextOffset, f, 0));

                }

                // draw horizontal labels
                for(float f = graphRect.xMin; f< graphRect.xMax; f += HorizontalLabelSpace) {
                    Rect labelRect = new Rect(f, this.graphRect.yMax + this.TextOffset, this.GraphOffset.left - this.TextOffset, labelH);
                    var uniform = graphRect.RectToUniform(new Vector2(f, graphRect.yMax));
                    var value = hRange.Scale(uniform.x);
                    GUI.Label(labelRect, value.ToString());
                    Handles.DrawLine(new Vector3(f, this.graphRect.yMax, 0), new Vector3(f, this.graphRect.yMax + this.TextOffset, 0));

                }


                Handles.color = defHandlesColor;
                Handles.matrix = defHandlesMatrix;
            }
        }
    }
}