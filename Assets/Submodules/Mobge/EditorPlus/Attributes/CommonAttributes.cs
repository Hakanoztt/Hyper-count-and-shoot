using System;
using UnityEngine;

namespace Mobge {
    public class LayerAttribute : PropertyAttribute { }
    public class LayerMaskAttribute : PropertyAttribute { }
    public class OwnComponentAttribute : PropertyAttribute {
        public Type type;
        public bool findAutomatically;

        public OwnComponentAttribute(Type type, bool findAutomatically = false) {
            this.type = type;
            this.findAutomatically = findAutomatically;
        }
        public OwnComponentAttribute(bool findAutomatically = false) : this(null, findAutomatically) {
        }
    }
    public class ReadOnlyAttribute : PropertyAttribute { }
}