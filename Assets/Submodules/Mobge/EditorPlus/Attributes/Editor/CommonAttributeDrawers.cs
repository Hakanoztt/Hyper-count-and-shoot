using UnityEditor;
using UnityEngine;

namespace Mobge {
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    public class LayerAttributeDrawer : BasePropertyDrawer {
        public int LayerCount { get => 32; }
        private string[] _allLayers;
        protected void OnEnable() {
            if(_allLayers!=null) return;
            
            _allLayers = new string[LayerCount];
            for(int i = 0; i < LayerCount; i++) {
                var name = LayerMask.LayerToName(i);
                _allLayers[i] = name;
            }
        }
        public override void OnGUI(UnityEngine.Rect position, UnityEditor.SerializedProperty property, UnityEngine.GUIContent label) {
            OnEnable();
            property.intValue = EditorDrawer.Popup(position, label.text, _allLayers, property.intValue);
        }
    }
    [CustomPropertyDrawer(typeof(LayerMaskAttribute))]
    public class LayerMaskAttributeDrawer : BasePropertyDrawer {
        public override void OnGUI(UnityEngine.Rect position, UnityEditor.SerializedProperty property, UnityEngine.GUIContent label) {
            property.intValue = InspectorExtensions.LayerMaskField(position, label.text, property.intValue);
        }
    }
    [CustomPropertyDrawer(typeof(OwnComponentAttribute))]
    public class OwnComponentAttributeDrawer : BasePropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if(property.serializedObject.targetObjects.Length > 1) {
                var p1 = position;
                var p2 = position;
                p1.width = EditorGUIUtility.labelWidth;
                p2.width -= p1.width;
                p2.x += p1.width;
                GUI.Label(p1, label);
                GUI.Label(p2, "Multiple object editing is not supported.");
                return;
            }
            var target = property.serializedObject.targetObject;
            if(!(target is Component)) {
                var c = GUI.contentColor;
                GUI.contentColor = Color.red;
                EditorGUI.LabelField(position, "ERROR! [OwnComponent] attribute can only be used at a Component property! " + GetType());
                GUI.contentColor = c;
            }
            else if (!fieldInfo.FieldType.IsClass) {
                var c = GUI.contentColor;
                GUI.contentColor = Color.red;
                EditorGUI.LabelField(position, "ERROR! [OwnComponent] attribute can only be used to find Component types! " + GetType());
                GUI.contentColor = c;
            }
            else {
                var self = target as Component;
                var att = attribute as OwnComponentAttribute;
                var ft = fieldInfo.FieldType;
                var fi = ft.IsArray ? ft.GetElementType() : ft;
                
                Component[] comps = self.GetComponentsInChildren(att.type == null ? fi : att.type, true);
                property.objectReferenceValue = EditorDrawer.Popup(position, label.text, comps, property.objectReferenceValue, (c) => c, att.findAutomatically ? null as string : "none");
                if(property.objectReferenceValue == null && att.findAutomatically) {
                    if(comps.Length > 0) {
                        property.objectReferenceValue = comps[0];
                    }
                }
            }
        }
    }
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUI.GetPropertyHeight(property, label, true);
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}

