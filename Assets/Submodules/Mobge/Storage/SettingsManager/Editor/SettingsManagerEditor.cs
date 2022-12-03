using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mobge {
    public static class SettingsManagerEditor {
        private static readonly string SettingsEditorFolderPath = "Assets" + Path.DirectorySeparatorChar +
                                                                  "MobgeSettings" + Path.DirectorySeparatorChar +
                                                                  "Editor" + Path.DirectorySeparatorChar;

        private static readonly string SettingsRuntimeFolderPath = "Assets" + Path.DirectorySeparatorChar +
                                                                   "MobgeSettings" + Path.DirectorySeparatorChar +
                                                                   "Resources" + Path.DirectorySeparatorChar +
                                                                   "MobgeSettings" + Path.DirectorySeparatorChar;
        
        public static void SetValue<T>(string path, string key, T value, bool runtimeReadable = false, bool saveImmediately = true) {
            var folderPath = runtimeReadable
                ? Path.Combine(SettingsRuntimeFolderPath, path)
                : Path.Combine(SettingsEditorFolderPath, path);
            var filePath = Path.Combine(folderPath, key + ".asset");
            Directory.CreateDirectory(folderPath);
            if (!File.Exists(filePath)) {
                var holder = ScriptableObject.CreateInstance<SettingDataHolder>(); 
                holder.SetData(value);
                AssetDatabase.CreateAsset(holder, filePath);
            }
            else {
                var holder = AssetDatabase.LoadAssetAtPath<SettingDataHolder>(filePath);
                holder.SetData(value);
                EditorUtility.SetDirty(holder);
            }
            if (saveImmediately) AssetDatabase.SaveAssets();
        }
        public static bool TryGetValue<T>(string path, string key, out T value, bool runtimeReadable = false){
            var filePath = runtimeReadable
                ? Path.Combine(SettingsRuntimeFolderPath, path, key + ".asset")
                : Path.Combine(SettingsEditorFolderPath, path, key + ".asset");
            return TryGetValue(filePath, out value);
        }
        private static bool TryGetValue<T>(string filePath, out T value) {
            var holder = AssetDatabase.LoadAssetAtPath<SettingDataHolder>(filePath);
            if (holder == null) {
                value = default;
                return false;
            }
            var data = holder.GetObject<T>();
            value = data;
            return true;
        }
        public static void Delete(string path, string key, bool runtimeReadable = false) {
            var folderPath = runtimeReadable
                ? Path.Combine(SettingsRuntimeFolderPath, path)
                : Path.Combine(SettingsEditorFolderPath, path);
            var filePath = Path.Combine(folderPath, key + ".asset");
            if (File.Exists(filePath)) {
                AssetDatabase.DeleteAsset(filePath);
            }
        }
        public static void DeleteAll(string path, bool runtimeReadable = false) {
            var folderPath = runtimeReadable
                ? Path.Combine(SettingsRuntimeFolderPath, path)
                : Path.Combine(SettingsEditorFolderPath, path);
            if (!Directory.Exists(folderPath)) return;
            AssetDatabase.DeleteAsset(folderPath);
        }
        public static SettingEnumerator<T> Enumerate<T>(string path, bool runtimeReadable = false) {
            var folderPath = runtimeReadable
                ? Path.Combine(SettingsRuntimeFolderPath, path)
                : Path.Combine(SettingsEditorFolderPath, path);
            return new SettingEnumerator<T>(folderPath);
        }
        
        public class SettingEnumerator<T> : IEnumerator<T>, IEnumerable<T> {
            private readonly IEnumerator<string> _enumerator;
            public SettingEnumerator(string path) {
                _enumerator = Directory.EnumerateFiles(path).GetEnumerator();
            }
            public bool MoveNext() {
                bool notFinished = _enumerator.MoveNext();
                while (
                    //only skip if not finished
                    notFinished &&
                    //skip meta file
                    (_enumerator.Current != null && _enumerator.Current.EndsWith(".meta") ||
                     //skip can't get value
                     !TryGetValue<T>(_enumerator.Current, out _)) 
                ) {
                    notFinished = _enumerator.MoveNext();
                }
                return notFinished;
            }
            public void Reset() {
                _enumerator.Reset();
            }
            public T Current {
                get {
                    TryGetValue<T>(_enumerator.Current, out var val);
                    return val;
                }
            }
            object IEnumerator.Current => Current;
            public void Dispose() {
                _enumerator.Dispose();
            }
            public IEnumerator<T> GetEnumerator() {
                return this;
            }
            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
        }
    }
}




