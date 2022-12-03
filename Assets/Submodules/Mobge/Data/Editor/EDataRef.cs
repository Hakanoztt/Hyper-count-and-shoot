using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge {

    [CustomPropertyDrawer(typeof(DataRef))]
    public class EDataRef : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            Vector2 labelSize = GUI.skin.GetStyle("Label").CalcSize(label);
            Rect half = new Rect(position.x + labelSize.x, position.y, (position.width - labelSize.x)/ 2, position.height);
            Rect otherHalf = new Rect(half.x + half.width, half.y, half.width, half.height);
            GUI.Label(position, label);

            GUI.enabled = false;
            DataRef f = fieldInfo.GetValue(property.serializedObject.targetObject) as DataRef;
            if (f != null) {
                GUI.TextField(half, f.ToString());
            } else {
                GUI.TextField(half, "NULL DATA!!");
            }
            GUI.enabled = true;

            if (GUI.Button(otherHalf, "Select Data")) {
                DataRefSelectorWindow.Init(f);
            }
            EditorGUI.EndProperty();
        }
    }

    public class DataRefSelectorWindow : EditorWindow {

        public static void Init(DataRef dref) {
            EditorWindow window = GetWindow<DataRefSelectorWindow>("Data Ref Selector");
            (window as DataRefSelectorWindow).dref = dref;
        }

        private DataRef dref;
        private List<_Data> datas = new List<_Data>();
        private string searchKey = string.Empty;

        private void OnEnable() {
            datas.Clear();

            string s = "t:" + nameof(_Data);
            var assets = AssetDatabase.FindAssets(s);
            for (int i = 0; i < assets.Length; i++) {
                var item = AssetDatabase.LoadAssetAtPath<_Data>(AssetDatabase.GUIDToAssetPath(assets[i]));
                datas.Add(item);
            }
        }

        protected void OnGUI() {
            if (datas.Count == 0) return;

            searchKey = EditorGUILayout.TextField("SEARCH ---> ", searchKey);

            foreach (_Data data in datas) {
                var enumerator = data.GetEnumerator();
                while (enumerator.MoveNext()) {
                    int index = (int)enumerator.Current;

                    string text = data.name + "/" + data.GetNameOf(index);
                    if (!InspectorExtensions.TextMatchesSearch(text, searchKey)) { continue; }

                    if (GUILayout.Button(text)) {
                        dref._data = data;
                        dref._index = index;
                    }
                }
            }
        }
    }
}