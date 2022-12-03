using Mobge.Core.Components;
using UnityEngine;
using static Mobge.LineRendererPlus;

namespace Mobge {
    public class PolygonCalculator {
        
        private static PolygonCalculator _instance;
        public static PolygonCalculator Instance => _instance ?? (_instance = new PolygonCalculator());

        public PolygonCalculator() {
            _polygonCalculatorEdgeCornerModule = PolygonCalculatorEdgeCornerModule.New();
            _meshBuilder = new MeshBuilder();
        }

        private readonly MeshBuilder _meshBuilder;
        private PolygonCalculatorEdgeCornerModule _polygonCalculatorEdgeCornerModule;
        
        public PolygonInstance CreatePolygonInstance(PolygonVisualizer visualizer, Transform parent, Vector3 offset, Quaternion rotation, Transform existingTransform = null) {
            var renderer = new PolygonInstance();
            GameObject go = null;
            if (existingTransform != null) {
                go = existingTransform.gameObject;
            }
            else {
                go = new GameObject(nameof(PolygonInstance));
                var rendererTransform = go.transform;
                rendererTransform.SetParent(parent, false);
                rendererTransform.localPosition = offset;
                rendererTransform.localRotation = rotation;
            }
            renderer.Transform = go.transform;
            EnsureRendererArray(visualizer, renderer);
            return renderer;
        }
        private static int GetSectionCount(PolygonVisualizer visualizer) {
            if (visualizer.joinInnerOuterAndWallMeshesIntoOneObject) return 1;
            int i = 0;
            if (visualizer.edgeMaterial != null) i++;
            if (visualizer.wallMaterial != null) i++;
            if (visualizer.fillMaterial != null) i++;
            return i;
        }
        private static int GetMaterialCount(PolygonVisualizer visualizer) {
            int i = 0;
            if (visualizer.edgeMaterial != null) i++;
            if (visualizer.wallMaterial != null) i++;
            if (visualizer.fillMaterial != null) i++;
            return i;
        }
        public void DrawPolygons(in Polygon[] polygons, PolygonVisualizer visualizer, PolygonInstance polygonInstance, Color color) {
            if (polygons == null || polygons.Length <= 0 || polygons[0].corners == null || polygons[0].corners.Length <= 1) return;
            EnsureRendererArray(visualizer, polygonInstance);
            SetupMaterialsOfMeshRendererArray(visualizer, polygonInstance);

            if (visualizer.joinInnerOuterAndWallMeshesIntoOneObject) {
                _meshBuilder.Clear();
                for (int i = 0; i < polygons.Length; i++) {
                    int submeshCount = -1;
                    if (visualizer.fillMaterial != null) {
                        submeshCount++;
                        _meshBuilder.MoveToSpecificSubMesh(submeshCount);
                        PolygonCalculatorInnerDrawerModule.DrawInnerPolygon(in polygons[i], visualizer, _meshBuilder);
                    }
                    if (visualizer.wallMaterial != null) {
                        submeshCount++;
                        _meshBuilder.MoveToSpecificSubMesh(submeshCount);
                        PolygonCalculatorWallDrawerModule.DrawWalls(in polygons[i], visualizer, _meshBuilder);
                    }
                    if (visualizer.edgeMaterial != null) {
                        submeshCount++;
                        _meshBuilder.MoveToSpecificSubMesh(submeshCount);
                        _polygonCalculatorEdgeCornerModule.DrawEdgesAndCorners(in polygons[i], visualizer, _meshBuilder);
                    }
                }
                var mesh = polygonInstance.meshFilters[0].sharedMesh;
                AddColorData(_meshBuilder, color);
                _meshBuilder.BuildMesh(mesh);
                if (visualizer.calculateNormals) {
                    mesh.RecalculateNormals();
                }
                if (visualizer.calculateTangents) {
                    mesh.RecalculateTangents();
                }
            }
            else {
                int rendererIndex = 0;
                if (visualizer.fillMaterial) {
                    _meshBuilder.Clear();
                    for (int i = 0; i < polygons.Length; i++) {
                        PolygonCalculatorInnerDrawerModule.DrawInnerPolygon(in polygons[i], visualizer, _meshBuilder);
                    }
                    var mesh = polygonInstance.meshFilters[rendererIndex++].sharedMesh;
                    AddColorData(_meshBuilder, color);
                    _meshBuilder.BuildMesh(mesh);
                    if (visualizer.calculateNormals) {
                        mesh.RecalculateNormals();
                    }
                    if (visualizer.calculateTangents) {
                        mesh.RecalculateTangents();
                    }
                }
                if (visualizer.wallMaterial) {
                    _meshBuilder.Clear();
                    for (int i = 0; i < polygons.Length; i++) {
                        PolygonCalculatorWallDrawerModule.DrawWalls(in polygons[i], visualizer, _meshBuilder);
                    }
                    var mesh = polygonInstance.meshFilters[rendererIndex++].sharedMesh;
                    AddColorData(_meshBuilder, color);
                    _meshBuilder.BuildMesh(mesh);
                    if (visualizer.calculateNormals) {
                        mesh.RecalculateNormals();
                    }
                    if (visualizer.calculateTangents) {
                        mesh.RecalculateTangents();
                    }
                }
                if (visualizer.edgeMaterial) {
                    _meshBuilder.Clear();
                    for (int i = 0; i < polygons.Length; i++) {
                        _polygonCalculatorEdgeCornerModule.DrawEdgesAndCorners(in polygons[i], visualizer, _meshBuilder);
                    }
                    if (rendererIndex != 0) {
                        polygonInstance.meshFilters[rendererIndex].transform.localPosition = Vector3.forward * visualizer.edgeZOffset;
                    }
                    var mesh = polygonInstance.meshFilters[rendererIndex++].sharedMesh;
                    AddColorData(_meshBuilder, color);
                    _meshBuilder.BuildMesh(mesh);
                    if (visualizer.calculateNormals) {
                        mesh.RecalculateNormals();
                    }
                    if (visualizer.calculateTangents) {
                        mesh.RecalculateTangents();
                    }
                }
            }
        }
        private static void EnsureRendererArray(PolygonVisualizer visualizer, PolygonInstance polygonInstance) {
            int sectionCount = GetSectionCount(visualizer);
            if (polygonInstance.meshRenderers != null && polygonInstance.meshRenderers.Length == sectionCount) return;
            Cleanup(polygonInstance);
            if (sectionCount >= 1) {
                polygonInstance.meshRenderers = new MeshRenderer[sectionCount];
                polygonInstance.meshFilters = new MeshFilter[sectionCount];
                polygonInstance.meshRenderers[0] = polygonInstance.Transform.gameObject.GetOrAddComponent<MeshRenderer>();
                polygonInstance.meshFilters[0] = polygonInstance.Transform.gameObject.GetOrAddComponent<MeshFilter>();
                for (int i = 1; i < sectionCount; i++) {
                    Transform extraRenderer;
                    if (polygonInstance.Transform.gameObject.transform.childCount >= i) {
                        extraRenderer = polygonInstance.Transform.GetChild(i - 1);
                    } else {
                        extraRenderer = new GameObject("extra renderer").transform;
                        extraRenderer.SetParent(polygonInstance.Transform);
                    }
                    extraRenderer.localPosition = Vector3.zero;
                    extraRenderer.localRotation = Quaternion.identity;
                    polygonInstance.meshRenderers[i] = extraRenderer.gameObject.GetOrAddComponent<MeshRenderer>();
                    polygonInstance.meshFilters[i] = extraRenderer.gameObject.GetOrAddComponent<MeshFilter>();
                }
            }
            for (int i = 0; i < polygonInstance.meshRenderers.Length; i++) {
                if (polygonInstance.meshFilters[i].sharedMesh == null) {
                    polygonInstance.meshFilters[i].sharedMesh = new Mesh();
                }
            }
        }
        private static void Cleanup(PolygonInstance polygonInstance) {
            if (polygonInstance.meshRenderers != null) {
                for (int i = 1; i < polygonInstance.meshRenderers.Length; i++) {
                    polygonInstance.meshRenderers[i].gameObject.DestroySelf();
                }
                polygonInstance.meshRenderers[0].DestroySelf();
                polygonInstance.meshFilters[0].DestroySelf();
                polygonInstance.meshRenderers = null;
                polygonInstance.meshFilters = null;
            }
            var childCount = polygonInstance.Transform.childCount;
            for (int i = 0; i < childCount; i++) {
                polygonInstance.Transform.GetChild(0).gameObject.DestroySelf();
            }
        }
        private void SetupMaterialsOfMeshRendererArray(PolygonVisualizer visualizer, PolygonInstance renderer) {
            if (visualizer.joinInnerOuterAndWallMeshesIntoOneObject) {
                var materialArray = new Material[GetMaterialCount(visualizer)];
                int i = 0;
                if (visualizer.fillMaterial != null) {
                    materialArray[i++] = visualizer.fillMaterial;
                }
                if (visualizer.wallMaterial != null) {
                    materialArray[i++] = visualizer.wallMaterial;
                }
                if (visualizer.edgeMaterial != null) {
                    materialArray[i++] = visualizer.edgeMaterial;
                }
                renderer.meshRenderers[0].sharedMaterials = materialArray;
            }
            else {
                int i = 0;
                if (visualizer.fillMaterial != null) {
                    renderer.meshRenderers[i++].sharedMaterial = visualizer.fillMaterial;
                }
                if (visualizer.wallMaterial != null) {
                    renderer.meshRenderers[i++].sharedMaterial = visualizer.wallMaterial;
                }
                if (visualizer.edgeMaterial != null) {
                    renderer.meshRenderers[i++].sharedMaterial = visualizer.edgeMaterial;
                }
            }
        }
        
        private void AddColorData(MeshBuilder meshBuilder, Color color) {
            var c = meshBuilder.vertices.Count;
            meshBuilder.colors.Clear();
            for (int i = 0; i < c; i++) {
                meshBuilder.colors.Add(color);
            }
        }
    }
}
    
