using Mobge;
using Mobge.Core.Components;
using UnityEngine;

namespace Mobge {
    public static class PolygonCalculatorInnerDrawerModule {
        public static void DrawInnerPolygon(in Polygon polygon, PolygonVisualizer visualizer, MeshBuilder meshBuilder) {
            var cos = Mathf.Cos(visualizer.innerTextureUVAngle * Mathf.Deg2Rad);
            var sin = Mathf.Sin(visualizer.innerTextureUVAngle * Mathf.Deg2Rad);
            int mainTextureWidth = 1;
            int mainTextureHeight = 1;
            if (visualizer.fillMaterial && visualizer.fillMaterial.mainTexture) {
                mainTextureWidth = visualizer.fillMaterial.mainTexture.width;
                mainTextureHeight = visualizer.fillMaterial.mainTexture.height;
            }
            var t = Triangulator.Instance;
            int baseVertex = meshBuilder.vertices.Count;
            for (int i = 0; i < polygon.corners.Length; i++) {
                Vector3 pos = polygon.corners[i].position;
                t.Points.Add(pos);
                meshBuilder.vertices.Add(pos);
                MapInnerPolygonUV(meshBuilder, pos, cos, sin, mainTextureWidth, mainTextureHeight,
                    visualizer.innerTextureScale, visualizer.globalScale);
            }
            var triangles = t.Triangulate();
            for (int i = 0; i < triangles.Count; i++) {
                meshBuilder.triangles.Add(triangles.array[i] + baseVertex);
            }
        }
        private static void MapInnerPolygonUV(MeshBuilder meshBuilder, Vector2 pos, float cos, float sin, int mainTextureWidth, int mainTextureHeight, float innerTextureScale, float globalScale) {
            var v = new Vector2(pos.x * cos - pos.y * sin, pos.x * sin + pos.y * cos);
            v /= innerTextureScale;
            if (globalScale >= .0001f) v /= globalScale;
            v.x /= mainTextureWidth * 0.01f;
            v.y /= mainTextureHeight * 0.01f;
            meshBuilder.uvs.Add(v);
        }
    }
}
