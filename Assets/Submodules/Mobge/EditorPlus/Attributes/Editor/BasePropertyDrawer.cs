using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge{
    public class BasePropertyDrawer : PropertyDrawer{
        static PropertyDescriptor _tempKey = new PropertyDescriptor();
        private float _height;
        //private static Dictionary<PropertyDescriptor, float> _heights = new Dictionary<PropertyDescriptor, float>();
        protected void SetHeight(SerializedProperty property, float height){
            //_heights[new PropertyDescriptor(property)] = height;
            _height = height;

        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label){
            //OnGUI(new Rect(100000,1000000, 0, 0), property, label);
            _height = 0;
            OnGUI(new Rect(-10000, -10000, 10, 10), property, label);
            _tempKey.SetProperty(property);
            float height;
            // if(!_heights.TryGetValue(_tempKey, out height)){
            //     return EditorGUIUtility.singleLineHeight;
            // }
            if(_height == 0) {
                _height = EditorGUIUtility.singleLineHeight;
            }
            height = _height;
            return height;
        }
    }
}