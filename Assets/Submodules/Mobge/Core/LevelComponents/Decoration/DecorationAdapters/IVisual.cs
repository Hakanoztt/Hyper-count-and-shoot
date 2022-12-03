using System.Collections;
using System.Collections.Generic;
using Mobge.Core.Components;
using UnityEngine;

namespace Mobge.Core
{
    public interface IVisual
    {
        IDecorationRenderer Visualize(DecorationComponent.Node node, bool final, Transform parent);
        void UpdateVisualization(ref IDecorationRenderer decorationRenderer, DecorationComponent.Node node);
	}
    public interface IDecorationRenderer
    {
        Transform Transform { get; }
    }
}
