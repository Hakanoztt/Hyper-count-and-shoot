using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace Mobge.Build {
	public class VersionSettingBuildPreProcessor : IPreprocessBuildWithReport {
		public int callbackOrder => 0;
		public void OnPreprocessBuild(BuildReport report) {
// 			VersionSetting.GetCurrentSetting((b, setting) => {
// 				var version = setting.FullVersionString;
// 				Debug.Log($"{nameof(VersionSettingBuildPreProcessor)} is setting up version code to {version}");
// 				PlayerSettings.bundleVersion = version;
// 				AssetDatabase.SaveAssets();
// 				// writes current version string to a temporary file for cloud build post process script to read
// #if UNITY_CLOUD_BUILD
// 				Debug.Log($"{nameof(VersionSettingBuildPreProcessor)} setting up readable version for script");
// 				
// 				var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Temp", "MobgeVersion");
// 				Directory.CreateDirectory(outputPath);
// 				
// 				var basicVersion = setting.BasicVersionString;
// 				var basicVersionPath = Path.Combine(outputPath, "mobge_basic_version.txt");
// 				var fileStream = File.Create(basicVersionPath);
// 				var info = new UTF8Encoding(true).GetBytes(basicVersion);
// 				fileStream.Write(info, 0, info.Length);
// 				fileStream.Close();
//
// 				var fullVersion = setting.FullVersionString;
// 				var fullVersionPath = Path.Combine(outputPath, "mobge_full_version.txt");
// 				fileStream = File.Create(fullVersionPath);
// 				info = new UTF8Encoding(true).GetBytes(fullVersion);
// 				fileStream.Write(info, 0, info.Length);
// 				fileStream.Close();
// #endif
// 			});
		}
	}
}
