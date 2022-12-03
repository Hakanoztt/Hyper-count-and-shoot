using System;

namespace Mobge.Core.Components {
    public class NumberListComponent : BaseListComponent<NumberListComponent.Data, float> {
	    [Serializable] public class Data : Data<float> { }
    }
}
