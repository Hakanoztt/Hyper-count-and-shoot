#if UNITY_EDITOR
    using System;
    using System.Diagnostics;
    using System.IO;
    using UnityEditor;
#endif
using UnityEngine;

namespace Mobge.Build {
    public static class GitCommitVersion {
        public static string CommitVersion {
            get {
                #if UNITY_EDITOR
                    return GetCommitVersionFromDevice();
                #else
                    var versionData = Resources.Load<GitCommitVersionData>("GitCommitVersionData");
                    return versionData == null ? "commit version cannot be found" : versionData.commitVersion;
                #endif
            }
        }
        public static string CommitCount {
            get {
                #if UNITY_EDITOR
                    return GetCommitCountFromDevice();
                #else
                    return "0";
                #endif
            }
        }
        #if UNITY_EDITOR
            private static string GetCommitVersionFromDevice() {
                return TryRunCommandWithOutput("git", "rev-parse HEAD", "commit version cannot be found");
            }
            private static string GetCommitCountFromDevice() {
                return TryRunCommandWithOutput("git", "rev-list HEAD --count", "0");
            }
            public static void PrepareForBuild() {
                const string path = "Assets/Resources";
                if (!AssetDatabase.IsValidFolder(path)) {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                const string dataObjectName = "GitCommitVersionData";
                string assetPathAndName = $"{path}/{dataObjectName}.asset";
                AssetDatabase.DeleteAsset(assetPathAndName);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var commitVersionData = ScriptableObject.CreateInstance<GitCommitVersionData>();
                commitVersionData.commitVersion = GitCommitVersion.GetCommitVersionFromDevice();
                AssetDatabase.CreateAsset(commitVersionData, assetPathAndName);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            public static void CleanupAfterBuild() {
                var path = "Assets/Resources/GitCommitVersionData.asset";
                if (File.Exists(path)) {
                    AssetDatabase.DeleteAsset(path);
                }
                var directory = new DirectoryInfo("Assets/Resources");
                if (directory.GetFiles().Length == 0 && directory.GetDirectories().Length == 0) {
                    FileUtil.DeleteFileOrDirectory(directory.FullName);
                    FileUtil.DeleteFileOrDirectory(directory.FullName+".meta");
                }
            }
            private static string TryRunCommandWithOutput(string filename, string arguments, string defaultOutput = "") {
                try {
                    var process = new Process {
                        StartInfo = {
                            FileName = filename,
                            Arguments = arguments,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return process.ExitCode != 0 ? defaultOutput : output;
                }
                catch (Exception) {
                    return defaultOutput;
                }
            }
        #endif
    }
}

