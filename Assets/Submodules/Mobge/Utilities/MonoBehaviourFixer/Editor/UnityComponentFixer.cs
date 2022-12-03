using System;
using UnityEditor;
using UnityEngine;

namespace Mobge
{
    public class UnityComponentFixer
    {
        public delegate bool Fixer<T>(GameObject root, T monoBehaviour);

        public static void Fix<T>(Fixer<T> fixerFunction)
        {
            var prefabGuids = AssetDatabase.FindAssets("t:prefab");
        
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var monoBehaviours = root.GetComponentsInChildren<T>();
                foreach (var mb in monoBehaviours)
                {
                    var save = fixerFunction(root, mb);
                    if (save) {
                        EditorExtensions.SetDirty(root);
                    }
                }
            }
            AssetDatabase.SaveAssets();
        }
        public static void Fix(Type type, Fixer<UnityEngine.Component> fixerFunction) {
            var prefabGuids = AssetDatabase.FindAssets("t:prefab");

            foreach (var guid in prefabGuids) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var monoBehaviours = root.GetComponentsInChildren(type);
                foreach (var mb in monoBehaviours) {
                    var save = fixerFunction(root, mb);
                    if (save) {
                        EditorExtensions.SetDirty(root);
                    }
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}

