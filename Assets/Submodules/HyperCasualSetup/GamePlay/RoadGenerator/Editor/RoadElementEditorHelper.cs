using UnityEditor;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {

    public class RoadElementEditorHelper {

        public static Vector3 ToLocal(Pose parent, Vector3 pos) {
            return Quaternion.Inverse(parent.rotation) * (pos - parent.position);
        }

        public static Vector3 ToWorld(Pose parent, Vector3 pos) {
            return parent.position + parent.rotation * pos;
        }

        public static Quaternion ToLocal(Pose parent, Quaternion rot) {
            return Quaternion.Inverse(parent.rotation) * rot;
        }

        public static Quaternion ToWorld(Pose parent, Quaternion rot) {
            return parent.rotation * rot;
        }

        public static Pose GetParentPose(ElementEditor editor, int parentIndex, ElementReference roadGenerator) {
            if (TryGetRoadGenerator(editor, roadGenerator, out var road)) {
                if (road.IsValid()) {
                    road.InitReferences();
                    if (parentIndex >= 0 && parentIndex < road.items.Length) {
                        //var pose = new Pose(road.position, road.rotation);
                        var e = road.NewItemEnumerator();
                        int index = 0;
                        while (index <= parentIndex) {
                            e.MoveNext();
                            index++;
                        }
                        return e.LastItemPose;
                    }

                }
            }
            return Pose.identity;
        }

        public static void SnapToClosestPiece(ElementEditor editor, BaseRoadElementComponent.Data data, float percent) {
            bool roadExists = TryGetRoadGenerator(editor, data.roadGenerator, out RoadGeneratorComponent.Data road);
            if (!roadExists) {
                return;
            }

            int nearestIndex = FindNearestRoadPiece(editor, road, data, out _);
            if (nearestIndex >= 0 && nearestIndex < road.items.Length) {
                data.parentIndex = nearestIndex;
                data.position = FindPositionOnBezier(nearestIndex, road, percent);
                data.rotation = Quaternion.identity;
            }
        }

        public static void SnapToParent(ElementEditor editor, BaseRoadElementComponent.Data data) {
            if (TryGetRoadGenerator(editor, data.roadGenerator, out _)) {
                data.position = Vector3.zero;
                data.rotation = Quaternion.identity; // position/rotation relative to parent set to 0 i.e. snapped to parent
            }
        }

        public static bool TryGetRoadGenerator(ElementEditor editor, ElementReference roadGenerator, out RoadGeneratorComponent.Data road) {
            if (editor.TryGetElement(roadGenerator, out var element)) {
                if (element.DataObject is RoadGeneratorComponent.Data r) {
                    if (r.IsValid() && r.InitReferences()) {
                        road = r;
                        return true;
                    }
                }
            }
            road = default;
            return false;
        }

        public static void ParentField(ElementEditor editor, BaseRoadElementComponent.Data data, ref Vector3 position, ref Quaternion rotation) {
            var pos = position;
            var rot = rotation;

            EditorGUI.BeginChangeCheck();
            var roads = EElementReference.ElementsWithId(editor, typeof(RoadGeneratorComponent.Data));
            data.roadGenerator = EditorLayoutDrawer.Popup("Road generator", roads, data.roadGenerator, "None");
            if (TryGetRoadGenerator(editor, data.roadGenerator, out var road)) {
                data.parentIndex = EditorGUILayout.IntField("Parent item", data.parentIndex);
                data.parentIndex = Mathf.Clamp(data.parentIndex, 0, road.items.Length - 1);
            }
            if (EditorGUI.EndChangeCheck()) {
                position = pos;
                rotation = rot;
            }
        }

        public static int FindNearestRoadPiece(ElementEditor editor, RoadGeneratorComponent.Data road, BaseRoadElementComponent.Data data, out Pose piecePose) {
            int nearestIndex = -1;
            piecePose = Pose.identity;

            if (road != null) {
                var it = road.NewItemEnumerator();
                float minDistanceSqr = float.PositiveInfinity;

                Vector3 worldPos = ToWorld(GetParentPose(editor, data.parentIndex, data.roadGenerator), data.position);
                while (it.MoveNext()) {
                    var distance = worldPos - it.LastItemPose.position;
                    var disSqr = distance.sqrMagnitude;
                    if (disSqr < minDistanceSqr) {
                        minDistanceSqr = disSqr;
                        nearestIndex = it.CurrentIndex;
                        piecePose = it.LastItemPose;
                    }
                }
            }

            return nearestIndex;
        }

        public static Vector3 FindPositionOnBezier(int pieceIndex, RoadGeneratorComponent.Data road, float percent) {
            ref RoadGeneratorComponent.ComponentRef @ref = ref road.prefabReferences[road.items[pieceIndex].id];
            BezierPath3D b = new BezierPath3D();
            RoadGeneratorComponent.UpdateBezier(b, @ref.StartPose, @ref.EndPose, road.defaultCubicness);

            var itr = b.GetEnumerator(0f);
            itr.MoveForward(0f);
            itr.MoveForwardByPercent(percent);
            return itr.CurrentPoint;
        }
    }
}