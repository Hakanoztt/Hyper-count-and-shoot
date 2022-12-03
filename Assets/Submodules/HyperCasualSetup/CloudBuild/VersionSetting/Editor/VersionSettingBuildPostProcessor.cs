using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Mobge.Build {
    public class VersionSettingBuildPostProcessor : IPostprocessBuildWithReport {
        public int callbackOrder => 0;
        public void OnPostprocessBuild(BuildReport report) {
            // if (report.summary.result != BuildResult.Failed && report.summary.result != BuildResult.Cancelled) {
            //     VersionSetting.GetNewSetting((b, setting) => {
            //         PlayerSettings.bundleVersion = setting.FullVersionString;
            //         AssetDatabase.SaveAssets();
            //     });
            // }
        }
    }
}

