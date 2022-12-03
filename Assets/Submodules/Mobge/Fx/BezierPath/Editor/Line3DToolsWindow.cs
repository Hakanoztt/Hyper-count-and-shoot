using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    public class Line3DToolsWindow : EditorWindow {

        [MenuItem("Mobge/Line 3D/Tools")]
        public static void Init() {
            GetWindow<Line3DToolsWindow>("Line 3D Tools");
        }

        EditorFoldGroups _groups;

        private void OnEnable() {
            _groups = new EditorFoldGroups(EditorFoldGroups.FilterMode.NoFilter);
        }

        private void OnGUI() {
            _groups.GuilayoutField(CreateGroups);
        }

        private void CreateGroups(EditorFoldGroups.Group obj) {
            obj.AddChild("line", () => {

            }, (roadGroups)=> {
                roadGroups.AddChild("spiral creator", () => {
                    // var line = _groups.ObjectField<Line3D>("road to edit");
                    float totalAngle = _groups.FloatField("total angle", 360f);
                    float pointPerDegree = _groups.FloatField("point per degree", 45f);
                    float totalHeight = _groups.FloatField("total height", 5f);
                    float radius = _groups.FloatField("radius", 5f);
                    bool turningRight = _groups.ToggleField("turning right", true);

                    if(GUILayout.Button("update line")) {
                        Line3D[] lines = Selection.GetFiltered<Line3D>(SelectionMode.Unfiltered);
                        if(lines == null || lines.Length != 1) {
                            EditorUtility.DisplayDialog("Warning", "Select a " + typeof(Line3D) + " object first.", "OK");
                        }
                        else {
                            var line = lines[0];
                            UpdateSpiralRoad(line, totalAngle, pointPerDegree, totalHeight, radius, turningRight);
                        }
                    }
                });
            });
        }

        void UpdateSpiralRoad(Line3D line, float totalAngle, float pointPerDegree, float totalHeight, float radius, bool turningRight) {
            if(line.path == null) {
                line.path = new BezierPath3D();
            }
            var p = new BezierPath3D.Point();

            
            line.path.Points.ClearFast();


            float yStep = pointPerDegree / totalAngle * totalHeight;
            float y = -yStep;

            int maxLoop = 1000;

            for (float deg = -pointPerDegree; deg <= totalAngle + pointPerDegree; deg += pointPerDegree, y += yStep) {
                maxLoop--;
                if (maxLoop < 0) {
                    Debug.LogError("too many points are generated");
                    break;
                }
                float rad = deg * Mathf.Deg2Rad;
                p.position.x = radius * Mathf.Sin(rad);
                p.position.z = radius * Mathf.Cos(rad);
                if (!turningRight) {
                    p.position.z = -p.position.z;
                }

                p.position.y = y;


                line.path.Points.Add(p);
            }

            line.path.UpdateControlsForAuto();
            line.path.controlMode = BezierPath3D.ControlMode.Free;

            line.path.Points.RemoveAt(0);
            line.path.Points.RemoveAt(line.path.Points.Count - 1);

            line.path.Points.Trim();


            float startY = line.path.Points.array[0].position.y;
            line.path.Points.array[0].leftControl.y = startY;
            line.path.Points.array[0].rightControl.y = startY;

            int lastIndex = line.path.Points.Count - 1;
            float endY = line.path.Points.array[lastIndex].position.y;

            line.path.Points.array[lastIndex].leftControl.y = endY;
            line.path.Points.array[lastIndex].rightControl.y = endY;

            line.ReconstructImmediate();

            EditorExtensions.SetDirty(line);
        }
    }
}