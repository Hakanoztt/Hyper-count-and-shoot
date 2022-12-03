using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Mobge.Core.Components
{
	public class DecorationComponent : ComponentDefinition<DecorationComponent.Data>
	{
		public class RendererList {
			public Transform Transform { get; }
			public RendererList(Transform transform)
			{
				Transform = transform;
				renderers = new List<IDecorationRenderer>();
			}
			public readonly List<IDecorationRenderer> renderers;
		}

		[Serializable] public class AssetReferenceVisualSet : AssetReferenceTyped<VisualSet> { }

		[Serializable]
		public class Data : BaseComponent, IRotationOwner, IChild, IResourceOwner
		{
			public string name;
			[SerializeField]
			[HideInInspector]
			private ElementReference _parent = -1; 
			public bool IsFinal { get; set; }
			Quaternion IRotationOwner.Rotation { get => rotation; set => rotation = value; }
			ElementReference IChild.Parent { get => _parent; set => _parent = value; }
			[SerializeField] public AssetReferenceVisualSet visualSet;
			public int setId;
			
			int IResourceOwner.ResourceCount => visualSet != null && visualSet.RuntimeKeyIsValid() ? 1 : 0;
			AssetReference IResourceOwner.GetResource(int index) => visualSet;
			public Vector3 scale = Vector3.one;
			public Quaternion rotation = Quaternion.identity;
			public Node[] nodes = Node.NewEmptyData();

			public override void Start(in InitArgs initData) {
				var origin = position;
				var player = initData.player;
				var level = player.level;
				var parentTr = initData.parentTr;
				CreateRenderers(level, origin, true, parentTr);
			}
			public RendererList CreateRenderers(Level level, Vector3 offset, bool final, Transform parent) {
				var rl = new RendererList(new GameObject("_decorRenderer").transform);
				rl.Transform.SetParent(parent, false);
				rl.Transform.localPosition = offset;
				rl.Transform.localRotation = rotation;
				rl.Transform.localScale = scale;
				
				var set = GetVisualSet(level);
				if (set == null) {
					#if UNITY_EDITOR
					if (Application.isPlaying) {
						Debug.LogError("Decoration Component Visual Set is null! Something is wrong!");
					}
					#endif
				}
				
				for (int i = 0; i < nodes.Length; i++) {
					var visual = GetVisual(set, nodes[i].decorId);
					var decorationRenderer = visual.Visualize(set, nodes[i], IsFinal, rl.Transform);
					rl.renderers.Add(decorationRenderer);
				}
				return rl;
			}
			public void UpdateRenderer(Level level, RendererList rendererList) {
				var set = GetVisualSet(level);
				// rendererList = SanityCheck(rendererList, set);
				for (int i = 0; i < nodes.Length; i++) {
					rendererList.Transform.localScale = scale;
					var visual = GetVisual(set, nodes[i].decorId);

					var renderer = rendererList.renderers[i];
					visual.UpdateVisualization(set, ref renderer, nodes[i], rendererList.Transform);
					if (renderer != null) {
						nodes[i].transformInfo.UpdateTransform(renderer.Transform);
					}
					rendererList.renderers[i] = renderer;
				}
			}
			public VisualSet GetVisualSet(Level level) {
				if (visualSet != null && visualSet.RuntimeKeyIsValid())
					return visualSet.LoadedAsset;
				var decorationSetAddressableReference = level.decorationSet;
				var decorationSet = decorationSetAddressableReference.LoadedAsset;
				if (decorationSet != null) 
					return decorationSet.GetVisualSet(setId);
				return null;
			}
			public Visual GetVisual(VisualSet set, int decorId) {
				if (set == null) return Visual.NullVisual;
				if (set.visuals == null) return Visual.NullVisual;
				if (!set.visuals.ContainsKey(decorId)) return Visual.NullVisual;
				return set.visuals[decorId];
			}

			
			/// <summary>
			/// Findsout if the present transformation list is sane to work with, if not prepares and delivers a correct list.
			/// </summary>
			/// <returns>Correct renderer transformation list.</returns>
			/// <param name="rl">Renderer list to sanity check.</param>
			/// <param name="vs">Current visual set.</param>
			// public RendererList SanityCheck(RendererList rl, VisualSet vs)
			// {
			// 	int neededNumberOfControllers = nodes.Length - rl.renderers.Count;
			// 	switch (neededNumberOfControllers) {
			// 		// Node added
			// 		case int n when neededNumberOfControllers > 0:
			// 			for (int i = 0; i < neededNumberOfControllers; i++) {
			// 				var dv = GetVisual(vs, nodes[i].decorId);
			// 				rl.renderers.Add(dv.Visualize(vs, nodes[i], IsFinal, rl.Transform));
			// 			}
			// 			return rl;
			// 		// Number of nodes did not change, data list is sane.
			// 		default:
			// 			return rl;
			// 		// Node removed
			// 		case int n when neededNumberOfControllers < 0:
			// 			rl.Transform.DestroyAllChildren();
			// 			rl.renderers.Clear();
			// 			for (int i = 0; i < nodes.Length; i++) {
			// 				var dv = GetVisual(vs, nodes[i].decorId);
			// 				rl.renderers.Add(dv.Visualize(vs, nodes[i], IsFinal, rl.Transform));
			// 			}
			// 			return rl;
			// 	}
			// }
			public override string ToString() {
				return name;
			}
		}
		[Serializable]
		public class Node : IRotationOwner {
			public TransformInfo transformInfo;
			public int decorId;
			public int materialId;
			public Color32 color = Color.white;
			public bool flipX = false;
			public bool flipY = false;
			public bool receiveShadows = false;

			public Quaternion Rotation { get => transformInfo.rotation; set => transformInfo.rotation = value; }
			public static Node[] NewEmptyData() => new Node[0];
		}

		[Serializable]
		public struct TransformInfo {
			public Vector3 position;
			public Quaternion rotation;
			public Vector3 scale;
			public TransformInfo(Vector3 position, Quaternion rotation, Vector3 scale) {
				this.position = position;
				this.rotation = rotation;
				this.scale = scale;
			}
			public static TransformInfo NewEmptyData() {
				return new TransformInfo(Vector3.zero, Quaternion.identity, Vector3.one);
			}
			public Transform CreateTransform(string gameObjectName = default) {
				var t = new GameObject(gameObjectName).transform;
				t.localPosition = position;
				t.localRotation = rotation;
				t.localScale = scale;
				return t;
			}
			public void UpdateTransform(Transform t) {
				t.localPosition = position;
				t.localRotation = rotation;
				t.localScale = scale;
			}
			public void SetByTransform(Transform t) {
				position = t.localPosition;
				rotation = t.localRotation;
				scale = t.localScale;
			}
		}
	}
}