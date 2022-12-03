using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.StateMachineAI {
    [CustomPropertyDrawer(typeof(AIComponentIndexAttribute))]
    public class AIComponentIndexDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            bool editorDrawed = false;
            var obj = property.serializedObject.targetObject;
            if(EStateAI.TryGetAI(obj, out var ai)) {
                editorDrawed = true;
                property.intValue = EditorDrawer.Popup(position, label.text, ai.componentVariables, property.intValue);
            }

            if (!editorDrawed) {
                EditorGUI.PropertyField(position, property, label);
                //base.OnGUI(position, property, label);
            }
        }
    }
}