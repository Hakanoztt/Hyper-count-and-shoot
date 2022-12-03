

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Mobge {
    public class DateTimePicker {
        // rates for: 0:year, 1:month, 2:day, 3:hour, 4:minute, 5:second
        private static float[] s_rates = new float[] { 2f, 1f, 1f, 1f, 1f, 1f };
        private static float s_spaceRate = 0.2f;
        private static float s_totalRate;

        static DateTimePicker() {
            s_totalRate = -s_spaceRate;
            for (int i = 0; i < s_rates.Length; i++) {
                s_totalRate += s_rates[i];
                s_totalRate += s_spaceRate;
            }
        }
        public static DateTime DateField(Rect position, string label, DateTime time) {
            return DateField(position, new GUIContent(label), time);
        }
        public static DateTime DateField(Rect position, GUIContent label, DateTime time) {


            var lw = EditorGUIUtility.labelWidth;
            RectangleIterator ri = new RectangleIterator(position);
            EditorGUI.LabelField(ri.NextRect(lw), label);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            float stepRate = (position.width - lw) / s_totalRate;
            int day = EditorGUI.IntField(ri.NextRect(stepRate * s_rates[2]), time.Day);
            EditorGUI.LabelField(ri.NextRect(s_spaceRate * stepRate), "/");
            int month = EditorGUI.IntField(ri.NextRect(stepRate * s_rates[1]), time.Month);
            EditorGUI.LabelField(ri.NextRect(s_spaceRate * stepRate), "/");
            int year = EditorGUI.IntField(ri.NextRect(stepRate * s_rates[0]), time.Year);
            ri.NextRect(stepRate * s_spaceRate);
            int hour = EditorGUI.IntField(ri.NextRect(stepRate * s_rates[3]), time.Hour);
            EditorGUI.LabelField(ri.NextRect(s_spaceRate * stepRate), ":");
            int minute = EditorGUI.IntField(ri.NextRect(stepRate * s_rates[4]), time.Minute);
            EditorGUI.LabelField(ri.NextRect(s_spaceRate * stepRate), ":");
            int second = EditorGUI.IntField(ri.NextRect(stepRate * s_rates[5]), time.Second);

            EditorGUI.indentLevel = indent;

            return new DateTime(year, month, day, hour, minute, second);
        }
        public static DateTime DateFieldLayout(string label, DateTime time) {
            return DateFieldLayout(new GUIContent(label), time);
        }
        public static DateTime DateFieldLayout(GUIContent label, DateTime time) {
            var w = GUILayoutUtility.GetLastRect().width;
            var r = GUILayoutUtility.GetRect(w, EditorGUIUtility.singleLineHeight);
            return DateField(r, label, time);
        }
        private struct RectangleIterator {
            private Rect _rect;

            public RectangleIterator(Rect rect) {
                _rect = rect;
            }
            public Rect NextRect(float width) {
                _rect.width = width;
                var r = _rect;
                _rect.x += width;
                return r;
            }
        }
    }
}