using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Mobge
{
    public class FolderDuplicateUtility : MonoBehaviour
    {
        private static HashSet<System.Type> s_ignoredTypes;
        static FolderDuplicateUtility() {
            s_ignoredTypes = new HashSet<System.Type>();
            s_ignoredTypes.Add(typeof(MonoScript));
            s_ignoredTypes.Add(typeof(Shader));
            s_ignoredTypes.Add(typeof(Texture));
            s_ignoredTypes.Add(typeof(Texture2D));
            s_ignoredTypes.Add(typeof(Texture3D));
        }
        private static string SelectedFolderName {
            get {
                var obj = Selection.activeObject;
                var path = AssetDatabase.GetAssetPath(obj);
                return path;
            }
        }
        [MenuItem("Assets/Duplicate folder with references", true)]
        private static bool DuplicateFolderValidate() {
            //Debug.Log(EditorWindow.focusedWindow);
            var path = SelectedFolderName;
            if (!string.IsNullOrEmpty(path)) {
                //path = System.IO.Path.Combine(Application.dataPath, path);
                
                return System.IO.Directory.Exists(path);
            }
            return false;
        }
        [MenuItem("Assets/Duplicate folder with references")]
        private static void DuplicateFolder() {
            var originalPath = SelectedFolderName;
            var targetPath = AssetDatabase.GenerateUniqueAssetPath(originalPath);

            DuplicateFolder(originalPath, targetPath);
        }
        public static void DuplicateFolder(string sourceFolder, string newFolder) {
            List<ItemPair> items = new List<ItemPair>();
            DuplicateFolder(items, sourceFolder, newFolder);
            Dictionary<UnityEngine.Object, UnityEngine.Object> oldToNewReferences = PrepareOldToNewReferences(items);

            for (int i = 0; i < items.Count; i++) {
                //Debug.Log(items[i].sourceObject, items[i].targetObject);
                //Debug.Log(items[i].sourceObject.name + " ------- " + items[i].targetObject.name + " ------ " + (items[i].sourceObject == items[i].targetObject));
                var e = GetObjectProperties(items[i].targetObject, out _);
                while (e.MoveNext()) {
                    var p = e.Current;
                    if (p.objectReferenceValue) {
                        if(oldToNewReferences.TryGetValue(p.objectReferenceValue, out UnityEngine.Object target)) {
                            p.objectReferenceValue = target;
                        }
                    }
                }
            }
            AssetDatabase.SaveAssets();
        }
        private static Dictionary<UnityEngine.Object, UnityEngine.Object> PrepareOldToNewReferences(List<ItemPair> items) {
            Dictionary<UnityEngine.Object, UnityEngine.Object> references = new Dictionary<Object, Object>();
            for(int i = 0; i < items.Count; i++) {
                var es = GetObjectProperties(items[i].sourceObject, out List<UnityEngine.Object> sourceInternals, false, true);
                var et = GetObjectProperties(items[i].targetObject, out List<UnityEngine.Object> targetInternals, false, true);
                while (es.MoveNext()) ;
                while (et.MoveNext()) ;
                for(int j = 0; j < sourceInternals.Count; j++) {
                    references[sourceInternals[j]] = targetInternals[j];
                }
            }
            return references;
        }
        public static IEnumerator<SerializedProperty> GetObjectProperties(UnityEngine.Object obj, out List<UnityEngine.Object> internals, bool enumerateNullReferences = false, bool @readonly = false) {
            internals = new List<UnityEngine.Object>();
            return GetObjectPropertiesRecursiveInternal(internals, obj, obj, enumerateNullReferences, @readonly);
            
        }
        private static IEnumerator<SerializedProperty> GetObjectPropertiesRecursiveInternal(List<UnityEngine.Object> roots, UnityEngine.Object rootObj, UnityEngine.Object localRoot, bool enumerateNullReferences, bool @readonly) {
            if (roots.Contains(localRoot)) {
                yield break;
            }
            roots.Add(localRoot);
            SerializedObject so = new SerializedObject(localRoot);
            var p = so.GetIterator();
            var rootPath = AssetDatabase.GetAssetPath(rootObj);
            do {
                if (p.propertyType == SerializedPropertyType.ObjectReference) {
                    if (p.objectReferenceValue != null) {
                        if (rootPath == AssetDatabase.GetAssetPath(p.objectReferenceValue)) {
                            var e = GetObjectPropertiesRecursiveInternal(roots, rootObj, p.objectReferenceValue, enumerateNullReferences, @readonly);
                            while (e.MoveNext()) {
                                yield return e.Current;
                            }
                        }
                        else {
                            yield return p;
                        }
                    }
                    else {
                        if (enumerateNullReferences) {
                            yield return p;
                        }
                    }
                }
            }
            while (p.Next(true));
            //if (so.hasModifiedProperties) {
            //    Debug.Log(so.targetObject + " : " + so.hasModifiedProperties);
            //}
            if (!@readonly) {
                so.ApplyModifiedProperties();
                EditorExtensions.SetDirty(localRoot);
            }
            so.Dispose();
        }
        private static void DuplicateFolder(List<ItemPair> items, string rootFolder, string targetRoot, string targetRelative = "") {
            var files = Directory.GetFiles(rootFolder);
            for (int i = 0; i < files.Length; i++) {
                var f = files[i];
                ItemPair ip;
                ip.sourceObject = AssetDatabase.LoadAssetAtPath(f, typeof(UnityEngine.Object));
                if (ip.sourceObject && !s_ignoredTypes.Contains(ip.sourceObject.GetType())) {
                    var fName = Path.GetFileName(f);
                    var relativePath = targetRelative + Path.DirectorySeparatorChar + fName;
                    var targetPath = targetRoot + relativePath;
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    AssetDatabase.CopyAsset(f, targetPath);
                    //if (ip.targetObject is GameObject go) {
                    //    PrefabUtility.SaveAsPrefabAsset(go, targetPath);
                    //    go.DestroySelf();
                    //}
                    //else {
                    //    AssetDatabase.CreateAsset(ip.targetObject, targetPath);
                    //}
                    ip.targetObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetPath);
                    if (ip.targetObject) {
                        items.Add(ip);
                    }

                }
            }
            var dirs = Directory.GetDirectories(rootFolder);
            for (int i = 0; i < dirs.Length; i++) {
                var d = dirs[i];
                DuplicateFolder(items, d, targetRoot, targetRelative + Path.DirectorySeparatorChar + Path.GetFileName(d));
            }
        }

        private struct ItemPair
        {
            public Object sourceObject;
            public Object targetObject;



        }
    }
}