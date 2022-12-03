using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    [Serializable]
    public struct ElementReference
    {
        public ElementReference(int id) {
            this.id = id;
        }
        public int id;
        public static implicit operator int (ElementReference e) => e.id; 
        public static implicit operator ElementReference (int id) => new ElementReference(id); 
    }
    public class ElementReferenceAttribute : PropertyAttribute
    {
        public Type elementType;

        public ElementReferenceAttribute(Type elementType) {
            this.elementType = elementType;
        }
    }
}
