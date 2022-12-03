using Mobge.Core.Components;
using UnityEngine;

namespace Mobge {
    
    [CreateAssetMenu(menuName = "Mobge/Mobge Polygon Visualizer")]
    public class PolygonVisualizer: ScriptableObject, IPolygonVisualizer {
        public Material edgeMaterial;
        public Material fillMaterial;
        public Material wallMaterial;

        public float topEdgeOffset;
        public float leftEdgeOffset;
        public float rightEdgeOffset;
        public float bottomEdgeOffset;
        public Sprite[] topEdgeSprites;
        public Sprite[] leftEdgeSprites;
        public Sprite[] rightEdgeSprites;
        public Sprite[] bottomEdgeSprites;
        public Sprite[] topLeftCornerSprites;
        public Sprite[] topRightCornerSprites;
        public Sprite[] bottomLeftCornerSprites;
        public Sprite[] bottomRightCornerSprites;
        public Sprite[] topInnerLeftCornerSprites;
        public Sprite[] topInnerRightCornerSprites;
        public Sprite[] bottomInnerLeftCornerSprites;
        public Sprite[] bottomInnerRightCornerSprites;

        public float globalScale = 1f;
        public float edgeSpriteMinimumStretchValue = .5f;
        public float innerTextureUVAngle = 0f;
        public float innerTextureScale = 1f;
        public float edgeZOffset = -0.02f;
        public float minimumEdgeDrawLength = 0f;
        public float minimumCornerAngle = 0f;
        public float maximumCornerAngle = 180f;
        public bool calculateNormals;
        public bool calculateTangents;
        public bool joinInnerOuterAndWallMeshesIntoOneObject = false;
        
        public IPolygonRenderer Visualize(Polygon[] polygons, Vector3 offset, Quaternion rotation, bool final, Transform parent, Color color) {
            var renderer = PolygonCalculator.Instance.CreatePolygonInstance(this, parent, offset, rotation);
            PolygonCalculator.Instance.DrawPolygons(in polygons, this, renderer, color);
            return renderer;
        }
        public void UpdateVisuals(IPolygonRenderer obj, Polygon[] polygons) {
            if (!(obj is PolygonInstance renderer)) return; 
            PolygonCalculator.Instance.DrawPolygons(in polygons, this, renderer, renderer.Color);
        }
        public Texture EditorVisual() {
            return edgeMaterial.mainTexture;
        }
    }
}


