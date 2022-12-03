using UnityEngine;

namespace Mobge.Core.Components {

    [DisallowMultipleComponent]
    public class ComponentExtensionGroup : MonoBehaviour, IComponentExtension {

        public Component[] componentExtensions;

        void IComponentExtension.Start(in BaseComponent.InitArgs initData) {
            foreach(IComponentExtension componentExtension in componentExtensions) {
                componentExtension.Start(initData);
            }
        }
    }
}
