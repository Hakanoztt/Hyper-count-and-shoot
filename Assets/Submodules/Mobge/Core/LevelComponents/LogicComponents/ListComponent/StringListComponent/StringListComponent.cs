using System;

namespace Mobge.Core.Components {
    public class StringListComponent : BaseListComponent<StringListComponent.Data, string> {
        [Serializable] public class Data : Data<string> { }
    }
}