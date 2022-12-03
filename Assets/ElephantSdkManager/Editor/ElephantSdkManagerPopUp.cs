using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ElephantSdkManager.Model;
using ElephantSdkManager.Util;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace ElephantSdkManager
{

    [InitializeOnLoad]
    public class ElephantSdkManagerPopUp 
    {
        private const string KeyVersion = "key_version";
        private const string Warning = "Please update your SDKs from [Elephant -> Manage Core SDKs]";
        
        private static List<Sdk> _sdkList;

        static ElephantSdkManagerPopUp()
        {
            FetchManifestStatus();
        }

        private static void FetchManifestStatus()
        {
            string elephantSettingsPage = Application.dataPath + "/Resources/ElephantSettings.asset";
            string gameId = "";
            if (File.Exists(elephantSettingsPage))
            {
                string[] lines = File.ReadAllLines(elephantSettingsPage);
                foreach (var line in lines)
                {
                    if (line.Contains("GameID"))
                    {
                        gameId = line.Replace("  GameID: ", "");
                    }
                }
            }
            
            var request = UnityWebRequest.Get(ManifestSource.ManifestURL  + gameId +  "&version=" + ElephantSdkManagerVersion.SDK_VERSION);
            request.SendWebRequest();
            while (!request.isDone && !request.isHttpError && !request.isNetworkError)
            {
                // no-op
            }

            if (request.isHttpError || request.isNetworkError || !string.IsNullOrWhiteSpace(request.error))
            {
                Debug.LogError("Couldn't finish opening request!");
                return;
            }

            var responseJson = request.downloadHandler.text;
            var manifest = JsonUtility.FromJson<Manifest>(responseJson);
            if (manifest != null)
            {
                _sdkList = manifest.sdkList;
            }

            request.Dispose();
            
            CheckVersions();
            CheckIfUpdateAvailable();
            
        }

        public static void CheckIfUpdateAvailable()
        {
            foreach (var sdk in _sdkList)
            {
                SetUpdateAvailable(sdk);
            }
            
            var sdksNeedUpdate = _sdkList.FindAll(sdk => sdk.isUpdateAvailable);
            if (sdksNeedUpdate.Count <= 0)
            {
                return;
            }

            var sdksNeedUpdateTitles = "";
            foreach (var sdk in sdksNeedUpdate)
            {
                sdksNeedUpdateTitles = sdksNeedUpdateTitles + sdk.sdkName + " version " + sdk.version + " available\n";
                SetShown(sdk);
            }
            
            var result = EditorUtility.DisplayDialog("Updates Available", Warning + "\n" + sdksNeedUpdateTitles, "OK");
     
        }
        
        private static void CheckVersions()
        {
            if (_sdkList == null || _sdkList.Count == 0)
            {
                return;
            }
            
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            foreach (var type in myAssembly.GetTypes())
            {
                if (type.FullName == null) return;

                if (type.FullName.Equals("ElephantSDK.ElephantVersion"))
                {
                    FieldInfo fieldInfo = type.GetField("SDK_VERSION",
                        BindingFlags.NonPublic | BindingFlags.Static);
                    Sdk elephantSdk = _sdkList.Find(sdk => sdk.sdkName.Equals("Elephant"));
                    if (elephantSdk != null)
                    {
                        if (!(fieldInfo is null)) elephantSdk.currentVersion = fieldInfo.GetValue(null).ToString();
                    }
                }

                if (type.FullName.Equals("RollicGames.Advertisements.AdsSdkVersion"))
                {
                    var fieldInfo = type.GetField("SDK_VERSION",
                        BindingFlags.NonPublic | BindingFlags.Static);
                    var adsSDK = _sdkList.Find(sdk => sdk.sdkName.Equals("Rollic Ads"));
                    if (adsSDK != null)
                    {
                        if (!(fieldInfo is null)) adsSDK.currentVersion = fieldInfo.GetValue(null).ToString();
                    } 
                }
                
                var mopubSdk = _sdkList.Find(sdk => sdk.sdkName.Equals("MoPub"));
                if (mopubSdk != null)
                {
                    string mopubPath = Application.dataPath + "/Mopub/Scripts/Mopub.cs";
                    if (File.Exists(mopubPath))
                    {
                        string[] lines = File.ReadAllLines(mopubPath);
                        foreach (var line in lines)
                        {
                            if (line.Contains("public const string MoPubSdkVersion"))
                            {
                                Regex regex = new Regex("\"(.*?)\"");
    
                                var matches = regex.Matches(line);
                                
                                if (matches.Count > 0)
                                {
                                    var mopubVersion = matches[0].Value;
                                    mopubVersion = mopubVersion.Replace("\"", "");
                                    mopubSdk.currentVersion = mopubVersion;
                                }
                            }
                        }
                    }
                }
            }
            
            var types = myAssembly.GetTypes();
            var packageConfigTypes = 
                types.Where(packageConfigType => packageConfigType.Name.Contains("PackageConfig")).ToList();
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
                    }
                }
            }

        }


        private static void SetUpdateAvailable(Sdk sdk)
        {
            if (sdk == null)
            {
                return;
            }
            
            var currentVersion = sdk.currentVersion;
            var latestVersion = sdk.version;
            if (currentVersion == null)
            { 
                
                sdk.isUpdateAvailable = !IsShown(sdk);
            }
            else
            {
                var isUpdateOrInstallAvailable = 
                    !string.IsNullOrEmpty(latestVersion) &&
                    !string.IsNullOrEmpty(currentVersion) &&
                    VersionUtils.CompareVersions(currentVersion?.Replace("v", string.Empty), 
                        latestVersion?.Replace("v", string.Empty)) != 0 &&
                    !IsShown(sdk);
            
                sdk.isUpdateAvailable = isUpdateOrInstallAvailable;
            }
        }
     


        private static void SetShown(Sdk sdk)
        {
            var now = DateTime.Now;
            EditorPrefs.SetString(KeyVersion + sdk.sdkName, sdk.version + "-" + now);
        }
        
        private static bool IsShown(Sdk sdk)
        {
            var savedSdk = EditorPrefs.GetString(KeyVersion + sdk.sdkName, "");
            var savedSdkSplit = savedSdk.Split('-');
            if (savedSdkSplit.Length > 1)
            {
                var sdkVersion = savedSdkSplit[0];
                var timeStampForSavedSdk = savedSdkSplit[1];
                var timeStampForSavedSdkDate = DateTime.Parse(timeStampForSavedSdk);
                var hoursDiff = (DateTime.Now - timeStampForSavedSdkDate).TotalHours;
                if (hoursDiff >= 8)
                {
                    if (sdk.type.Equals("network"))
                    {
                        return sdk.version.Equals(sdkVersion);
                    }

                    EditorPrefs.DeleteKey(KeyVersion + sdk.sdkName);
                    return false;
                }

                return sdk.version.Equals(sdkVersion);

            }
            
            return sdk.version.Equals(EditorPrefs.GetString(KeyVersion + sdk.sdkName, ""));
        }
    }
}