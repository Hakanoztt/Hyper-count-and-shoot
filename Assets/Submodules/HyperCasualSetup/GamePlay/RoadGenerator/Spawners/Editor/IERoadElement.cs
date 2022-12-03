using Mobge.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Mobge.HyperCasualSetup.RoadGenerator {

    public interface IERoadElement {
        RoadElementData Data { get; set; }
    }

    public static class ERoadElementHelper {

        private static BezierPath3D.SegmentEnumerator s_segmentEnumerator;

        public static bool TryGetPiecePose(IERoadElement element, ElementEditor editor, out Pose pose) {
            return TryGetPiecePose(element, editor, out pose, out _);
        }
        public static bool TryGetPiecePose(IERoadElement element, ElementEditor editor, out Pose pose, out ERoadGeneratorComponent.Editor roadEditor) {
            if (TryGetRoad(element, editor, out roadEditor)) {
                var e = new ERoadGeneratorComponent.PoseEnumerator(roadEditor.DataObjectT);
                var data = element.Data;
                while (e.MoveNext()) {
                    if (data.pieceIndex == e.CurrentIndex) {
                        var c = e.Current;
                        pose = GetPieceOffsetPose(roadEditor, c, data.pieceIndex, data.percentage);
                        return true;
                    }
                }
            }
            pose = default;
            return false;
        }
        private static Pose GetPieceOffsetPose(ERoadGeneratorComponent.Editor roadEditor, Pose piecePose, int pieceIndex, float offset) {
            var road = roadEditor.DataObjectT;
            var item = road.items[pieceIndex];
            var pieceRes = road.prefabReferences[item.id];
            var localPose = pieceRes.SampleFromPose(road, offset, piecePose);
            var roadPose = new Pose(road.position, road.rotation);
            return localPose.GetTransformedBy(roadPose);
        }

        public static bool TryGetRoad(this IERoadElement e, ElementEditor editor, out ERoadGeneratorComponent.Editor comp) {
            if (editor.TryGetElement(e.Data.roadGenerator, out var ee)) {
                if (ee is ERoadGeneratorComponent.Editor d) {
                    comp = d;
                    return true;
                }
            }
            comp = default;
            return false;
        }

        public static Vector3 GetPosition(this IERoadElement element, ElementEditor editor) {
            var e = (AEditableElement)element;
            var p = ((BaseComponent)e.DataObject).position;
            if (TryGetPiecePose(element, editor, out var pose)) {
                return pose.position + pose.rotation * p;
            }
            return p;
        }


        public static Vector3 SetPosition(this IERoadElement element, ElementEditor editor, Vector3 position) {
            //var e = (AEditableElement)element;
            if (TryGetPiecePose(element, editor, out var pose, out var road)) {
                var worldPose = new Pose(position, Quaternion.identity);
                var pos = worldPose.GetInverseTransformedBy(pose).position;
                return pos;
            }
            else {
                return position;
            }
        }

        public static Quaternion GetRotation(this IERoadElement element, ElementEditor editor) {
            var e = (AEditableElement)element;
            Quaternion r;
            if(e.DataObject is IRotationOwner ro) {
                r = ro.Rotation;
            }
            else {
                r = Quaternion.identity;
            }
            if (TryGetPiecePose(element, editor, out var pose)) {
                return pose.rotation * r;
            }
            return r;
        }

        public static Quaternion SetRotation(this IERoadElement element, ElementEditor editor, Quaternion rotation) {
            //var e = (AEditableElement)element;
            if (TryGetPiecePose(element, editor, out var pose)) {
                return Quaternion.Inverse(pose.rotation) * rotation;
            }
            else {
                return rotation;
            }
        }

        public static void InspectorGUI(this IERoadElement element, string roadLabel, ref bool toggle, ElementEditor editor) {

            var pose = editor.GetPose((AEditableElement)element);

            var data = RoadDataField(roadLabel, ref toggle, element.Data, editor, out bool changed);
            
            element.Data = data;
            if (changed) {
                if (!element.SnapToNearestPiece(editor, pose)) {
                    editor.SetPose((AEditableElement)element, pose);
                }


            }
        }
        public static bool OnSceneGUI(this IERoadElement element, ElementEditor editor) {
            bool changed = true;
            var e = (AEditableElement)element;
            if (!TryGetRoad(element, editor, out _)) {
                var els = editor.GetElementsWithDataType<RoadGeneratorComponent.Data>();
                if (els.MoveNext()) {
                    var data = element.Data;
                    var pose = editor.GetPose(e);
                    data.roadGenerator = els.Current.Key;
                    element.Data = data;
                    SnapToNearestPiece(element, editor, pose);
                    changed = true;
                }
                els.Dispose();
            }

            if (Event.current.type != EventType.Used) {
                

                var comp = ((BaseComponent)e.DataObject);
                if(comp.position.z != 0) {
                    if (SnapToNearestPiece(element, editor, editor.GetPose(e))) {
                        comp.position.z = 0;
                        changed = true;
                    }
                }
            }
            return changed;
        }
        public static RoadElementData RoadDataField(string label, ref bool toggle, RoadElementData roadData, ElementEditor editor, out bool roadChanged) {
            toggle = EditorGUILayout.BeginFoldoutHeaderGroup(toggle, label);
            roadChanged = false;
            if (toggle) {
                EditorGUI.BeginChangeCheck();
                roadData.roadGenerator = EElementReference.LayoutField("road", roadData.roadGenerator, editor, typeof(RoadGeneratorComponent.Data));
                roadChanged = EditorGUI.EndChangeCheck();
                roadData.percentage = EditorGUILayout.Slider("percentage", roadData.percentage, 0f, 1f);
                roadData.pieceIndex = EditorGUILayout.IntField("piece index", roadData.pieceIndex);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            return roadData;
        }
        public static bool SnapToNearestPiece(this IERoadElement element, ElementEditor editor, Pose elementPose) {
            if (TryGetRoad(element, editor, out var rg)) {

                var data = element.Data;
                var en = rg.GetPoseEnumerator(false);
                float nearestDisSqr = float.PositiveInfinity;
                var e = (AEditableElement)element;
                //var elementPose = editor.GetPose(e);
                int selected = -1;
                var currentPose = en.CurrentEndPose;
                Pose pose = Pose.identity;
                while (en.MoveNext()) {
                    var middle = currentPose.position + en.CurrentEndPose.position;
                    currentPose = en.CurrentEndPose;
                    middle *= 0.5f;
                    var disSqr = (middle - elementPose.position).sqrMagnitude;
                    if (disSqr < nearestDisSqr) {
                        nearestDisSqr = disSqr;
                        selected = en.CurrentIndex;
                        pose = en.Current;
                    }
                }
                data.pieceIndex = selected;
                nearestDisSqr = float.PositiveInfinity;
                var road = rg.DataObjectT;
                float p = FindNearestPercentage(road, selected, pose, elementPose, 0f, 0.99f, 0.1f);
                data.percentage = FindNearestPercentage(road, selected, pose, elementPose,Mathf.Max(0, p-0.05f), Mathf.Min(0.99f, p + 0.0499f), 0.01f);
                element.Data = data;
                editor.SetPosition(e, elementPose.position);

                var comp = ((BaseComponent)e.DataObject);
                comp.position.z = 0;

                //((BaseComponent)e.DataObject).position = elementPose.position;
                //((IRotationOwner)e.DataObject).Rotation = Quaternion.identity;
                return true;
            }
            return false;
        }
        static float FindNearestPercentage(RoadGeneratorComponent.Data road, int selectedItem, Pose itemPose, Pose elementPose, float min, float max, float step) {
            float percentage = 0;
            var itemRef = road.prefabReferences[road.items[selectedItem].id];
            float nearestDisSqr = float.PositiveInfinity;
            for (float f = min; f < max; f += step) {
                var point = itemRef.SampleFromPose(road, f, itemPose);
                var dis = (elementPose.position - point.position).sqrMagnitude;
                if (dis < nearestDisSqr) {
                    nearestDisSqr = dis;
                    percentage = f;
                }
            }
            return percentage;
        }
    }
}