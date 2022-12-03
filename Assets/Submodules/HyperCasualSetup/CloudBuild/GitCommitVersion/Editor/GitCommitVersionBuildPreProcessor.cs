using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Mobge.Build {
    public class GitCommitVersionBuildPreProcessor : IPreprocessBuildWithReport {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) {
            GitCommitVersion.PrepareForBuild();
        }
    }
}

