using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Mobge {
    [CustomPropertyDrawer(typeof(ElementReferenceAttribute))]
    public class EElementType : EElementReference {

    }
    [CustomPropertyDrawer(typeof(ElementReference))]
    public class EElementReference : PropertyDrawer {
        public static ElementReference LayoutField(string label, ElementReference r, ElementEditor editor, Type typeFilter = null) {

            r.id = EditorLayoutDrawer.Popup(label, EElementReference.ElementsWithId(editor, typeFilter), r.id, "none");
            return r;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var c = ElementEditor.CurrentEditor;
            if (c == null) {
                EditorGUI.PropertyField(position, property);
            }
            else {

                var att = (ElementReferenceAttribute)attribute;
                var p = property.FindPropertyRelative(nameof(ElementReference.id));
                p.intValue = EditorDrawer.Popup(position, label.text, ElementsWithId(c, att == null ? null : att.elementType), p.intValue, "none");
            }
        }
        public static IEnumerable<EditorDrawer.Pair> ElementsWithId(ElementEditor editor, Type typeFilter) {
            var e = editor.AllElements;
            while (e.MoveNext()) {
                var c = e.Current;
                if (c.Id >= 0) {
                    if (typeFilter == null || typeFilter.IsAssignableFrom(c.DataObject.GetType())) {

                        yield return new BaseEditorDrawer.Pair(c.Id, c.Id + ": " + c.DataObject.ToString());
                    }
                }
            }
        }
    }
}