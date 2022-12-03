using Mobge.Core;
using System;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.Components
{
    [CustomEditor(typeof(PoseToPoseMovementComponent))]
    public class EPoseToPoseMovementComponent : EComponentDefinition
    {

        private static PointEditor<PoseToPoseMovementComponent.Pose> s_pointEditor = new PointEditor<PoseToPoseMovementComponent.Pose>((v) => v.position, (ref PoseToPoseMovementComponent.Pose point, Vector3 val) => point.position = val, new PointEditor<PoseToPoseMovementComponent.Pose>.VisualSettings() {
            mode = PointEditor<PoseToPoseMovementComponent.Pose>.Mode.Point,
        });
        private static UnityEditor.IMGUI.Controls.ArcHandle s_arcHandle = new UnityEditor.IMGUI.Controls.ArcHandle();
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as PoseToPoseMovementComponent.Data, this);
        }
        public class Editor : EditableElement<PoseToPoseMovementComponent.Data>
        {
            private bool _editMode;
            private int _iCorner = -1;
            private EditorTools _editorTools;
            private EditorPopup _popup;
            public Editor(PoseToPoseMovementComponent.Data component, EComponentDefinition editor) : base(component, editor) {
                _editorTools = new EditorTools();
                _editorTools.AddTool(new EditorTools.Tool("close popup") {
                    activation = new EditorTools.ActivationRule() {
                        mouseButton = 0,
                    },
                    onPress = () => {
                        //Debug.Log("is null: " + (_popup == null));
                        if (_popup != null) {
                            _popup = null;
                            return true;
                        }
                        return false;
                    }
                });
            }
            // In this method, implement the logic you would normally implement under OnInspectorGUI() 
            public override void DrawGUILayout() {
                // Inspector: Boiler plate for exclusive editing of the element
                _editMode = ExclusiveEditField("edit on scene");
                base.DrawGUILayout();
            }
            // In this method, implement the logic you would normally implement under OnSceneGUI() 
            public override bool SceneGUI(in SceneParams @params) {
                // Scene: Boiler plate for exclusive editing of the element
                bool enabled = @params.selected && _editMode;

                var temp = ElementEditor.BeginMatrix(@params.matrix);
                var data = DataObjectT;
                if (data.poses == null) {
                    data.poses = new PoseToPoseMovementComponent.Pose[0];
                }
                for (int i = 0; i < data.poses.Length; i++) {
                    var pose = data.poses[i];
                    var size = HandleUtility.GetHandleSize(pose.position) * 0.5f;
                    var rad = pose.angle * Mathf.Deg2Rad;
                    Handles.DrawLine(pose.position, pose.position + size * new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)));
                    Handles.SphereHandleCap(0, pose.position, Quaternion.identity, size * 0.2f, Event.current.type);
                    string l;
                    if(pose.AutoPlay >= 0) {
                        l = i + "->" + pose.AutoPlay;
                    }
                    else {
                        l = i.ToString();
                    }
                    ElementEditor.visualHandler.DrawLabel(pose.position + size * new Vector2(0.0f,0.3f), l, new Color(0.5f, 0.5f, 1f, 0.7f));
                }
                if (enabled) {
                    if (_iCorner >= 0 && _iCorner < data.poses.Length) {
                        Handles.color = Color.white;
                        var tm = Handles.matrix;
                        Handles.matrix = tm * Matrix4x4.TRS(data.poses[_iCorner].position, Quaternion.Euler(0, 90, 90), Vector3.one);
                        s_arcHandle.radius = HandleUtility.GetHandleSize(Vector3.zero) * 0.5f;
                        s_arcHandle.angle = data.poses[_iCorner].angle;
                        s_arcHandle.DrawHandle();
                        data.poses[_iCorner].angle = s_arcHandle.angle;
                        Handles.matrix = tm;
                    }
                    if (Event.current.type != EventType.Used) {
                        if (s_pointEditor.SelectedPolygon == 0 && s_pointEditor.SelectedCornerConunt == 1) {
                            var e = s_pointEditor.Selection.GetEnumerator();
                            e.MoveNext();
                            _iCorner = e.Current;
                            e.Dispose();
                        }
                    }
                }
                s_pointEditor.OnRightClick = HandleRightClick;
                s_pointEditor.OnSceneGUI(ref data.poses, enabled);
                if (enabled) {
                    _editorTools.OnSceneGUI();
                }

                ElementEditor.EndMatrix(temp);
                var t = Event.current.type;

                bool edited = enabled && (t == EventType.Used || t == EventType.MouseUp || t == EventType.Used);
                if (edited) {
                    UpdateTargetVisual();
                }
                return edited;
            }

            private void HandleRightClick(int obj) {
                s_pointEditor.Selection.Clear();
                s_pointEditor.Selection.Add(obj);
                _popup = new EditorPopup((rects, popup) => {
                    var data = DataObjectT;
                    EditorGUIUtility.labelWidth = 40;
                    EditorGUI.BeginChangeCheck();
                    data.poses[obj].AutoPlay = EditorGUI.IntField(rects.NextRect(), "->", data.poses[obj].AutoPlay);
                    if (EditorGUI.EndChangeCheck()) {
                        UpdateData();
                    }
                });
                _popup.Show(new Rect(ElementEditor.visualHandler.ScreenMousePosition, Vector2.zero), new Vector2(100, 50));
            }

            bool TryGetCurrentPose(out PoseToPoseMovementComponent.Pose pose) {
                var data = DataObjectT;
                if(_iCorner < 0 || _iCorner >= data.poses.Length) {
                    pose = default;
                    return false;
                }
                pose = data.poses[_iCorner];
                return true;
            }
            void UpdateTargetVisual() {
                var cons = LogicComponent.Connections;
                if (cons == null) {
                    return;
                }
                if (TryGetCurrentPose(out PoseToPoseMovementComponent.Pose pose)) {
                    var e = GetConnections(0);
                    if (e.MoveNext()) {
                        var mat = GetMatrix();
                        var c = e.Current;
                        UnityEngine.Pose pose3D;
                        pose3D.position = mat.MultiplyPoint3x4(pose.position);
                        pose3D.rotation = mat.rotation * Quaternion.AngleAxis(pose.angle, Vector3.forward);
                        c.elemenet.SetPose(pose3D);
                        c.elemenet.UpdateData();
                    }
                }
            }
        }
    }
}
