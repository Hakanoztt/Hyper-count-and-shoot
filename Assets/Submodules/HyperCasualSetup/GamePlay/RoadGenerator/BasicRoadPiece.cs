using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    public class BasicRoadPiece : MonoBehaviour, RoadGeneratorComponent.IRoadPiece {

        public Pose[] endPoints;
        [Range(.01f, 0.99f)]public float roadCubicness = .3f;

        int RoadGeneratorComponent.IRoadPiece.EndPointCount => endPoints == null ? 0 : endPoints.Length;

        Pose RoadGeneratorComponent.IRoadPiece.GetLocalEndPoint(int index) {
            return endPoints[index];
        }

        public void UpdateBezier(BezierPath3D path, int endpoint1, int endpoint2) {
            var pos = new Pose(transform.position, transform.rotation);
            RoadGeneratorComponent.UpdateBezier(path, endPoints[endpoint1].GetTransformedBy(pos), endPoints[endpoint2].GetTransformedBy(pos), roadCubicness);
        }
        Pose RoadGeneratorComponent.IRoadPiece.SampleFromPose(float percentage, Pose pose, int endpoint1, int endpoint2) {
            return RoadGeneratorComponent.SampleBezier(endPoints[endpoint1].GetTransformedBy(pose), endPoints[endpoint2].GetTransformedBy(pose), roadCubicness, percentage);
        }

        protected void OnDrawGizmosSelected() {
            if (endPoints != null) {
                Gizmos.matrix = transform.localToWorldMatrix;
                for(int i =    0; i < endPoints.Length; i++) {
                    var ep = endPoints[i];
                    var sideOffset = ep.rotation * Vector3.right * 0.15f;
                    Gizmos.DrawLine(ep.position + sideOffset, ep.position - sideOffset);
                    Gizmos.DrawLine(ep.position, ep.position + ep.rotation * Vector3.forward * 0.5f);
                }
                Gizmos.matrix = Matrix4x4.identity;
            }
        }

    }
}