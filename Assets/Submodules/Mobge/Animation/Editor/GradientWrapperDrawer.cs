using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Mobge.GradientWrapper;

namespace Mobge {
    [CustomPropertyDrawer(typeof(GradientWrapper))]
    public class GradientWrapperDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            //base.OnGUI(position, property, label);
            var g = GetGradient(property);
            EditorGUI.BeginChangeCheck();
            g = EditorGUI.GradientField(position, label, g);
            if (EditorGUI.EndChangeCheck()) {
                SetGradient(property, g);
            }
        }

        Gradient GetGradient(SerializedProperty property) {
            var p_colors = property.FindPropertyRelative(c_colorKeysName);
            var p_alphas = property.FindPropertyRelative(c_alphaKeysName);
            var p_mode = property.FindPropertyRelative(c_modeName);
            GradientWrapper gw = new GradientWrapper();
            gw.colorKeys = new ColorKey[p_colors.arraySize];
            gw.alphaKeys = new AlphaKey[p_alphas.arraySize];
            gw.mode = (GradientMode)p_mode.intValue;

            for (int i = 0; i < gw.colorKeys.Length; i++) {
                GradientColorKey key;
                var p = p_colors.GetArrayElementAtIndex(i);
                var cc = p.FindPropertyRelative(nameof(GradientColorKey.color));
                key.color.r = cc.FindPropertyRelative("r").floatValue;
                key.color.g = cc.FindPropertyRelative("g").floatValue;
                key.color.b = cc.FindPropertyRelative("b").floatValue;
                key.color.a = cc.FindPropertyRelative("a").floatValue;
                key.time = p.FindPropertyRelative(nameof(GradientColorKey.time)).floatValue;
                gw.colorKeys[i] = key;
            }
            for (int i = 0; i < gw.alphaKeys.Length; i++) {
                GradientAlphaKey key;
                var p = p_alphas.GetArrayElementAtIndex(i);
                key.alpha = p.FindPropertyRelative(nameof(GradientAlphaKey.alpha)).floatValue;
                key.time = p.FindPropertyRelative(nameof(GradientAlphaKey.time)).floatValue;
                gw.alphaKeys[i] = key;
            }

            return gw.ToGradient();

        }
        void SetGradient(SerializedProperty property, Gradient gradient) {
            var p_colors = property.FindPropertyRelative(c_colorKeysName);
            var p_alphas = property.FindPropertyRelative(c_alphaKeysName);
            var p_mode = property.FindPropertyRelative(c_modeName);

            p_mode.intValue = (int)gradient.mode;

            p_colors.arraySize = gradient.colorKeys.Length;

            for(int i = 0; i < gradient.colorKeys.Length; i++) {
                var key = gradient.colorKeys[i];
                var p = p_colors.GetArrayElementAtIndex(i);
                var cc = p.FindPropertyRelative(nameof(GradientColorKey.color));
                cc.FindPropertyRelative("r").floatValue = key.color.r;
                cc.FindPropertyRelative("g").floatValue = key.color.g;
                cc.FindPropertyRelative("b").floatValue = key.color.b;
                cc.FindPropertyRelative("a").floatValue = key.color.a;
                p.FindPropertyRelative(nameof(GradientColorKey.time)).floatValue = key.time;
            }

            p_alphas.arraySize = gradient.alphaKeys.Length;
            for (int i = 0; i < gradient.alphaKeys.Length; i++) {
                var key = gradient.alphaKeys[i];
                var p = p_alphas.GetArrayElementAtIndex(i);
                p.FindPropertyRelative(nameof(GradientAlphaKey.alpha)).floatValue = key.alpha;
                p.FindPropertyRelative(nameof(GradientAlphaKey.time)).floatValue = key.time;
            }
        }
    }
}