using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mobge.Platformer.Character{
    [CustomPropertyDrawer(typeof(CharacterEffect))]
    public class ECharacterEffect : PropertyDrawer
    {
        private LayoutRectSource _rects = new LayoutRectSource();
        private float _height;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginChangeCheck();
            _rects.Reset(position);
            var values = property.FindPropertyRelative(nameof(CharacterEffect.values));
            var probability = property.FindPropertyRelative(nameof(CharacterEffect.probability));
            var effect = property.FindPropertyRelative(nameof(CharacterEffect.effect));
            var effectObj = effect.objectReferenceValue as ACharacterEffect;
            var visualEffect = property.FindPropertyRelative(nameof(CharacterEffect.visualEffect));
            if(effectObj != null) {
                EditorGUI.PropertyField(_rects.NextRect(), probability, true);
                var names = effectObj.EditorValueNames;
                values.arraySize = names.Length;
                for(int i = 0; i < names.Length; i++) {
                    var element = values.GetArrayElementAtIndex(i);
                    element.floatValue = EditorGUI.FloatField(_rects.NextRect(), names[i], element.floatValue);
                }
                EditorGUI.PropertyField(_rects.NextRectWithLineCount(2), visualEffect, true);

            }
            effect.objectReferenceValue = EditorDrawer.ObjectField(_rects, effect.displayName, effectObj, false);
            _height = _rects.Height;
            if(EditorGUI.EndChangeCheck()) {
                property.serializedObject.ApplyModifiedProperties();
            }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            
            OnGUI(new Rect(-10000, -10000, 10, 10), property, label);
            return _height;
        }
    }
}
