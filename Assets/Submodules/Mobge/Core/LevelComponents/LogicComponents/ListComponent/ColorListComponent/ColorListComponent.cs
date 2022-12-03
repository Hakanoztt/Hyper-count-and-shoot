using System;
using UnityEngine;

namespace Mobge.Core.Components {
    public class ColorListComponent : BaseListComponent<ColorListComponent.Data, Color> { 
        [Serializable] public class Data : Data<Color> { }
    }
}
