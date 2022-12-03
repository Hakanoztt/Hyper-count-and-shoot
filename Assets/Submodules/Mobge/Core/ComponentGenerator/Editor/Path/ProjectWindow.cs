using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.ComponentGenerator {
    public static class ProjectWindow {
        /// <summary>
        /// If there is no valid folder in project explorer window [Unity Project Hierarchy], returns /Assets
        /// </summary>
        /// <returns> Current path on project window</returns>
        public static string GetSelectedPathOrFallback() {
            var path = "Assets";

            // not perfect but good enough
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets)) {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }

            return path;
        }
    }
}