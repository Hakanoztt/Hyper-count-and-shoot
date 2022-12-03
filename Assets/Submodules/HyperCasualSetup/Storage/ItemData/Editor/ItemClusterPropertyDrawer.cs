using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace Mobge.HyperCasualSetup {
    [CustomPropertyDrawer(typeof(ItemCluster))]
    public class ItemClusterPropertyDrawer : PropertyDrawer {

        private LayoutRectSource _layout = new LayoutRectSource();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            OnGUIInternal(position, property, label);
        }
        private void OnGUIInternal(Rect position, SerializedProperty property, GUIContent label) {

            _layout.Reset(position);

            property.isExpanded = EditorGUI.Foldout(_layout.NextRect(), property.isExpanded, label, true);
            if (property.isExpanded) {
                var set = property.FindPropertyRelative(nameof(ItemCluster.set));
                EditorGUI.PropertyField(_layout.NextRect(), set, true);
                ItemSet setInstance = set.objectReferenceValue as ItemSet;
                if (setInstance != null && setInstance.items!=null) {
                    var items = property.FindPropertyRelative(ItemCluster.PropertyName_items);

                    _layout.NextSplitRect(_layout.Width - 60, out var r1, out var r2);
                    items.isExpanded = EditorGUI.Foldout(r1, items.isExpanded, items.displayName, true);
                    int size = EditorGUI.DelayedIntField(r2, items.arraySize);
                    items.arraySize = Mathf.Min(size, setInstance.items.Count);
                    if (items.isExpanded) {
                        for (int i = 0; i < items.arraySize; i++) {
                            var item = items.GetArrayElementAtIndex(i);
                            var count = item.FindPropertyRelative(nameof(ItemCluster.ItemContent.count));
                            var id = item.FindPropertyRelative(nameof(ItemCluster.ItemContent.id));


                            _layout.NextSplitRect(_layout.Width - 100, out r1, out r2);
                            id.intValue = EditorDrawer.Popup(r1, " ", setInstance.items, id.intValue);


                            var lw = EditorGUIUtility.labelWidth;
                            EditorGUIUtility.labelWidth = 40;
                            EditorGUI.PropertyField(r2, count, new GUIContent("x"));
                            EditorGUIUtility.labelWidth = lw;

                        }
                    }
                }
            }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            var type = Event.current.type;

            Event.current.type = EventType.Layout;

            OnGUIInternal(new Rect(0,0,100,100), property, label);
            Event.current.type = type;
            
            return _layout.Height;
        }
    }
}