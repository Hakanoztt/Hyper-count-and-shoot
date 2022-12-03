using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ElephantSdkManager.Model;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace ElephantSdkManager
{
    public class NetworkControlManager : EditorWindow
    {
        private static List<Sdk> _sdkList;

        public static string FetchManifestStatus()
        {
            var elephantSettingsPage = Application.dataPath + "/Resources/ElephantSettings.asset";
            var gameId = "";
            if (File.Exists(elephantSettingsPage))
            {
                var lines = File.ReadAllLines(elephantSettingsPage);
                foreach (var line in lines)
                {
                    if (line.Contains("GameID"))
                    {
                        gameId = line.Replace("  GameID: ", "");
                    }
                }
            }

            var request = UnityWebRequest.Get(ManifestSource.ManifestURL + gameId +  "&version=" + ElephantSdkManagerVersion.SDK_VERSION);
            request.SendWebRequest();
            while (!request.isDone && !request.isHttpError && !request.isNetworkError)
            {
                // no-op
            }

            if (request.isHttpError || request.isNetworkError || !string.IsNullOrWhiteSpace(request.error))
            {
                Debug.LogError("Couldn't finish opening request!");
                return "";
            }

            var responseJson = request.downloadHandler.text;
            var manifest = JsonUtility.FromJson<Manifest>(responseJson);
            if (manifest != null)
            {
                _sdkList = manifest.sdkList;
            }

            request.Dispose();

            return CheckVersions();
        }

        private static string CheckVersions()
        {
            var myAssembly = Assembly.GetExecutingAssembly();
            var types = myAssembly.GetTypes();
            var packageConfigTypes =
                types.Where(packageConfigType => packageConfigType.Name.Contains("PackageConfig")).ToList();

            var networkList = new List<Sdk>();
            if (packageConfigTypes.Count > 0)
            {
                foreach (var packageConfigType in packageConfigTypes)
                {
                    var version = "";
                    var name = "";
                    var versionMethodInfo = packageConfigType.GetMethod("get_Version");
                    if (versionMethodInfo != null)
                    {
                        var classInstance = Activator.CreateInstance(packageConfigType, null);
                        version = (string) versionMethodInfo.Invoke(classInstance, null);
                    }

                    var nameMethodInfo = packageConfigType.GetMethod("get_Name");
                    if (nameMethodInfo != null)
                    {
                        var classInstance = Activator.CreateInstance(packageConfigType, null);
                        name = (string) nameMethodInfo.Invoke(classInstance, null);
                    }

                    var networkSdk = _sdkList.Find(sdk => sdk.sdkName.Equals(name));
                    if (networkSdk != null)
                    {
                        networkSdk.currentVersion = version;
                        networkList.Add(networkSdk);
                    }
                }
            }

            var remoteNetworkList = _sdkList.Where(sdk => sdk.type.Equals("network"));
            var warningString = remoteNetworkList.Aggregate("",
                (current1, remoteSDK) =>
                    (from localSDK in networkList
                        where localSDK.sdkName.Equals(remoteSDK.sdkName)
                        where !string.IsNullOrEmpty(localSDK.currentVersion)
                        let remoteSdkVersion = remoteSDK.version.Replace("v", string.Empty)
                        where !localSDK.currentVersion.Equals(remoteSdkVersion)
                        select localSDK).Aggregate(current1,
                        (current, localSDK) =>
                            current + remoteSDK.sdkName + " to version " + remoteSDK.version + "\n"));

            return warningString;
        }
    }

    [InitializeOnLoad]
    public class CodeControlOnBuild
    {
        private static readonly string ErrorText;

        static CodeControlOnBuild()
        {
            ErrorText = NetworkControlManager.FetchManifestStatus();
            if (!string.IsNullOrEmpty(ErrorText))
            {
                BuildPlayerWindow.RegisterBuildPlayerHandler(ThrowException);
            }
        }

        static void ThrowException(BuildPlayerOptions obj)
        {
            EditorUtility.DisplayDialog("Error", "You need to update following SDKs: \n" + ErrorText, "OK");
            throw new BuildPlayerWindow.BuildMethodException(ErrorText);
        }
    }
}