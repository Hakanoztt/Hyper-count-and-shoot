using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Mobge.GitHooks {
    public static class GitHooksInstaller {
        private const string RememberKey = "git_hooks_installer_remembered_choice";
        private const string SourceHooksPath = "./Assets/Submodules/HyperCasualSetup/GitHooksSystem/GitHooks/";
        private const string DestinationHooksPath = ".git/hooks/";
        private const string DestinationModulesPath = ".git/modules/";
        [InitializeOnLoadMethod]
        private static void OnLoad() {
            if (RememberedChoice == RememberChoice.None && InstallNeeded) {
                GitHooksInstallWindow.OpenWindow();
            }
            if (RememberedChoice == RememberChoice.Install && InstallNeeded) {
                Install();
            }
        }
        internal enum RememberChoice {
            None = 0,
            DoNotInstall = 1,
            Install = 2,
        }
        internal static RememberChoice RememberedChoice {
            get => (RememberChoice) EditorPrefs.GetInt(RememberKey);
            set => EditorPrefs.SetInt(RememberKey, (int) value);
        }
        private static bool InstallNeeded {
            get {
                //if SourceHooksPath/version.txt is higher than .git/githooks/version.txt
                int newVersion;
                try {
                    newVersion = int.Parse(File.ReadAllText(Path.Combine(SourceHooksPath, "version.txt")));
                }
                catch (Exception) {
                    return false;
                }

                int currentVersion = 0;
                try {
                    currentVersion = int.Parse(File.ReadAllText(Path.Combine(DestinationHooksPath, "version.txt")));
                }
                catch (Exception) {
                    /* ignored */
                }

                return newVersion > currentVersion;
            }
        }
        internal static void Install() {
            var hooksDirectoriesList = FindHooksDirectories();
            foreach (var directory in hooksDirectoriesList) {
                //unset hooksPath variable on config
                UnsetGitHooksVariableOnGitHooksPath(directory);
                // Clear Hooks Path Directory
                DeleteDirectory(directory);
                // Copy Source Hooks Path to Hooks Path
                CopyDirectoryWithoutMeta(new DirectoryInfo(SourceHooksPath), directory);
                Debug.Log($"installed to {directory}");
            }
            Debug.Log("hooks installed");
        }
        public static void Remove() {
            var hooksDirectoriesList = FindHooksDirectories();
            foreach (var directory in hooksDirectoriesList) {
                // Clear Hooks Path Directory
                DeleteDirectory(directory);
                directory.Create();
                Debug.Log($"hooks removed from {directory}");
            }
            Debug.Log("hooks removed");
        }
        private static List<DirectoryInfo> FindHooksDirectories() {
            var hooksDirectoriesList = new List<DirectoryInfo> {new DirectoryInfo(DestinationHooksPath)};
            RecursivelyAddHookFolder(hooksDirectoriesList, new DirectoryInfo(DestinationModulesPath));
            return hooksDirectoriesList;
        }
        private static void UnsetGitHooksVariableOnGitHooksPath(DirectoryInfo directory) {
            var configFilePath = directory.Parent?.GetFiles("config")[0];
            if (configFilePath != null) {
                var configFileContent = File.ReadAllText(configFilePath.FullName);
                if (configFileContent.Contains("hooksPath =")) {
                    var configLines = File.ReadAllLines(configFilePath.FullName);
                    var newConfigLines = configLines.Where(l => !l.Contains("hooksPath ="));
                    File.WriteAllLines(configFilePath.FullName, newConfigLines);
                }
            }
        }
        private static void RecursivelyAddHookFolder(List<DirectoryInfo> list, DirectoryInfo path) {
            if (path.Name == "hooks") {
                list.Add(path);
                return;
            }
            foreach (var directory in path.GetDirectories()) {
                RecursivelyAddHookFolder(list, directory);
            }
        }
        private static void DeleteDirectory(DirectoryInfo directory) {
            try { directory.Delete(true); } catch (Exception) { /* ignored */ }
        }
        private static void CopyDirectoryWithoutMeta(DirectoryInfo source, DirectoryInfo target) {
            Directory.CreateDirectory(target.FullName);
            // Copy each file into the new directory.
            foreach (var fileInfo in source.GetFiles()) {
                if (!fileInfo.Name.EndsWith(".meta")) {
                    fileInfo.CopyTo(Path.Combine(target.FullName, fileInfo.Name), true);
                }
            }
            // Copy each subdirectory using recursion.
            foreach (var diSourceSubDir in source.GetDirectories()) {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyDirectoryWithoutMeta(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}


