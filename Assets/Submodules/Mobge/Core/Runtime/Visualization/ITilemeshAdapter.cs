using UnityEngine;
using Mobge.Core.Components;
namespace Mobge.Core
{
    /// <summary>
    /// Classes that implement Tilemesh adapter interface is responsible of visualizing data.
    /// </summary>
    public interface IPieceVisualizer
    {
        IPieceRenderer Visualize(GridInfo gi, Vector3 offset, int decorIndex, bool final, Transform parent);
        void UpdateVisuals(IPieceRenderer obj, GridInfo gi, int decorIndex);
        bool SupportCollider();
        void UpdateCollider(IPieceRenderer renderer);
    }
    public interface IPieceRenderer{
        Transform Transform{get;}
    }
}