using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mobge.HyperCasualSetup {
    [CustomPropertyDrawer(typeof(ALevelSet.ID))]
    public class LevelIDDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var lw = EditorGUIUtility.labelWidth;
            var lr = position;
            lr.width = lw;
            EditorGUI.LabelField(lr, label);
            position.x += lw;
            position.width -= lw;
            position.width *= 0.25f;
            var p = property.FindPropertyRelative(ALevelSet.ID.c_valueFieldName);
            var id = new ALevelSet.ID(p.intValue);
            var il = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            id[0] = EditorGUI.IntField(position, id[0]);
            position.x += position.width;
            id[1] = EditorGUI.IntField(position, id[1]);
            position.x += position.width;
            id[2] = EditorGUI.IntField(position, id[2]);
            position.x += position.width;
            id[3] = EditorGUI.IntField(position, id[3]);
            EditorGUI.indentLevel = il;
            p.intValue = id.Value;

        }
    }
}