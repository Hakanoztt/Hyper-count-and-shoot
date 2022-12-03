using System.IO;
using UnityEditor;
using UnityEngine;

namespace SubmoduleManager {
    public static class ImportExporter {
        public static T Import<T>() {
            var path = EditorUtility.OpenFilePanelWithFilters(
                "Import repo settings from json.",
                "",
                new[] {
                    "Json file",
                    "json"
                });
            if (!string.IsNullOrEmpty(path)) {
                if (File.Exists(path)) {
                    var json = File.ReadAllText(path);
                    var data = JsonUtility.FromJson<T>(json);
                    return data;
                }
                else
                    Debug.LogError("Settings file cannot be found.");
            }
            else {
                Debug.Log("Import canceled.");
            }
            return default;
        }
        public static void Export<T>(T data) {
            var json = JsonUtility.ToJson(data, true);
            if (json != null) {
                var path = EditorUtility.SaveFilePanel(
                    "Export repo settings as json.",
                    "",
                    "SubmoduleManagerConfig",
                    "json");
                if (!string.IsNullOrEmpty(path)) {
                    File.WriteAllText(path, json);
                }
                else {
                    Debug.Log("Export canceled.");
                }
            }
            else {
                Debug.LogError("Settings cannot be found.");
            }
        }
    }
}