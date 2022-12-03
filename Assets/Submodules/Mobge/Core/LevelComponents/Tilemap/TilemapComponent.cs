using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Mobge.Core.Piece;

namespace Mobge.Core.Components
{
	public class TilemapComponent : ComponentDefinition<TilemapComponent.Data>
	{
		public TilemapComponent()
		{
		}
		public class RendererList
		{
			private Transform _transform;
			public RendererList(Transform transform)
			{
				_transform = transform;
				Data = new Dictionary<int, IPieceRenderer>();
			}
			public Transform Transform => _transform;
			public Dictionary<int, IPieceRenderer> Data { get; set; }
		}
		[Serializable]
		public class Data : BaseComponent
		{
			private static GridInfo s_tempGridinfo = new GridInfo();
			[SerializeField]
			public Piece.Atom[] atoms;
			public override void Start(in InitArgs initData)
			{
				CreateRenderer(initData.player.level, initData.player.DecorationRoot, position, false);
				GenerateColliders(initData.player.PhysicsRoot, position);
			}
			public void UpdateRenderer(Level level, RendererList renderers, Vector3 position)
			{
				s_tempGridinfo.Init(atoms);
				// Sanity check
				foreach (int i in s_tempGridinfo.GetExistingIndexes())
					if (!renderers.Data.ContainsKey(i)) {
						IPieceRenderer ipr;
						var ipv = level.decorationSet.LoadedAsset.GetPieceVisualizer(i);
						
						ipr = ipv.Visualize(s_tempGridinfo, position, i, false, renderers.Transform);
						renderers.Data.Add(i, ipr);
					}


				foreach (IPieceRenderer rens in renderers.Data.Values)
					rens.Transform.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>().ClearAllTiles();

				foreach (KeyValuePair<int, IPieceRenderer> kvpair in renderers.Data)
					level.decorationSet.LoadedAsset.GetPieceVisualizer(kvpair.Key).UpdateVisuals(kvpair.Value, s_tempGridinfo, kvpair.Key);
				s_tempGridinfo.Clear();
			}

			public RendererList CreateRenderer(Level level, Transform parent, Vector3 offset, bool final)
			{
				var rl = new RendererList(new GameObject("renderer").transform);
				rl.Transform.SetParent(parent, false);
				s_tempGridinfo.Init(atoms);
				foreach (int i in s_tempGridinfo.GetExistingIndexes()) {
					IPieceRenderer ipr;
					var ipv = level.decorationSet.LoadedAsset.GetPieceVisualizer(i);
					ipr = ipv.Visualize(s_tempGridinfo, offset, i, true, rl.Transform);
					rl.Data.Add(i, ipr);
				}
				return rl;
			}
			private void GenerateColliders(Transform root, Vector3 offset)
			{
				if (!root) return;
				var _offset = new Vector2(-0.5f + position.x + offset.x, -0.5f + position.y + offset.y);
				for (int i = 0; i < atoms.Length; i++) {
					var col = root.gameObject.AddComponent<BoxCollider2D>();
					col.offset = atoms[i].rectangle.center + _offset;
					col.size = atoms[i].rectangle.size;
				}
			}

			/// <summary>
			/// Gets the self rect.
			/// </summary>
			/// <value>The self rect.</value>
			/// <remarks> Does not consider offset, gives raw rectangle. </remarks>
			public Rect SelfRect {
				get {
					float xMax = float.NegativeInfinity;
					float xMin = float.PositiveInfinity;
					float yMax = float.NegativeInfinity;
					float yMin = float.PositiveInfinity;
					if (atoms.Length > 0) {
						for (int i = 0; i < atoms.Length; i++) {
							if (xMax < atoms[i].rectangle.xMax)
								xMax = Mathf.RoundToInt(atoms[i].rectangle.xMax);
							if (xMin > atoms[i].rectangle.xMin)
								xMin = Mathf.RoundToInt(atoms[i].rectangle.xMin);
							if (yMax < atoms[i].rectangle.yMax)
								yMax = Mathf.RoundToInt(atoms[i].rectangle.yMax);
							if (yMin > atoms[i].rectangle.yMin)
								yMin = Mathf.RoundToInt(atoms[i].rectangle.yMin);
						}
						return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
					}
					return new Rect();
				}
			}
		}
		//[Serializable]
		//public struct Atom {
		//    public RectInt rectangle;
		//    public int decorationID;
		//    public override string ToString()
		//    {
		//        return "Rectangle: " + rectangle + " DecorID: " + decorationID;
		//    }
		//}
	}
}
