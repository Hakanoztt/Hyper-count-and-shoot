using UnityEditor;
using UnityEngine;

namespace Mobge {
    [CustomPropertyDrawer(typeof(InterfaceConstraintAttribute))]
    public class InterfaceConstraintAttributeDrawer : PropertyDrawer {


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
            var att = (InterfaceConstraintAttribute)this.attribute;
            if(att.referenceField != null){
                property = property.FindPropertyRelative(att.referenceField);
            }
            float editorWidth = position.width - EditorGUIUtility.labelWidth;
            var p1 = position;
            p1.width -= editorWidth * 0.5f;
            float p2XOffset = EditorGUIUtility.labelWidth + editorWidth * 0.5f;
            var p2 = position;
            p2.xMin += p2XOffset;
            
            EditorGUI.ObjectField(p1, property, label);
            Component component = null;
            if(property.objectReferenceValue is GameObject go) {
                component = go.transform;
            }
            else if(property.objectReferenceValue is Component comp) {
                component = comp;
            }
            if (component != null) {
                var comps = component.GetComponents(att.interfaceType);
                component = EditorDrawer.Popup(p2, null, comps, component, cc => cc);
                property.objectReferenceValue = component;
            }
            else if (property.objectReferenceValue != null) {
                if(!att.interfaceType.IsAssignableFrom(property.objectReferenceValue.GetType())) {
                    property.objectReferenceValue = null;
                }
            }
            //property.objectReferenceValue = EditorGUI.ObjectField(position, label, property.objectReferenceValue, att.interfaceType, true);
        }
    }
}