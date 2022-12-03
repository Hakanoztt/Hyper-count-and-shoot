using System;
using System.Collections.Generic;
using Mobge.Core.Components;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core
{
	public class DecorationSet : ScriptableObject
	{
#pragma warning disable 0649
		[SerializeField] private PieceVisualizerRefMap _tileSets;
		[SerializeField] private PolygonVisualizerRefMap _polygonVisualizers;
		[SerializeField] private VisualSetMap _visualSets;
#pragma warning restore 0649

		#region Properties
#if UNITY_EDITOR
		public PieceVisualizerRefMap Tilesets { get => _tileSets; set => _tileSets = value; }
		public PolygonVisualizerRefMap PolygonVisualizers { get => _polygonVisualizers; set => _polygonVisualizers = value; }
		public VisualSetMap VisualSets { get => _visualSets; set => _visualSets = value; }
		public int TilesetCount => _tileSets.Count;

#endif
		#endregion

		#region Indexed access methods
		public VisualSet GetVisualSet(int setId) {
			return _visualSets != null && _visualSets.ContainsKey(setId) ? _visualSets[setId] : null;
		}
		public IPieceVisualizer GetPieceVisualizer(int index) {
			return _tileSets != null && _tileSets.ContainsKey(index) ? _tileSets[index].Visualizer : null;
		}
		public IPolygonVisualizer GetPolygonVisualizer(int index) {
			return _polygonVisualizers != null && _polygonVisualizers.ContainsKey(index) ? _polygonVisualizers[index].Visualizer : null;
		}
		#endregion

		#region Data structures
		[Serializable] public class PieceVisualizerRefMap : AutoIndexedMap<PieceVisualizerReference> { }
		[Serializable] public class PolygonVisualizerRefMap : AutoIndexedMap<PolygonVisualizerReference> { }
		[Serializable] public class MaterialMap : AutoIndexedMap<Material> { }
		[Serializable] public class VisualSetMap : AutoIndexedMap<VisualSet> { }

		[Serializable]
		public struct PieceVisualizerReference
		{
#if UNITY_EDITOR
			public UnityEngine.Object Reference { get => _reference; set => _reference = value; }
#endif
			[SerializeField]
			[InterfaceConstraint(typeof(IPieceVisualizer))]
			private UnityEngine.Object _reference;
			public IPieceVisualizer Visualizer => _reference as IPieceVisualizer;
			public override string ToString()
			{
				if (_reference == null) return "null";
				return _reference.ToString();
			}
		}

		[Serializable]
		public struct PolygonVisualizerReference
		{
#if UNITY_EDITOR
			public UnityEngine.Object Reference { get => _reference; set => _reference = value; }
#endif
			[SerializeField]
			[InterfaceConstraint(typeof(IPolygonVisualizer))]
			private UnityEngine.Object _reference;
			public IPolygonVisualizer Visualizer => _reference as IPolygonVisualizer;
			public override string ToString()
			{
				if (_reference == null) return "null";
				return _reference.ToString();
			}
		}
		#endregion
	}
	[Serializable]
	public class AssetReferenceDecorSet : AssetReferenceTyped<DecorationSet>
	{
	}
}
