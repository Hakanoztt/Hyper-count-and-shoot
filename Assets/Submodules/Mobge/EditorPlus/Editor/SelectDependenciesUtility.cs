using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class SelectDependenciesUtility {
    [MenuItem("Assets/Select Dependencies For Export", true)]
    private static bool SelectDependenciesValidate() {
        return Selection.objects.Length > 0;
    }
    [MenuItem("Assets/Select Dependencies For Export")]
    private static void SelectDependencies() {
        var dependencies = new HashSet<string>();
        var selectedObjects = Selection.objects;
        foreach (var obj in selectedObjects) {
            var path = AssetDatabase.GetAssetPath(obj);
            RecursiveAddDependency(dependencies, path);
        }

        var list = new List<Object>();
        foreach (var o in dependencies.Select(AssetDatabase.LoadAllAssetsAtPath)) {
            list.AddRange(o);
        }
        Selection.objects = list.ToArray();
    }
    private static void RecursiveAddDependency(HashSet<string> set, string path) {
        if (path.EndsWith(".cs")) return;
        if (AssetDatabase.IsValidFolder(path)) {
            var assetGuids = AssetDatabase.FindAssets("", new []{path});
            foreach (var guid in assetGuids) {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                RecursiveAddDependency(set, assetPath);
            }
        }
        else {
            set.Add(path);
            var dependencies = AssetDatabase.GetDependencies(path);
            foreach (var dependency in dependencies) {
                if (!dependency.Equals(path)) {
                    RecursiveAddDependency(set, dependency);
                }
            }
        }
    }
}
