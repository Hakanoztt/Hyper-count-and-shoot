using System.Collections.Generic;
using Mobge.Core.Components;
using UnityEngine;

namespace Mobge {
    public class PolygonInstance : IPolygonRenderer {
        public PolygonInstance() { }
        public PolygonInstance (Color color) {
            _color = color;
        }

        public MeshRenderer[] meshRenderers;
        public MeshFilter[] meshFilters;

        private static readonly List<Color> StaticColorCache = new List<Color>();
        private Color _color;

        public Transform Transform { get; set; }
        public Color Color {
            get => _color;
            set {
                if (_color == value) return;
                _color = value;
                for (int i = 0; i < meshFilters.Length; i++) {
                    var mesh = meshFilters[i].sharedMesh;
                    if (mesh != null) {
                        int vertexCount = mesh.vertexCount;
                        StaticColorCache.Clear();
                        // StaticColorCache.SetCountFast(vertexCount);
                        for (int j = 0; j < vertexCount; j++) {
                            StaticColorCache.Add(_color);
                            // StaticColorCache.array[j] = _color;
                        }
                        mesh.SetColors(StaticColorCache);
                        // mesh.SetColors(StaticColorCache.array, 0, vertexCount);
                    }
                }
            }
        }
    }
}