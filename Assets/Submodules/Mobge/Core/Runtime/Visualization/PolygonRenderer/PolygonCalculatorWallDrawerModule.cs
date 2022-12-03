using Mobge;
using Mobge.Core.Components;
using UnityEngine;

namespace Mobge {
    public static class PolygonCalculatorWallDrawerModule {
        public static void DrawWalls(in Polygon polygon, PolygonVisualizer visualizer, MeshBuilder meshBuilder) {
            var vertices = meshBuilder.vertices;
            var uvs = meshBuilder.uvs;
            var material = visualizer.wallMaterial;
            float uvScale;
            if (material == null) {
                uvScale = 1;
            }
            else {
                var tex = material.mainTexture;
                if (tex == null) {
                    uvScale = 1;
                }
                else {
                    uvScale = tex.height / (tex.width * polygon.height);
                }
            }
            var prev = polygon.corners[polygon.corners.Length - 1];
            const float z1 = 0;
            float z2 = polygon.height;
            float uvx = 0;
            int vi = vertices.Count + polygon.corners.Length * 2 - 2;
            for (int i = 0; i < polygon.corners.Length; i++) {
                var corner = polygon.corners[i];
                Vector3 top = corner.position;
                Vector3 bottom = corner.position;
                top.z = z1;
                bottom.z = z2;
    
                int viNext = vertices.Count;
                vertices.Add(top);
                vertices.Add(bottom);
                var dis = corner.position - prev.position;
    
                uvs.Add(new Vector2(uvx, 0));
                uvs.Add(new Vector2(uvx, 1));
    
                prev = corner;
                uvx += dis.magnitude * uvScale;
    
                meshBuilder.AddTriangle(vi, vi + 1, viNext);
                meshBuilder.AddTriangle(vi+1, viNext+1, viNext );
                vi = viNext;
            }
        }
    }
}
