using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mobge.HyperCasualSetup {
    public class SearchPrefabWithMonoBehaviourWindow : EditorWindow {
        [MenuItem("Mobge/Search Prefabs With MonoBehaviour")]
        private static void Init() {
            GetWindow<SearchPrefabWithMonoBehaviourWindow>(nameof(SearchPrefabWithMonoBehaviourWindow));
        }
        private string _typeName;
        private List<Object> _results = new List<Object>();
        private Vector2 _scrollPosition;
        private void OnGUI() {
            _typeName = EditorGUILayout.TextField("Type Name", _typeName);
            if (GUILayout.Button("Search")) {
                var type = GetTypeFromName(_typeName);
                _results.Clear();
                var guids = AssetDatabase.FindAssets("t:Prefab");
                foreach (var guid in guids) {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go.GetComponent(type)) {
                        _results.Add(go);
                    }
                }
            }
            using (Scopes.ScrollView(ref _scrollPosition)) {
                foreach (var o in _results) {
                    EditorGUILayout.ObjectField(o, typeof(MonoBehaviour));
                }
            }
        }
        private Type GetTypeFromName(string name) {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (var assemblyDefinedType in assembly.DefinedTypes) {
                    if (typeof(MonoBehaviour).IsAssignableFrom(assemblyDefinedType)) {
                        if (assemblyDefinedType.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) {
                            return assemblyDefinedType;
                        }
                    }
                }
            }
            return default;
        }
    }
}