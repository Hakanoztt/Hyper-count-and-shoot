
using Mobge.Core.Components;
using System;
using System.Security;
using UnityEngine;

namespace Mobge {
    public class PolygonRenderer : MonoBehaviour {
        private PolygonInstance _polygonInstance;
        public Data data = new Data() {
            color = Color.white
        };
        
        protected void OnEnable() {
            if (data.IsValid) {
                //EnsureInstance();
                UpdateVisuals();
            }
        }
        public void EnsureInstance() {
            if (_polygonInstance == null && data.IsValid) {
                _polygonInstance = PolygonCalculator.Instance.CreatePolygonInstance(data.visualizer, null, Vector3.zero, Quaternion.identity, transform);
            }
        }
        public void UpdateVisuals() {
            EnsureInstance();
            _polygonInstance.Color = data.color;
            data.visualizer.UpdateVisuals(_polygonInstance, data.polygons);
        }

        [Serializable]
        public struct Data {
            public int subdivisionCount;
            public int cubicness;
            public Polygon[] polygons;
            public PolygonVisualizer visualizer;
            public Color color;
            
            public bool IsValid {
                get => visualizer != null;
            }
        }
    }
}