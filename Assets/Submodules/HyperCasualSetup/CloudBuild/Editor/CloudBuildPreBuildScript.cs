using Mobge.HyperCasualSetup;

namespace Mobge.Build {
    public static class CloudBuild {
        public static void OnBeforeBuild() {
            LevelSetAddressablePreBuildProcessor.PreExport();            
        }
    }
}