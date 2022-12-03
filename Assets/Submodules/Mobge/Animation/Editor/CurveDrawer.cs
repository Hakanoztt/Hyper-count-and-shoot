using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mobge.Animation
{
    [CustomPropertyDrawer(typeof(Curve))]
    public class CurveDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var c = GetCurve(property);
            EditorGUI.BeginChangeCheck();
            var ac = EditorGUI.CurveField(position, label, c.ToAnimationCurve());
            if (EditorGUI.EndChangeCheck()) {
                c.UpdateKeys(ac);
                SetCurve(property, c);
            }
        }
        Curve GetCurve(SerializedProperty property) {
            var keys = property.FindPropertyRelative(Curve.c_keyArrayName);
            Keyframe[] ks = new Keyframe[keys.arraySize];
            for(int i = 0; i < keys.arraySize; i++) {
                var p = keys.GetArrayElementAtIndex(i);
                ks[i] = GetKeyFrame(p);
            }
            return new Curve(ks);
        }
        void SetCurve(SerializedProperty property, Curve curve) {
            var ks = curve.Keys;
            var keys = property.FindPropertyRelative(Curve.c_keyArrayName);
            keys.arraySize = ks.Length;
            for (int i = 0; i < ks.Length; i++) {
                SetKeyFrame(keys.GetArrayElementAtIndex(i), ks[i]);
            }
        }
        Keyframe GetKeyFrame(SerializedProperty property) {
            Keyframe kf;
            kf.time = property.FindPropertyRelative(nameof(Keyframe.time)).floatValue;
            kf.value = property.FindPropertyRelative(nameof(Keyframe.value)).floatValue;
            kf.inTangent = property.FindPropertyRelative(nameof(Keyframe.inTangent)).floatValue;
            kf.outTangent = property.FindPropertyRelative(nameof(Keyframe.outTangent)).floatValue;
            return kf;
        }
        void SetKeyFrame(SerializedProperty property, Keyframe kf) {
            property.FindPropertyRelative(nameof(Keyframe.time)).floatValue = kf.time;
            property.FindPropertyRelative(nameof(Keyframe.value)).floatValue = kf.value;
            property.FindPropertyRelative(nameof(Keyframe.inTangent)).floatValue = kf.inTangent;
            property.FindPropertyRelative(nameof(Keyframe.outTangent)).floatValue = kf.outTangent;
        }
    }
}