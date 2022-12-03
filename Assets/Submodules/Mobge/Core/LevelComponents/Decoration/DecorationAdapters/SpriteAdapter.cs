using System;
using System.Collections;
using System.Collections.Generic;
using Mobge.Core.Components;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mobge.Core {
	public class SpriteAdapter : ScriptableObject, IAdapter {
		public IDecorationRenderer Visualize( VisualSet set, UnityEngine.Object obj, DecorationComponent.Node node, bool final, Transform parent) {
			var sprite = (Sprite)obj;
			var transform = node.transformInfo.CreateTransform(sprite.name);
			transform.SetParent(parent, false);
			var spriteRenderer = transform.gameObject.AddComponent<SpriteRenderer>();
			var spriteRendererAdapter = transform.gameObject.AddComponent<SpriteRendererAdapter>();
			spriteRendererAdapter.spriteRenderer = spriteRenderer;
			
			spriteRenderer.sprite = sprite;
			if (set != null) {
				spriteRenderer.material = set.GetMaterial(node.materialId);
			}
			spriteRenderer.color = node.color;
			spriteRenderer.flipX = node.flipX;
			spriteRenderer.flipY = node.flipY;
			spriteRenderer.receiveShadows = node.receiveShadows;

			return spriteRendererAdapter;
		}

		public void UpdateVisualization(VisualSet set, ref IDecorationRenderer renderer, Object obj, DecorationComponent.Node node, Transform parent) {
			var sprite = (Sprite)obj;

			var spriteRendererAdapter = renderer as SpriteRendererAdapter;
			if (spriteRendererAdapter == null) {
				DestroyImmediate(renderer.Transform.gameObject);
				renderer = Visualize(set, obj, node, false, parent);
				return;
			}
			
			var spriteRenderer = spriteRendererAdapter.spriteRenderer;
			spriteRenderer.sprite = sprite;
			if (set != null) {
				spriteRenderer.material = set.GetMaterial(node.materialId);
			}
			spriteRenderer.color = node.color;
			spriteRenderer.flipX = node.flipX;
			spriteRenderer.flipY = node.flipY;
			spriteRenderer.receiveShadows = node.receiveShadows;
		}
	}

	public class SpriteRendererAdapter : MonoBehaviour, IDecorationRenderer {
		public Transform Transform => transform;
		public SpriteRenderer spriteRenderer;
	}
}
