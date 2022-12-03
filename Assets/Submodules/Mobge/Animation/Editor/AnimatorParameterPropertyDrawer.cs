using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using System.Linq;
using static Mobge.UnityExtensions;
using System.Security.Policy;

namespace Mobge.Animation {
    
    public abstract class AnimatorParameterAttributeDrawer : AnimatorStateAttributeDrawer {
        
        protected override void DrawPicker(Rect position, SerializedProperty property, GUIContent label, AnimatorController controller, string addNoneOption = null) {
            DrawField(position, label, this.Type, controller, property);
        }
        protected abstract AnimatorControllerParameterType Type { get; }
        public static void DrawField(Rect position, GUIContent label, AnimatorControllerParameterType type, AnimatorController controller, SerializedProperty property, string noneOption = "<none>") {
            var hashToName = AnimatorUtility.hashToName;
            var nameToHash = AnimatorUtility.nameToHash;
            hashToName.Clear();
            nameToHash.Clear();
            var parameters = controller.parameters;
            foreach (var param in parameters) {
                
                if (param.type == type) {
                    hashToName.Add(param.nameHash, param.name);
                    nameToHash.Add(param.name, param.nameHash);
                }
            }
            if (hashToName.Count <= 0) {
                var c = GUI.contentColor;
                GUI.contentColor = Color.red;
                EditorGUI.LabelField(position, "ERROR on [AnimatorParameter] attribute. Attached animators do not have any matching parameters! " + type);
                GUI.contentColor = c;
                return;
            }
            EditorGUI.BeginChangeCheck();
            if (property.propertyType == SerializedPropertyType.Integer) {
                if (!hashToName.ContainsKey(property.intValue)) {
                    property.intValue = 0;
                }
                hashToName.TryGetValue(property.intValue, out var defVal);
                var val = EditorDrawer.Popup(position, label.text, hashToName.Values.ToList(),
                                defVal, (c) => c, "None");

                property.intValue = val == null ? 0 :
                    nameToHash[val];
            }
            else {
                if (!nameToHash.ContainsKey(property.stringValue)) {
                    property.stringValue = nameToHash.Keys.ToArray()[0];
                }
                property.stringValue =
                    EditorDrawer.Popup(position, label.text, hashToName.Values.ToList(),
                        property.stringValue, (c) => c, noneOption);
            }
            if (EditorGUI.EndChangeCheck()) {
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
    [CustomPropertyDrawer(typeof(AnimatorFloatParameterAttribute))]
    public class AnimatorFloatParameterAttributeDrawer : AnimatorParameterAttributeDrawer {
        protected override AnimatorControllerParameterType Type => AnimatorControllerParameterType.Float;

    }
    [CustomPropertyDrawer(typeof(AnimatorIntParameterAttribute))]
    public class AnimatorIntParameterAttributeDrawer : AnimatorParameterAttributeDrawer {
        protected override AnimatorControllerParameterType Type => AnimatorControllerParameterType.Int;
    }
    [CustomPropertyDrawer(typeof(AnimatorBoolParameterAttribute))]
    public class AnimatorBoolParameterAttributeDrawer : AnimatorParameterAttributeDrawer {
        protected override AnimatorControllerParameterType Type => AnimatorControllerParameterType.Bool;
    }
    [CustomPropertyDrawer(typeof(AnimatorTriggerAttribute))]
    public class AnimatorTriggerParameterAttributeDrawer : AnimatorParameterAttributeDrawer {
        protected override AnimatorControllerParameterType Type => AnimatorControllerParameterType.Trigger;
    }
}