using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Mobge.Core;
using Mobge;
using UnityEditor.AnimatedValues;
using System.Linq;

namespace Mobge.Core.Components
{
    [CustomEditor(typeof(Movement3DComponent))]
    public class EMovement3DComponent : EComponentDefinition
    {
        public override EditableElement CreateEditorElement(BaseComponent dataObject)
        {
            return new Editor((Movement3DComponent.Data)dataObject, this);
        }
        public class Editor : EditableElement<Movement3DComponent.Data>
        {
            private Movement3DDrawer _movement3DDrawer;
            private EditorTools _editorTools;
            private bool _editMode;
            private ExposedList<Movement3DComponent.Data.MovingBody3D> _movingBodies;
            private AnimBool _showOffsets, _showOffsetOrdered, _showOffsetManual;
            private bool _offsetFoldout, _curveFoldout, _positionCurveFoldout, _rotationCurveFoldout;
            private List<int> _logicElementIds;
            private bool _logicElementIdFirstInit = false;
            private GUIStyle _connectionIdLabelStyle;
            public Editor(Movement3DComponent.Data component, EMovement3DComponent editor) : base(component, editor)
            {
                _editorTools = new EditorTools();
                _movement3DDrawer = new Movement3DDrawer();
                _movement3DDrawer.onUpdateFromWindowExtra = MovementUpdateFromWindow;
                _movement3DDrawer.onDataChange = UpdateData;

                _connectionIdLabelStyle = GUIStyle.none;
                _connectionIdLabelStyle.fontSize = 25;
                _connectionIdLabelStyle.normal.textColor = Color.white;

                InitAnimBools();

                DataObjectT.InitUpdateRangeFunctions();
                EditorApplication.delayCall += InitMovingBodies;
                EditorApplication.delayCall += InitLogicElementIDList;
            }
            private void InitLogicElementIDList()
            {
                if (DataObjectT.offsets.Count == 0)
                {
                    DataObjectT.offsets.Add(new Movement3DComponent.Data.OffsetInfo());
                }
                _logicElementIds = new List<int>();
                var e = GetConnections(0);
                while (e.MoveNext())
                {
                    var curr = e.Current;
                    _logicElementIds.Add(curr.elemenet.Id);
                }
                _logicElementIdFirstInit = true;
            }
            public override void DrawGUILayout()
            {
                _editMode = ExclusiveEditField();
                var data = DataObjectT;
                data.position = EditorGUILayout.Vector3Field("Position", data.position);
                data.autoStart = EditorGUILayout.Toggle("Auto Start", data.autoStart);
                EditorGUI.BeginChangeCheck();
                DrawCurveData();
                data.offsetMode = (Movement3DComponent.Data.OffsetMode)EditorGUILayout.EnumPopup("Offset Mode", data.offsetMode);
                DrawOffsetInfos();
                data.mode = (Movement3DComponent.Data.Mode)EditorGUILayout.EnumPopup("Mode", data.mode);
                data.movementDirection = (Movement3DComponent.Data.MovementDirection)EditorGUILayout.EnumPopup("Direction", data.movementDirection);
                if (EditorGUI.EndChangeCheck())
                {
                    DataObjectT.InitUpdateRangeFunctions();
                    InitMovingBodies();
                    _movement3DDrawer.UpdateVisualsFromWindow();
                }
            }
            private void InitMovingBodies()
            {
                var e = GetConnections(0);
                int index = 0;
                _movingBodies = new ExposedList<Movement3DComponent.Data.MovingBody3D>();
                while (e.MoveNext())
                {
                    DataObjectT.InitMovingBody(null, index, ref _movingBodies);
                    index++;
                }
            }
            private void UpdateData(Movement3D movement) {
                DataObjectT.movement = movement;
                UpdateData();
            }
            public override void UpdateData()
            {
                if (!_logicElementIdFirstInit) {
                    List<int> logicIdList = new List<int>();
                    var e = GetConnections(0);
                    while (e.MoveNext()) {
                        var curr = e.Current;
                        logicIdList.Add(curr.elemenet.Id);
                    }
                    var addedConnectionIdList = logicIdList.Except(_logicElementIds);
                    var removedConnectionIdList = _logicElementIds.Except(logicIdList);
                    var offsets = DataObjectT.offsets;
                    if (logicIdList.Count > 1) {
                        foreach (int id in addedConnectionIdList) {
                            offsets.Add(new Movement3DComponent.Data.OffsetInfo());
                        }
                    }
                    foreach (int id in removedConnectionIdList) {
                        if (offsets.Count > 1) {
                            var i = _logicElementIds.IndexOf(id);
                            offsets.RemoveAt(i);
                        }
                    }
                    _logicElementIds = logicIdList.ToList();
                }
                base.UpdateData();
            }
            public override bool SceneGUI(in SceneParams @params)
            {
                bool updated = false;
                if (_editMode) {
                    var parent = new Pose(@params.position, @params.rotation);
                    updated = _movement3DDrawer.OnSceneGUI(parent, DataObjectT.movement);
                    _editorTools.OnSceneGUI();
                    var e = GetConnections(0);
                    int index = 0;
                    while (e.MoveNext())
                    {
                        var element = e.Current.elemenet;
                        
                        Handles.Label(element.Position, index.ToString(), _connectionIdLabelStyle);
                        index++;
                    }
                }
                var t = Event.current.type;
                return updated || t == EventType.Used || t == EventType.MouseUp;
            }
            private void MovementUpdateFromWindow(Pose pose, float time) {
                var e = GetConnections(0);
                var arr = _movingBodies.array;
                var index = 0;

                while (e.MoveNext())
                {
                    var curr = e.Current;
                    //pose.position = pose.position;
                    // pose.rotation = pose.rotation;
                    curr.elemenet.SetPose(pose);
                    curr.elemenet.UpdateData();

                    index++;
                }
            }
            private void DrawCurveData()
            {
                var data = DataObjectT;
                _curveFoldout = EditorGUILayout.Foldout(_curveFoldout, "Curves", true);
                if (_curveFoldout) {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.indentLevel++;
                    _positionCurveFoldout = EditorGUILayout.Foldout(_positionCurveFoldout, "Position", true);
                    if (_positionCurveFoldout)
                    {
                        EditorGUI.indentLevel++;
                        var pc = data.movement.positionData.curves;
                        pc[0].UpdateKeys(EditorGUILayout.CurveField("X", pc[0].ToAnimationCurve()));
                        pc[1].UpdateKeys(EditorGUILayout.CurveField("Y", pc[1].ToAnimationCurve()));
                        pc[2].UpdateKeys(EditorGUILayout.CurveField("Z", pc[2].ToAnimationCurve()));
                        EditorGUI.indentLevel--;
                    }
                    _rotationCurveFoldout = EditorGUILayout.Foldout(_rotationCurveFoldout, "Rotation", true);
                    if (_rotationCurveFoldout)
                    {
                        EditorGUI.indentLevel++;
                        var rc = data.movement.rotationData.curves;
                        rc[0].UpdateKeys(EditorGUILayout.CurveField("X", rc[0].ToAnimationCurve()));
                        rc[1].UpdateKeys(EditorGUILayout.CurveField("Y", rc[1].ToAnimationCurve()));
                        rc[2].UpdateKeys(EditorGUILayout.CurveField("Z", rc[2].ToAnimationCurve()));
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                }
            }
            private void InitAnimBools()
            {
                _showOffsets = new AnimBool(false);
                _showOffsetOrdered = new AnimBool(false);
                _showOffsetManual = new AnimBool(false);

                _showOffsets.valueChanged.AddListener(Editor.Repaint);
                _showOffsetOrdered.valueChanged.AddListener(Editor.Repaint);
                _showOffsetManual.valueChanged.AddListener(Editor.Repaint);
            }
            private void DrawOffsetInfos()
            {
                var mode = DataObjectT.offsetMode;
                _showOffsets.target = mode != Movement3DComponent.Data.OffsetMode.None;
                if (EditorGUILayout.BeginFadeGroup(_showOffsets.faded))
                {
                    _offsetFoldout = EditorGUILayout.Foldout(_offsetFoldout, "Offsets", true);
                    {
                        _showOffsetOrdered.target = _offsetFoldout && mode == Movement3DComponent.Data.OffsetMode.Ordered;
                        if (EditorGUILayout.BeginFadeGroup(_showOffsetOrdered.faded))
                        {
                            DataObjectT.offsets[0] = DrawOffsetField(DataObjectT.offsets[0], 0);
                        }
                        EditorGUILayout.EndFadeGroup();

                        _showOffsetManual.target = _offsetFoldout && mode == Movement3DComponent.Data.OffsetMode.ManuallySet;
                        if (EditorGUILayout.BeginFadeGroup(_showOffsetManual.faded))
                        {
                            for (int i = 0; i < DataObjectT.offsets.Count; i++)
                            {
                                DataObjectT.offsets[i] = DrawOffsetField(DataObjectT.offsets[i], i);
                            }
                        }
                        EditorGUILayout.EndFadeGroup();
                    }
                }
                EditorGUILayout.EndFadeGroup();
            }
            private Movement3DComponent.Data.OffsetInfo DrawOffsetField(Movement3DComponent.Data.OffsetInfo offsetInfo, int index)
            {
                EditorGUILayout.LabelField("Offset: " + index.ToString());
                EditorGUI.indentLevel++;
                offsetInfo.time = EditorGUILayout.FloatField("time", offsetInfo.time);
                offsetInfo.position = EditorGUILayout.Vector3Field("position", offsetInfo.position);
                EditorGUI.indentLevel--;
                return offsetInfo;
            }
        }
    }
}