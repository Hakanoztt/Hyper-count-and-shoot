#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace ElephantSDK
{
    public class PostProcess
    {
        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) {
            if (target == BuildTarget.iOS)
            {
                string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
                PBXProject project = new PBXProject();
                project.ReadFromString(File.ReadAllText(projectPath));
                
#if UNITY_2019_3_OR_NEWER
            string xcodeTarget = project.GetUnityFrameworkTargetGuid();
#else
                string xcodeTarget = project.TargetGuidByName("Unity-iPhone");
#endif
                project.AddFrameworkToProject(xcodeTarget, "StoreKit.framework", true);
                project.AddFrameworkToProject(xcodeTarget, "AppTrackingTransparency.framework", true);

                EditInfoPlist(pathToBuiltProject);

                // Write.
                File.WriteAllText(projectPath, project.WriteToString());
            }
        }
        
        private static void EditInfoPlist(string pathToBuiltProject)
        {
            string plistPath = pathToBuiltProject + "/Info.plist";
            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));
            
            PlistElementDict rootDict = plist.root;

            // ATT
            rootDict.SetString("NSUserTrackingUsageDescription", "Your data will only be used to deliver personalized ads to you.");
            
            // Attribution report endpoint
            rootDict.SetString("NSAdvertisingAttributionReportEndpoint", "https://newapi.rollic.gs");

            
            File.WriteAllText(plistPath, plist.WriteToString());
        }
    }
}
#endif