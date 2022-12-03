using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Mobge;

public abstract class EData<T> : Editor {

    private Data<T> data;

    private void OnEnable() {
        data = serializedObject.targetObject as Data<T>;
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        var enumerator = data.map.GetPairEnumerator();
        while(enumerator.MoveNext()) {
            var element = enumerator.Current;
            var index = element.Key;
            var pair = element.Value;

            EditorGUILayout.BeginHorizontal();

            pair.label = EditorGUILayout.TextField(pair.label);
            pair.value = DataEditor(pair.value);

            if (GUILayout.Button("-")) {
                data.map.RemoveElement(index);
                EditorGUILayout.EndHorizontal();
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
                serializedObject.ApplyModifiedProperties();
                /* we overstayed our welcome, let the next frame render it from scratch */
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (EditorGUI.EndChangeCheck()) {
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

        if (GUILayout.Button("+")) {
            data.map.AddElement(new Pair<T>() { label = "!!RENAME THIS!!" });
        }

        serializedObject.ApplyModifiedProperties();
    }

    public abstract T DataEditor(T val);
}

[CustomEditor(typeof(DataInt))]
public class EDataInt : EData<int> {
    public override int DataEditor(int val) => EditorGUILayout.IntField(val);
}

[CustomEditor(typeof(DataFloat))]
public class EDataFloat : EData<float> {
    public override float DataEditor(float val) => EditorGUILayout.FloatField(val);
}

[CustomEditor(typeof(DataString))]
public class EDataString : EData<string> {
    public override string DataEditor(string val) => EditorGUILayout.TextField(val);
}
