using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge
{
    public interface IVisualSpawner 
    {
#if UNITY_EDITOR
        Transform CreateVisuals();
        void UpdateVisuals(Transform instance);
#endif
    }
}