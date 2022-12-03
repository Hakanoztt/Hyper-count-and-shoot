using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public interface IPolygonVisualizer
	{
        IPolygonRenderer Visualize(Polygon[] polygon, Vector3 offset, Quaternion rotation, bool final, Transform parent, Color color);
        void UpdateVisuals(IPolygonRenderer obj, Polygon[] polygons);
#if UNITY_EDITOR
		Texture EditorVisual();
#endif
	}
    public interface IPolygonRenderer {
        Transform Transform { get; }
        Color Color { get; set; }
    }
    
    [Serializable] 
    public class AssetReferencePolygonVisualizer : AssetReferenceTyped<IPolygonVisualizer> { }
    
}