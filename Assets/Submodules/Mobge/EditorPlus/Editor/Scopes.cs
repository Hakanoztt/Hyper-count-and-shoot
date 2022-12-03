using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mobge {
    public static class Scopes {
        //GUI Color
        public static GUIColorEnd GUIColor(Color color) {
            var oldColor = GUI.color;
            GUI.color = color;
            return new GUIColorEnd(oldColor);
        }
        public struct GUIColorEnd : IDisposable {
            private readonly Color _oldColor;
            public GUIColorEnd(Color oldColor) {
                _oldColor = oldColor;
            }
            public void Dispose() {
                GUI.color = _oldColor;
            }
        }

        //GUI Background Color
        public static GUIBackgroundColorEnd GUIBackgroundColor(Color color) {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            return new GUIBackgroundColorEnd(oldColor);
        }
        public struct GUIBackgroundColorEnd : IDisposable {
            private readonly Color _oldColor;
            public GUIBackgroundColorEnd(Color oldColor) {
                _oldColor = oldColor;
            }
            public void Dispose() {
                GUI.backgroundColor = _oldColor;
            }
        }

        //GUI Enabled
        public static GUIEnabledEnd GUIEnabled(bool enabled) {
            var oldEnabled = GUI.enabled;
            GUI.enabled = enabled;
            return new GUIEnabledEnd(oldEnabled);
        }
        public struct GUIEnabledEnd : IDisposable {
            private readonly bool _oldEnabled;
            public GUIEnabledEnd(bool oldEnabled) {
                _oldEnabled = oldEnabled;
            }
            public void Dispose() {
                GUI.enabled = _oldEnabled;
            }
        }

        //Handles Matrix
        public static HandlesMatrixEnd HandlesMatrix(Matrix4x4 matrix) {
            var oldMatrix = Handles.matrix;
            Handles.matrix = matrix;
            return new HandlesMatrixEnd(oldMatrix);
        }
        public struct HandlesMatrixEnd : IDisposable {
            private readonly Matrix4x4 _oldMatrix;
            public HandlesMatrixEnd(Matrix4x4 oldMatrix) {
                _oldMatrix = oldMatrix;
            }
            public void Dispose() {
                Handles.matrix = _oldMatrix;
            }
        }

        //Handles Color
        public static HandlesColorEnd HandlesColor(Color color) {
            var oldColor = Handles.color;
            Handles.color = color;
            return new HandlesColorEnd(oldColor);
        }
        public struct HandlesColorEnd : IDisposable {
            private readonly Color _oldColor;
            public HandlesColorEnd(Color oldColor) {
                _oldColor = oldColor;
            }
            public void Dispose() {
                Handles.color = _oldColor;
            }
        }

        //Handles GUI
        public static HandlesGUIEnd HandlesGUI() {
            Handles.BeginGUI();
            return new HandlesGUIEnd();
        }
        public struct HandlesGUIEnd : IDisposable {
            public void Dispose() {
                Handles.EndGUI();
            }
        }

        //GUI Indent
        public static GUIIndentEnd GUIIndent(int indent) {
            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = indent;
            return new GUIIndentEnd(oldIndent);
        }
        public struct GUIIndentEnd : IDisposable {
            private readonly int _oldIndent;
            public GUIIndentEnd(int oldIndent) {
                _oldIndent = oldIndent;
            }
            public void Dispose() {
                EditorGUI.indentLevel = _oldIndent;
            }
        }

        //Scroll View
        public static ScrollViewEnd ScrollView(ref Vector2 scrollPosition) {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            return new ScrollViewEnd();
        }
        public struct ScrollViewEnd : IDisposable {
            public void Dispose() {
                GUILayout.EndScrollView();
            }
        }

        //Vertical
        public static VerticalEnd Vertical(GUIStyle style) {
            EditorGUILayout.BeginVertical(style);
            return new VerticalEnd();
        }
        public struct VerticalEnd : IDisposable {
            public void Dispose() {
                EditorGUILayout.EndVertical();
            }
        }
        
        //Horizontal
        public static HorizontalEnd Horizontal(GUIStyle style = null) {
            if (style == null) {
                EditorGUILayout.BeginHorizontal();
            }
            else {
                EditorGUILayout.BeginHorizontal(style);
            }
            return new HorizontalEnd();
        }
        public struct HorizontalEnd : IDisposable {
            public void Dispose() {
                EditorGUILayout.EndHorizontal();
            }
        }

        //On Change Dirty
        public static OnChangeDirtyEnd OnChangeDirty(Object o) {
            EditorGUI.BeginChangeCheck();
            return new OnChangeDirtyEnd(o);
        }
        public struct OnChangeDirtyEnd : IDisposable {
            private readonly Object _o;
            public OnChangeDirtyEnd(Object o) {
                _o = o;
            }
            public void Dispose() {
                if (EditorGUI.EndChangeCheck()) {
                    if (_o != null) {
                        EditorExtensions.SetDirty(_o);
                    }
                }
            }
        }

        //On Change Action
        public static OnChangeActionEnd OnChangeAction(Action action) {
            EditorGUI.BeginChangeCheck();
            return new OnChangeActionEnd(action);
        }
        public struct OnChangeActionEnd : IDisposable {
            private readonly Action _action;
            public OnChangeActionEnd(Action action) {
                _action = action;
            }
            public void Dispose() {
                if (EditorGUI.EndChangeCheck()) {
                    if (_action != null) {
                        _action.Invoke();
                    }
                }
            }
        }

        //Label Width
        public static LabelWidthEnd LabelWidth(float labelWidth) {
            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = labelWidth;
            return new LabelWidthEnd(oldLabelWidth);
        }
        public struct LabelWidthEnd : IDisposable {
            private readonly float _oldLabelWidth;
            public LabelWidthEnd(float oldLabelWidth) {
                _oldLabelWidth = oldLabelWidth;
            }
            public void Dispose() {
                EditorGUIUtility.labelWidth = _oldLabelWidth;
            }
        }
    }
}