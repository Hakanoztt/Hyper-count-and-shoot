using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mobge {
    public class LabelPickerAttribute : PropertyAttribute
    {
        public readonly string label;
        public readonly bool allowSceneObjects;
        public readonly Type type;
        public LabelPickerAttribute(string label = null, bool allowSceneObjects = false, Type type = null) {
            this.label = label;
            this.allowSceneObjects = allowSceneObjects;
            this.type = type;
        }
    }
}

