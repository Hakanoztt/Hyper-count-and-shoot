using System;
using UnityEngine;

namespace Mobge {
    public class InterfaceConstraintAttribute : PropertyAttribute {
        public Type interfaceType;
        public string referenceField;
        public InterfaceConstraintAttribute(Type interfaceType) {
            this.interfaceType = interfaceType;
        }
        public InterfaceConstraintAttribute(Type interfaceType, string referenceField) {
            this.referenceField = referenceField;
            this.interfaceType = interfaceType;
        }
    }
}