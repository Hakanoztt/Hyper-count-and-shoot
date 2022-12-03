using System;
using UnityEngine;

namespace Mobge.Core.Components {
    public class Vector3ListComponent : BaseListComponent<Vector3ListComponent.Data, Vector3> {
        [Serializable] public class Data : Data<Vector3> { }
    }
}