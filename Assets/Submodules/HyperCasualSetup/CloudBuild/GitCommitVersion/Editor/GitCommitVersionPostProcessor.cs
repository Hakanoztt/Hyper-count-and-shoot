using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Mobge.Build {
    public class GitCommitVersionPostProcessor : IPostprocessBuildWithReport {
        public int callbackOrder => 0;
        public void OnPostprocessBuild(BuildReport report) {
            GitCommitVersion.CleanupAfterBuild();
        }
    }
}
