using System.Collections;
using System.Collections.Generic;
using Mobge.Core.Components;
using UnityEngine;

namespace Mobge.Core {
    public interface IAdapter { 
        IDecorationRenderer Visualize(VisualSet set, Object obj, DecorationComponent.Node node, bool final, Transform parent);
        void UpdateVisualization(VisualSet set, ref IDecorationRenderer renderer, Object obj, DecorationComponent.Node node, Transform parent);
    }
}
