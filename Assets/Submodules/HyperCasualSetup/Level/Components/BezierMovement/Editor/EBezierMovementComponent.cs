using System;
using Mobge.Animation;
using Mobge.Core;
using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEngine;

namespace Mobge.HyperCasualSetup {
    [CustomEditor(typeof(BezierMovementComponent))]
    public class EBezierMovementComponent : EComponentDefinition 
    {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as BezierMovementComponent.Data, this);
        }
        public class Editor : EditableElement<BezierMovementComponent.Data> {
            
            private bool _editMode;
            private readonly EBezierPath _pathEditor = new EBezierPath();
            private bool _reCalculateTime = true;
            private float _approximateTime;

            public Editor(BezierMovementComponent.Data component, EComponentDefinition editor) : base(component, editor) 
            {
                EnsureObject();
            }
            
            private void EnsureObject() {
                if (DataObjectT.path == null || DataObjectT.path.Points == null) 
                {
                    DataObjectT.path = BezierMovementComponent.Data.DefaultBezierPath;
                }
            }

            public override void DrawGUILayout() {
                EnsureObject();
                var data = DataObjectT;
                
                EditorGUILayout.HelpBox("The approximate time is for one loop is : " + _approximateTime, MessageType.Info);

                if(data.path.closed) {
                    EditorGUILayout.HelpBox(nameof(data.path.closed) + " and " + 
                    nameof(BezierMovementComponent.Data.Mode.ContinueFromEnd) + " are conflicting two attributes, this is why " + 
                    nameof(BezierMovementComponent.Data.Mode.ContinueFromEnd) + " is not available when " + 
                    nameof(data.path.closed) + " attribute is on!", MessageType.Warning);
                }
                
                base.DrawGUILayout();
                
                data.Rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("rotation", data.Rotation.eulerAngles));

                var value = Mathf.Clamp(EditorGUILayout.FloatField("Max Speed", data.maxSpeed), 0, float.MaxValue);
                _reCalculateTime = _reCalculateTime || data.maxSpeed != value;
                data.maxSpeed = value;
                
                var guiContent = new GUIContent("Damping", "Smoothness on speed change.");
                value = Mathf.Clamp(EditorGUILayout.FloatField(guiContent, data.damping), 0, float.PositiveInfinity);
                _reCalculateTime = _reCalculateTime || data.damping != value;
                data.damping = value;

                data.autoStart = EditorGUILayout.Toggle("Auto Start", data.autoStart);
                data.applyRotation = EditorGUILayout.Toggle("Apply Rotation", data.applyRotation);

                if (data.applyRotation)
                {
                    EditorGUILayout.HelpBox("Align axis offset starts from right and moves clock wise. Right(0) Bottom(90) Left(180) Top(270)", MessageType.Info);
                    data.forwardAngleOffset = EditorGUILayout.FloatField("Forward Align Axis Offset", data.forwardAngleOffset);
                    data.backwardAngleOffset = EditorGUILayout.FloatField("Backward Align Axis Offset", data.backwardAngleOffset);
                }
                
                data.enableDebugButtons = EditorGUILayout.Toggle("Enable Debug Buttons", data.enableDebugButtons);
                data.mode = (BezierMovementComponent.Data.Mode)EditorGUILayout.EnumPopup(new GUIContent("Path Mode"), data.mode, CheckEnabled, false, new GUIStyle(GUI.skin.GetStyle("popup")), null);
               
                var rememberClosed = data.path.closed;
                _pathEditor.OnInspectorGUI(DataObjectT.path);
                if (data.path.closed != rememberClosed)
                {
                    _reCalculateTime = true;
                    if (data.path.closed && data.mode == BezierMovementComponent.Data.Mode.ContinueFromEnd)
                    {
                        data.mode = BezierMovementComponent.Data.Mode.Normal;
                    }
                }
                
                if (_reCalculateTime)
                {
                    _approximateTime = CalculateApproximateTime();
                    _reCalculateTime = false;
                }
                
                _editMode = ExclusiveEditField("edit on scene");
            }
            
            public override bool SceneGUI(in SceneParams sceneParams) 
            {
                EnsureObject();
                    
                bool enabled = @sceneParams.selected;
                bool edited = false;
                
                var defaultMat = Handles.matrix;
                var parentMat = defaultMat * @sceneParams.matrix;
                Handles.matrix = parentMat;
                    
                if (enabled && _editMode)
                {
                    edited = _pathEditor.OnSceneGUI(DataObjectT.path);
                    
                    int index = _pathEditor.SingleSelectedIndex;
                    if (index >= 0)
                    {
                        Handles.matrix = defaultMat;
                        UpdateTargetVisuals(index, new Pose(sceneParams.position, sceneParams.rotation));
                        Handles.matrix = parentMat;
                    }
                }
                else if((Event.current.type == EventType.Repaint))
                {
                    _pathEditor.DrawBezierLine(DataObjectT.path);
                }
                
                Handles.matrix = defaultMat;
                
                return enabled && edited;
            }

            private float CalculateApproximateTime()
            {
                var data = DataObjectT;
                if (data.maxSpeed <= 0) return 0;
                var totalLength = data.path.GetEnumerator(0).CalculateTotalLength();
                float speed = 0;
                float targetSpeed = data.maxSpeed;
                float approximateTime = 0;
                var limit = 5000;

                while (totalLength > 0) {
                    limit--;
                    if (limit < 0)
                    {
                        Debug.Log("Loop limit reached while calculating approximate time");
                        return approximateTime;
                    }
                    totalLength -= speed * Time.fixedDeltaTime;
                    if (speed < targetSpeed) {
                        speed += data.damping * Time.fixedDeltaTime;
                        if (speed > targetSpeed) speed = targetSpeed;
                    }
                    approximateTime += Time.fixedDeltaTime;
                }

                return approximateTime;
            }

            private bool CheckEnabled(Enum arg) {
                //Assuming ContinueFromEnd = 2
                if (arg.Equals(BezierMovementComponent.Data.Mode.ContinueFromEnd))
                {
                    return !DataObjectT.path.closed;
                }
                return true;
            }
            
            private void UpdateTargetVisuals(int pointIndex, Pose parent) 
            {
                var cons = LogicComponent.Connections;
                if(cons == null) 
                {
                    return;
                }
                
                var segmentEnumerator = DataObjectT.path.GetEnumerator(0, pointIndex, 0);
                var currentPoint = segmentEnumerator.CurrentPoint;
                
                Quaternion rotation = Quaternion.identity;
                if (DataObjectT.applyRotation)
                {
                    Vector2 diff;
                    if (pointIndex == 0)
                    {
                        segmentEnumerator.MoveForward(DataObjectT.maxSpeed * 0.03f);
                        diff = segmentEnumerator.CurrentPoint - currentPoint;
                    }
                    else
                    {
                        segmentEnumerator.MoveBackward(DataObjectT.maxSpeed * 0.03f);
                        diff = currentPoint - segmentEnumerator.CurrentPoint;
                    }
                    
                    float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg + DataObjectT.forwardAngleOffset;
                    rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                }
                
                Pose pose = new Pose(currentPoint, rotation); 
                pose = pose.GetTransformedBy(parent);
                
                var e = GetConnections(0);
                while(e.MoveNext()) 
                {
                    var c = e.Current;
                    
                    if(DataObjectT.applyRotation) 
                    {
                        c.elemenet.SetPose(pose);
                    }
                    else 
                    {
                        c.elemenet.SetPosition(pose.position);
                    }
                    c.elemenet.UpdateData();
                }
            }
        }
    }
}
