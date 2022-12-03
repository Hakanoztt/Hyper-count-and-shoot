using System;
using System.Collections;
using System.Collections.Generic;
using Mobge.Serialization;
using UnityEngine;

namespace Mobge.Core.Components {
    public class PolygonComponent : BasePlatformComponent<PolygonComponent.Data>
    {
	    [Serializable]
		public new class Data : BasePlatformComponent<PolygonComponent.Data>.Data
		{
			public Data() {
				subdivisionCount = 1;
			}
			public static Polygon[] DefaultPolygon => new Polygon[] {
				new Polygon(
					new Corner[] {
						new Corner(new Vector2(0,1)),
						new Corner(new Vector2(-1,-1)),
						new Corner(new Vector2(1,-1)),
					},
					1f, 
					false, 
					false, 
					false) 
			};
#if UNITY_EDITOR
			public Polygon[] polygons = DefaultPolygon;
#else
            public Polygon[] polygons;
#endif
            public float cubicness = 0.3f;
            
            public override Polygon[] GetPolygons() => _polygonRasterizer.GetPolygons(this, Application.isEditor);
            
			private static PolygonRasterizer _polygonRasterizer = new PolygonRasterizer();


			private struct PolygonRasterizer {
				private Polygon[] _polygons;
				private PolygonComponent.Data _owner;
				public Polygon[] GetPolygons(PolygonComponent.Data data, bool dontCache = false) {
					if (_owner == data && !dontCache) {
						return _polygons;
					}
					_owner = data;
					var polygons = PolygonUtilities.GetSubdividedPolygon(data.polygons, data.subdivisionCount, data.cubicness);
					_polygons = polygons;
					return polygons;
				}
			}
		}


	}
}