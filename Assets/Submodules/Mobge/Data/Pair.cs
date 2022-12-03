using System;

namespace Mobge {

    [Serializable]
    public class Pair<T> {
#if UNITY_EDITOR
        public string label;
#endif
        public T value;
    }
}