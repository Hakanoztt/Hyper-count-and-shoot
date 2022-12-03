using System;
using System.Collections.Generic;
using Mobge.Core.Components;
using UnityEngine;
using static Mobge.Core.DecorationSet;

namespace Mobge.Core
{
	[Serializable]
	[CreateAssetMenu(menuName = "Mobge/VisualSet")]
	public class VisualSet : ScriptableObject
	{
		[SerializeField] public VisualMap visuals;
		[SerializeField] public DecorationSet.MaterialMap materials;

		public Material GetMaterial(int index)
		{
			if (materials == null || !materials.ContainsKey(index)) {
				return null;
			}
			return materials[index];
		}
		
		private static AutoAdapter _adapters;
		public static AutoAdapter Adapters {
			get {
#if UNITY_EDITOR
				if (!Application.isPlaying) {
					EnsureAdapter();
				}
#endif
				return _adapters;
			}
		}
		
		public class AutoAdapter
		{
			private Dictionary<Type, IAdapter> _adapters = new Dictionary<Type, IAdapter>();
			public IAdapter GetAdapter(Type type)
			{
				if (_adapters.TryGetValue(type, out IAdapter adapter))
					return adapter;
				return null;
			}
			public void RegisterAdapter(Type type, IAdapter adapter)
			{
				_adapters.Add(type, adapter);
			}
		}
		
		protected void OnEnable()
		{
			EnsureAdapter();
		}

		/// <summary>
		/// Ensures known adapters existing.
		/// </summary>
		private static void EnsureAdapter()
		{
			if (_adapters == null) {
				_adapters = new AutoAdapter();
				_adapters.RegisterAdapter(typeof(Sprite), CreateInstance<SpriteAdapter>());
			}
		}
		
	}

	[Serializable] public class VisualMap : AutoIndexedMap<Visual> { }

	[Serializable]
	public struct Visual
	{

		#if UNITY_EDITOR
		public UnityEngine.Object Reference { get => _reference; set => _reference = value; }
		#endif
		
		[SerializeField] private UnityEngine.Object _reference;
		public IDecorationRenderer Visualize(VisualSet set, DecorationComponent.Node node, bool final, Transform parent)
		{
			if (_reference is IVisual visual)
				return visual.Visualize(node, final, parent);
			
			var adapter = VisualSet.Adapters.GetAdapter(_reference.GetType());
			if (adapter == null)
				return NullVisual.Visualize(set, node, false, parent);
			
			return adapter.Visualize(set, _reference, node, final, parent);
		}

		public void UpdateVisualization(VisualSet set, ref IDecorationRenderer renderer, DecorationComponent.Node node, Transform parent) {
			if (_reference is IVisual visual) {
				visual.UpdateVisualization(ref renderer, node);
				return;
			}
			
			var adapter = VisualSet.Adapters.GetAdapter(_reference.GetType());
			if (adapter == null) {
				NullVisual.UpdateVisualization(set, ref renderer, node, parent);
				return;
			}
			
			adapter.UpdateVisualization(set, ref renderer, _reference, node, parent);
		}
		private static Visual _nullVisual;
		public static Visual NullVisual {
			get {
#if UNITY_EDITOR
				Debug.LogError("Null Visual is needed from a visual set. Something " +
				               "must be going wrong with a decoration component. " +
				               "\nProbably you deleted a visual from a visual set and " +
				               "didn't delete the same decorations from level.");
#endif
				if (_nullVisual._reference == null)
					_nullVisual = new Visual {
						_reference = CreateNullSprite()
					};
				return _nullVisual;
			}
		}
		private static Sprite CreateNullSprite(int size = 64) {
			var texture2D = new Texture2D(size, size, TextureFormat.RGBA32, false);
			var colorMatrix = new Color[size * size];
			for (int i = 0; i < size * size; i++) colorMatrix[i] = Color.magenta;
			texture2D.SetPixels(0, 0, size, size, colorMatrix);
			texture2D.Apply();
			var sprite = Sprite.Create(texture2D, new Rect(Vector2.zero, new Vector2(texture2D.width, texture2D.height)), Vector2.zero);
			sprite.name = " --  Error -- Missing";
			return sprite;
		}

		public override string ToString()
		{
			if (_reference) {
				return _reference.name;
			}
			return "null";
		}
	}
}