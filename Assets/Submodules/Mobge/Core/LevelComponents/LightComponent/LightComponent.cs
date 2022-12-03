using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components
{
	/// <summary>
	/// Light component.
	/// </summary>
	public class LightComponent : ComponentDefinition<LightComponent.Data>
	{
		[Serializable]
		public class Data : BaseComponent, IChild
		{
			[SerializeField]
			[HideInInspector]
			private LogicConnections _connections;
			private LightRenderer _renderer;
			[HideInInspector] [SerializeField] private ElementReference _parent = -1;
			public ElementReference Parent { get => _parent; set => _parent = value; }
			public Color color = Color.white;
			public LightType type = LightType.Point;
			public float intensity = 1;
			public float indirectMultiplier = 1;
			public LightShadows shadows;
			public float range = 10;
			public float spotAngle;
			public enum RenderMode { Auto, Important, NotImportant }
			public RenderMode renderMode;
			public bool enabled = true;

			public override void Start(in InitArgs initData)
			{
				_renderer = CreateRenderer(position, initData.parentTr);
				UpdateRenderer(_renderer);
			}
			public override LogicConnections Connections {
				get {
					return _connections;
				}
				set {
					_connections = value;
				}
			}
			public override object HandleInput(ILogicComponent sender, int index, object input)
			{
				var _light = _renderer.Light;
				switch (index) {
					case 0:
						_light.enabled = true;
						break;
					case 1:
						_light.enabled = false;
						break;
					case 2:
						_light.color = (Color)input;
						break;
					case 3:
						_light.intensity = (float)input;
						break;
				}
				return null;
			}
#if UNITY_EDITOR
			public override void EditorInputs(List<LogicSlot> slots)
			{
				slots.Add(new LogicSlot("enable", 0));
				slots.Add(new LogicSlot("disable", 1));
				slots.Add(new LogicSlot("color", 2, typeof(Color)));
				slots.Add(new LogicSlot("light intensity", 3, typeof(float)));
			}
#endif

			public void UpdateRenderer(LightRenderer renderer)
			{
				var tr = renderer.Transform;
				var l = renderer.Light;
				// Lightmap bake type should be set first. Certain types may override bake type.
				l.type = type;
				switch (l.type) {
					case LightType.Spot:
						l.spotAngle = Mathf.Clamp(spotAngle, 1, 180);
						l.range = range;
						break;
					case LightType.Point:
						l.range = range;
						break;
					case LightType.Area:
						//l.SetLightDirty();
						break;
				}
				switch (renderMode) {
					case RenderMode.Auto:
						l.renderMode = LightRenderMode.Auto;
						break;
					case RenderMode.Important:
						l.renderMode = LightRenderMode.ForcePixel;
						break;
					case RenderMode.NotImportant:
						l.renderMode = LightRenderMode.ForceVertex;
						break;
				}
				if (l.color != color) {
					l.color = color;
					tr.name = Rename();
				}
				l.intensity = intensity;
				l.bounceIntensity = indirectMultiplier;
				l.shadows = shadows;
				l.enabled = enabled;
			}

			public LightRenderer CreateRenderer(Vector3 position, Transform parent)
			{
				string _name = Rename();
				Transform tr = new GameObject(_name).transform;
				tr.SetParent(parent, false);
				tr.localPosition = position;
				return new LightRenderer(tr);
			}

			private string Rename() => "Light | Color: " + ColorUtility.ToHtmlStringRGBA(color);

			public class LightRenderer
			{
				public Transform Transform { get; }
				public Light Light { get; }
				public LightRenderer(Transform tr)
				{
					Transform = tr;
					Light = Transform.gameObject.AddComponent<Light>();
				}
			}
		}
	}
}