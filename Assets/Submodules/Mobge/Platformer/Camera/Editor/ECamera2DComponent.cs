using System;
using System.Collections;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.Core.Components;
using UnityEditor;
using UnityEngine;

namespace Mobge.Platformer {
    [CustomEditor(typeof(Camera2DComponent))]
    public class ECamera2DComponent : EComponentDefinition
    {
        private static Collider2DShapeEditor _shapeEditor = new Collider2DShapeEditor();
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor((Camera2DComponent.Data)dataObject, this);
        }
        public class Editor : EditableElement<Camera2DComponent.Data>
        {
            private bool _editMode;
            ECamera2DComponent _editor;
            private Mode _mode = Mode.Movement;
            private EditorTools _tools;
            public Editor(Camera2DComponent.Data component, ECamera2DComponent editor) : base(component, editor)
            {
                _editor = editor;
                InitTools();
            }
            private void InitTools() {
                _tools = new EditorTools();
                _tools.AddTool(new EditorTools.Tool("toggle mod") {
                    activation = new EditorTools.ActivationRule() {
                        key = KeyCode.S,
                    },
                    onRelease = () => {
                        _mode++;
                        if((int)_mode > 2) {
                            _mode = 0;
                        }
                    },
                });
            }
            public override void DrawGUILayout() {
                var comp = DataObjectT;
                if (comp.data == null) {
                    comp.data = level.GameSetup.camera.default2D.Clone();
                }
                _editMode = ExclusiveEditField();
                using (var changeCheck = new EditorGUI.ChangeCheckScope()) {
                    base.DrawGUILayout();
                    if (changeCheck.changed) {
                        _mode = Mode.Movement;
                    }
                }
                using(var changeCheck = new EditorGUI.ChangeCheckScope()) {
                    LimitField("x min", ref comp.xMin, -1);
                    LimitField("x max", ref comp.xMax, 1);
                    LimitField("y min", ref comp.yMin, -1);
                    LimitField("y max", ref comp.yMax, 1);
                    if (changeCheck.changed) {
                        _mode = Mode.Limits;
                    }
                }
                _mode = (Mode)EditorGUILayout.EnumPopup("mode", _mode);
                _shapeEditor.OnInspectorGUI(ref comp.shape);
            }
            private static void LimitField(string label, ref float value, float @defaultValue) {
                EditorGUILayout.BeginHorizontal();
                bool exists = !float.IsNaN(value);
                var val = EditorGUILayout.Toggle(exists, UnityEngine.GUILayout.Width(20));
                if(val != exists) {
                    if(val) {
                        value = @defaultValue;
                    }
                    else {
                        value = float.NaN;
                    }
                }
                if(val) {
                    value = EditorGUILayout.FloatField(label, value);
                }
                else {
                    EditorGUILayout.LabelField(label);
                }
                EditorGUILayout.EndHorizontal();
            }
            public override bool SceneGUI(in SceneParams @params) {
                bool edited = false;
                var mat = ElementEditor.BeginMatrix(@params.matrix);
                if (@params.selected) {
                    switch (_mode) {
                        case Mode.Limits:
                            UpdateLimits();
                            break;
                        case Mode.Movement:
                            UpdateMovement2();
                            break;
                        default:
                            break;
                    }
                }
                UpdateTrigger();
                if(_editMode) {
                    _tools.OnSceneGUI();
                    var t = Event.current.type;
                    edited = t == EventType.Used || t == EventType.MouseUp;
                    
                }
                ElementEditor.EndMatrix(mat);
                return edited;
            }
            private void UpdateLimits() {
                if (_editMode) {
                    var comp = DataObjectT;
                    Handles.color = new Color(1, 0, 0, 1);
                    LimitHandle(Vector2.right, true, ref comp.xMin);
                    LimitHandle(Vector2.right, false, ref comp.xMax);
                    LimitHandle(Vector2.up, true, ref comp.yMin);
                    LimitHandle(Vector2.up, false, ref comp.yMax);
                }
            }
            private void UpdateTrigger() {
                _shapeEditor.OnSceneGUI(ref DataObjectT.shape, _editMode && _mode == Mode.Trigger);
            }
            private void UpdateMovement2() {
                if (!_editMode) {
                    return;
                }
                var setup = level.GameSetup;
                if (setup == null) {
                    return;
                }
                //UnityEngine.UI.CanvasScaler sd;
               var data = DataObjectT.data;
                float z = data.zOffset;
                string[] res = UnityStats.screenRes.Split('x');
                var sceneSize = new Vector2(int.Parse(res[0]), int.Parse(res[1]));
                var side2dCam = FindObjectOfType<Side2DCamera>();
                var fov = setup.camera.fov;
                if (side2dCam != null) {
                    z = side2dCam.Abstract2Real(z, sceneSize);
                    fov = side2dCam.camera.fieldOfView;
                }
                var frameSize = Side2DCamera.CalculateFrameSize(fov, sceneSize.x / sceneSize.y /*setup.camera.aspectRatio*/, z);
                var frameExtends = frameSize * 0.5f;
                var area = DataObjectT.shape.CalculateBounds();
                Vector2 offset = new Vector2(frameExtends.x * data.horizontalOffset, frameExtends.y * -data.verticalOffset);
                var min = area.min * data.movementRate + offset - frameExtends;
                var max = area.max * data.movementRate + offset + frameExtends;
                var r = new Rect(min, max - min);
                if (data.movementRate.x < 0 || data.movementRate.y < 0) {
                    Handles.color = Color.red;
                }
                else {
                    Handles.color = Color.black;
                }
                Handles.DrawSolidRectangleWithOutline(r, Color.clear, Color.white);
                var t = Event.current.type;
                r = HandlesExtensions.RectHandle(r);
                if (t != EventType.Used && Event.current.type == EventType.Used) {
                    UpdateValuesFromRect2(data, r, frameExtends, area);
                }
            }
            static void UpdateValuesFromRect2(Side2DCameraData data, Rect r, Vector2 frameExtends, Bounds area) {
                var min = r.min + frameExtends;
                var max = r.max - frameExtends;
                var diagonal = max - min;
                var size = area.size;
                Vector2 movementRate;
                if (area.size.x == 0) {
                    movementRate.x = 0;
                }
                else {
                    movementRate.x = diagonal.x / size.x;
                }
                if (area.size.y == 0) {
                    movementRate.y = 0;
                }
                else {
                    movementRate.y = diagonal.y / size.y;
                }
                Vector2 offset = (min - area.min * movementRate) / frameExtends;
                offset.y = -offset.y;
                data.horizontalOffset = offset.x;
                data.verticalOffsetAir = offset.y;
                data.verticalOffset = data.verticalOffsetAir;
                data.movementRate = movementRate;
            }
            //private Vector2 Lerp(Vector2 a, Vector2 b, Vector2 t) {
            //    return new Vector2(Mathf.LerpUnclamped(a.x, b.x, t.x), Mathf.LerpUnclamped(a.y, b.y, t.y));
            //}
            private static void LimitHandle(Vector2 direction, bool negative, ref float value) {
                if(!float.IsNaN(value)) {
                    var p = Handles.Slider(direction * value, negative ? -direction : direction);
                    value = Vector2.Dot(p, direction);
                    Vector3 perpendicular = new Vector2(-direction.y, direction.x) * value;
                    Handles.DrawLine(p + perpendicular, p - perpendicular);
                }
            }
            public enum Mode {
                Limits = 0,
                Movement = 1,
                Trigger = 2,
            }
        }
    }
}