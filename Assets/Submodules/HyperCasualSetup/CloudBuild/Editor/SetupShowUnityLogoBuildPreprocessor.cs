using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Mobge.Build {
    public class SetupShowUnityLogoBuildPreprocessor : IPreprocessBuildWithReport {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) {
            PlayerSettings.SplashScreen.showUnityLogo = false;
        }
    }
}
