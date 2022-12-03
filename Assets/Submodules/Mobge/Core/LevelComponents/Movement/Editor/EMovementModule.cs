using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(MovementModule))]
    public class EMovementModule : EComponentDefinition
    {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as MovementModule.Data, this);
        }
        public class Editor : EditableElement<MovementModule.Data>
        {
            private const float c_sampleCount = 40;
            
            private static AnimationWindow _window;

            EditorTools _editorTools;
            bool _editMode;
            bool _isWindowOpen;
            private Pose _currentPose;
            //private int _poseReason=-1;
            EComponentDefinition _editor;
            private bool _isVisualDirty = false;

            public override bool DrawOtherObjectsGizmos => true;
            public Editor(MovementModule.Data component, EComponentDefinition editor) : base(component, editor)
            {
                if(_window==null) {
                    _window = new AnimationWindow();
                }
                _editor = editor;
                _editorTools = new EditorTools();
                _editorTools.AddTool(new EditorTools.Tool("open options") {
                    activation = new EditorTools.ActivationRule() {
                        mouseButton = 1,
                    },
                    onRelease = OpenPopup,
                });
                _editorTools.AddTool(new EditorTools.Tool("deselect"){
                    activation = new EditorTools.ActivationRule() {
                        mouseButton = 0,
                    },
                    onRelease = ReleaseExclusive
                });
            }

            private void OpenPopup()
            {
                //var pos = Event.current.mo
                //var pos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                _window.UpdatePosition();
                _isWindowOpen = true;
               // _poseReason = 2;
                _currentPose = WindowPose;
            }
            void ReleaseExclusive() {
                base.ReleaseExclusiveEdit();
                
                _editor.Repaint();
            }

            private void CurveField(string label, ref Animation.Curve curve, Color color) {
                
                var uc = curve.ToAnimationCurve();
                EditorGUI.BeginChangeCheck();
                var nuc = EditorGUILayout.CurveField(label, uc, color, new Rect());//, new Rect(0, min, 1, max-min));
                if(EditorGUI.EndChangeCheck()) {
                    curve.UpdateKeys(nuc);
                }
            }
            public override void DrawGUILayout() {

                var comp = DataObjectT;
                comp.position = EditorGUILayout.Vector3Field("position", comp.position);
                comp.Rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("rotation", comp.Rotation.eulerAngles));
                comp.EnsureData();
                _editMode = ExclusiveEditField("edit");
                var pc = comp.poseCurve;
                CurveField("x", ref pc.x, Color.red);
                CurveField("y", ref pc.y, Color.green);
                pc.hasAngle = EditorGUILayout.Toggle("has angle", pc.hasAngle);
                if(pc.hasAngle) {
                    CurveField("angle", ref pc.angle, Color.blue);
                }
                pc.duration = EditorGUILayout.FloatField("total time", pc.duration);
                comp.autoStart = EditorGUILayout.Toggle("auto start", comp.autoStart);
                comp.motorForce = EditorGUILayout.FloatField("motor force (0 = no motor)", comp.motorForce);
                comp.Mode = (MovementModule.Mode)EditorGUILayout.EnumPopup("mode", comp.Mode);
                comp.poseCurve = pc;

            }
            public override bool SceneGUI(in SceneParams @params) {
                var comp = DataObjectT;
                comp.EnsureData();
                var parent = new Pose(@params.position, @params.rotation);
                var pc = comp.poseCurve;
                if(_editMode && @params.selected) {
                    if(_isWindowOpen) {
                        AnimationWindow.Curve[] curves;
                        if (pc.hasAngle) {
                            curves = new AnimationWindow.Curve[] {
                                new AnimationWindow.Curve(pc.x.ToAnimationCurve(), "x"),
                                new AnimationWindow.Curve(pc.y.ToAnimationCurve(), "y"),
                                new AnimationWindow.Curve(pc.angle.ToAnimationCurve(), "angle"),
                            };
                        }
                        else {
                            curves = new AnimationWindow.Curve[] {
                                new AnimationWindow.Curve(pc.x.ToAnimationCurve(), "x"),
                                new AnimationWindow.Curve(pc.y.ToAnimationCurve(), "y"),
                            };
                        }
                        _window.OnGUI(curves, UpdateData, UpdateTargetVisualsFromWindow, GetCurrentValue, ref pc.duration);
                        comp.poseCurve = pc;
                        if(PoseHandle(parent) || _isVisualDirty) {
                            _isVisualDirty = false;
                            UpdateTargetVisuals();
                        }
                        DrawWindowPose(parent);
                    }
                    _editorTools.OnSceneGUI();
                }
                DrawPath(@params.matrix);

                var t = Event.current.type;
                return t == EventType.Used || t == EventType.MouseUp;
            }
            
            public void DrawPath(in Matrix4x4 matrix) {
                var m = Handles.matrix;
                Handles.matrix = matrix;
                float time = 0;
                var pc = DataObjectT.poseCurve;
                float timeStep = pc.duration / c_sampleCount;
                var p = pc.EvaluatePosition(0);
                Handles.color = Color.gray;
                for(int i = 0; i < c_sampleCount; i++) {
                    time += timeStep;
                    var np = pc.EvaluatePosition(time);
                    Handles.DrawLine(p, np);
                    p = np;
                }
                Handles.matrix = m;
            }
            private float GetCurrentValue(string label) {
                switch(label) {
                    case "x":
                    default:
                    return _currentPose.position.x;
                    case "y":
                    return _currentPose.position.y;
                    case "angle":
                        return MovementModule.ConvertToAngle(_currentPose.rotation);
                    //_currentPose.rotation.ToAngleAxis(out float angle, out Vector3 axis);
                    //return angle;
                }
            }
            private void DrawWindowPose(Pose parent) {
                var p = WindowPose.GetTransformedBy(parent);
                float size = HandleUtility.GetHandleSize(p.position) * 0.15f;
                Handles.SphereHandleCap(0, p.position, p.rotation, size, Event.current.type);
            }
            private bool PoseHandle(Pose parent) {
                Pose p;
                var hasAngle = DataObjectT.poseCurve.hasAngle;
                var old = _currentPose.GetTransformedBy(parent);
                if (hasAngle) {
                    p = HandlesExtensions.PoseHandle(old);
                }
                else {
                    p.position = Handles.PositionHandle(old.position, Quaternion.identity);
                    p.rotation = old.rotation;
                }
                bool b = p != old;
                parent.rotation = Quaternion.Inverse(parent.rotation);
                parent.position = parent.rotation * -parent.position;
                //_poseReason = 1;
                _currentPose = p.GetTransformedBy(parent);
                return b;
            }
            private Pose WindowPose {
                get {
                    float time = _window.Time;
                    var pc = DataObjectT.poseCurve;
                    Pose p;
                    p.position = pc.EvaluatePosition(time);
                    p.rotation = Quaternion.AngleAxis(pc.EvaluateAngle(time), Vector3.forward);
                    return p;
                }
            }
            private void UpdateTargetVisualsFromWindow() {
               // _poseReason = 3;
                 _currentPose= WindowPose;
                //UpdateTargetVisuals();
                _isVisualDirty = true;
                Editor.Repaint();
            }
            private void UpdateTargetVisuals() {
                var cons = LogicComponent.Connections;
                if(cons == null) {
                    return;
                }
                //var ee = cons.GetConnections(0);
                var e = GetConnections(0);
                if(e.MoveNext()) {
                    var mat = GetMatrix();
                    //mat = ElementEditor.Matrix * mat;
                    var c = e.Current;
                    //Debug.Log("reason: " + _poseReason + " " + _currentPose.position.x);
                    Pose p = _currentPose;
                    p.position = mat.MultiplyPoint3x4(p.position);
                    if(DataObjectT.poseCurve.hasAngle) {
                       // p.rotation = mat.rotation * p.rotation;
                        c.elemenet.SetPose(p);
                    }
                    else {
                        c.elemenet.SetPosition(p.position);
                    }
                    c.elemenet.UpdateData();
                }
            }
            private void UpdateData(AnimationWindow.Curve[] curves, float time) {
                var pc = DataObjectT.poseCurve;
                pc.x.UpdateKeys(curves[0].curve);
                pc.y.UpdateKeys(curves[1].curve);
                if(pc.hasAngle) {
                    pc.angle.UpdateKeys(curves[2].curve);
                }
                pc.duration = time;
                pc.Ensure(true);
                DataObjectT.poseCurve = pc;
                UpdateData();
            }
        }
    }
}