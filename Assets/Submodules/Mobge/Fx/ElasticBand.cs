using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace Mobge.Fx
{
	public class ElasticBand : MonoBehaviour
	{
		public GameObject AnchorPoint;
		public Settings setting;
		public Material material;
		public bool isSnapToAnchor;
		[NonSerialized] public bool isSnapTriggered;
		private float ctrlFLoat;

		public ElasticBandGraphics Graphics { get; set; }
		private AElasticBandData _data;
		public AElasticBandData Data {
            get => _data; set {
                _data = value;
                if (Graphics) Graphics.Data = _data;
            }
        }

		public void GenerateData(Vector3 anchorPoint, Settings settings)
		{
			switch (setting.type) {
				case DataType.Circlar:
					Data = new CircularElasticBandData(anchorPoint, settings);
					break;
				case DataType.Linear:
					Data = new LinearElasticBandData(anchorPoint, settings);
					break;
				default:
					Data = new LinearElasticBandData(anchorPoint, settings);
					break;
			}
		}

		private void Awake()
		{
			var _graphicRoot = new GameObject("Graphic Root").transform;
			_graphicRoot.SetParent(transform, false);
			Graphics = _graphicRoot.gameObject.AddComponent<ElasticBandGraphics>();
			GenerateData(AnchorPoint.transform.position, setting);
			Graphics.ReConstruct(material);
		}

		private void FixedUpdate()
		{
			Data.Update(Time.deltaTime);
		}

		private void Update()
		{
			Graphics.Paint();
			if (isSnapTriggered) {
				Snapback(isSnapToAnchor);

				if (ctrlFLoat > 1) {
					isSnapTriggered = false;
				}
				ctrlFLoat += 0.125f;
				AnchorPoint.transform.position = transform.position;
			} else {
				ctrlFLoat = 0;
				Data.EndPoint = transform.position;
			}
		}


		public void Snapback(bool isToAnchor)
		{
			Data.Snapback(isToAnchor);
			Graphics.SnapBack(isToAnchor);
		}

		public enum DataType
		{
			Linear,
			Circlar
		}

		[Serializable]
		public struct Settings
		{
			public DataType type;
			public int width;
			public float mass;
			public int numberOfSpring;
			public float springConstant;
			public float drag;
			public float gravityScale;
		}
	}
}
