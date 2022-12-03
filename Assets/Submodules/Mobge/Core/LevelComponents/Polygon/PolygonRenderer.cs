using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Mobge.Core.Components
{
	public class PolygonRenderer : IPolygonRenderer
	{
		private Transform _transform;
		public PolygonRenderer(Transform tr)
		{
			_transform = tr;
		}
		public Transform Transform => _transform;

        public Color Color { get => Color.white; set { } }
    }
}