using UnityEngine;

namespace Mobge {
    public class CacheBehaviour : MonoBehaviour  {
        public PrefabCache<Component> cache;
        public void Initialize() {
            cache = new PrefabCache<Component>(true, true);
        }
    }
}
