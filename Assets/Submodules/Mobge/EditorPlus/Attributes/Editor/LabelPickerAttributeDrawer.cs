using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mobge {
    [CustomPropertyDrawer(typeof(LabelPickerAttribute))]
    public class LabelPickerAttributeDrawer : BasePropertyDrawer {
        public override void OnGUI(
            UnityEngine.Rect position,
            UnityEditor.SerializedProperty property,
            UnityEngine.GUIContent label) {

            var att = (LabelPickerAttribute) attribute;
            var type = att.type != null ? att.type : fieldInfo.FieldType;
            if (type.IsArray) {
                type = type.GetElementType();
            }
            property.objectReferenceValue = 
                InspectorExtensions.CustomFields.LabelPicker.DrawLabeledObjectPicker(
                    position, 
                    label.text, 
                    property.objectReferenceValue, 
                    type,
                    att.label,
                    att.allowSceneObjects);
        }
    }
}


