using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace SubmoduleManager {
    public static class SubmoduleManager {
        private const string SubmoduleFolder = "Assets/Submodules/";
        private static readonly Dictionary<Submodule, bool> SubmoduleExistsCache = new Dictionary<Submodule, bool>();
        [Serializable]
        public struct Submodule {
            public string url;
            public bool IsValid => !string.IsNullOrEmpty(url) && url.StartsWith("git@") && url.EndsWith(".git");
            public string Name {
                get {
                    if (!IsValid) return "is not valid";
                    var slashIndex = url.LastIndexOf("/", StringComparison.Ordinal);
                    var dotIndex = url.LastIndexOf(".", StringComparison.Ordinal);
                    return url.Substring(slashIndex + 1, dotIndex - slashIndex - 1);
                }
            }
        }
        public static bool GetIsSubmoduleAdded(Submodule submodule) {
            if (!submodule.IsValid) return false;
            if (SubmoduleExistsCache.ContainsKey(submodule)) 
                return SubmoduleExistsCache[submodule];
            var outputData = CommandRunner.RunCommand("git", $"submodule status {SubmoduleFolder}{submodule.Name}");
            var exists = outputData.ExitCode == 0;
            SubmoduleExistsCache.Add(submodule, exists);
            return exists;
        }
        public static void AddSubmodule(Submodule submodule) {
            CommandRunner.RunCommand("git",$"submodule add {submodule.url} {SubmoduleFolder}{submodule.Name}");
            SubmoduleExistsCache.Clear();
            TriggerRecompile();
        }
        public static void RemoveSubmodule(Submodule submodule) {
            CommandRunner.RunCommand("git", $"submodule deinit -f {SubmoduleFolder}{submodule.Name}");
            CommandRunner.RunCommand("git",$"rm -f {SubmoduleFolder}{submodule.Name}");
            ForceDeleteDirectory($".git/modules/{SubmoduleFolder}{submodule.Name}");
            SubmoduleExistsCache.Clear();
            TriggerRecompile();
        }
        private static void TriggerRecompile() {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        /// <summary>
        ///     THIS METHOD WILL PROBABLY DELETE MORE THAN WHAT YOU THINK IT WILL DELETE.
        ///     Be aware this function should NEVER be used with Junctions / Symbolic Links
        ///     Unless you know what you are doing. It will follow and will delete the originals also.
        ///     BE SUPER CAREFUL! YOU HAVE BEEN WARNED!
        /// </summary>
        /// <param name="targetDir"></param>
        private static void ForceDeleteDirectory(string targetDir) {
            var files = Directory.GetFiles(targetDir);
            var dirs = Directory.GetDirectories(targetDir);
            foreach (var file in files) {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            foreach (var dir in dirs) ForceDeleteDirectory(dir);
            Directory.Delete(targetDir, false);
        }
    }
}

