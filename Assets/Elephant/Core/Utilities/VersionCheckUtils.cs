using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEngine;

namespace ElephantSDK
{
    // TODO implement external package version checks here
    public class VersionCheckUtils
    {
        private const string MediationAppLovinMax = "AppLovin MAX";
        private const string MediationIronSource = "IronSource";
        
        private static VersionCheckUtils _instance;
        public string AdSdkVersion = "";
        public string MediationVersion = "";
        public string UnityVersion = "";
        public string Mediation = "";

        public static VersionCheckUtils GetInstance()
        {
            if (_instance != null) return _instance;
            
            _instance = new VersionCheckUtils();
            
            if (!string.IsNullOrEmpty(GetMaxVersion()))
            {
                _instance.Mediation = "AppLovin MAX";
                _instance.MediationVersion = GetMaxVersion();
            }
            else
            {
                _instance.Mediation = "IronSource";
                _instance.MediationVersion = GetIsVersion();
            }
                
            _instance.UnityVersion = GetUnityVersion();
            _instance.AdSdkVersion = GetAdSdkVersion();

            return _instance;
        }

        private static string GetAdSdkVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var adsSdkVersion = "";

            try
            {
                var type = Array.Find(assembly.GetTypes(),
                    typeToFind =>
                        typeToFind.FullName != null
                        && typeToFind.FullName.Equals("RollicGames.Advertisements.Version"));

                if (type == null) return adsSdkVersion;
                var fieldInfo = type.GetField("SDK_VERSION",
                    BindingFlags.NonPublic | BindingFlags.Static);

                if (fieldInfo == null) return adsSdkVersion;
                adsSdkVersion = fieldInfo.GetValue(null).ToString();

                return adsSdkVersion;
            }
            catch (Exception e)
            {
                return adsSdkVersion;
            }
        }

        private static string GetMaxVersion()
        {
            var currentDomain = System.AppDomain.CurrentDomain;
            var maxVersion = "";
            
            foreach (var assembly in currentDomain.GetAssemblies())
            {
                try
                {
                    var type = Array.Find(assembly.GetTypes(),
                        typeToFind =>
                            typeToFind.FullName != null
                            && typeToFind.FullName.Equals("MaxSdk"));

                    if (type == null) continue;
                    
                    var fieldInfo = type.GetProperty("Version");

                    if (fieldInfo == null) return maxVersion;
                    maxVersion = fieldInfo.GetValue(null).ToString();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
            
            return maxVersion;
        }
        
        private static string GetIsVersion()
        {
            var currentDomain = System.AppDomain.CurrentDomain;
            var isVersion = "";
            
            foreach (var assembly in currentDomain.GetAssemblies())
            {
                try
                {
                    var type = Array.Find(assembly.GetTypes(),
                        typeToFind =>
                            typeToFind.FullName != null
                            && typeToFind.FullName.Equals("IronSource"));

                    if (type == null) continue;
                    
                    var method = type.GetMethod("pluginVersion");

                    if (method == null) return isVersion;
                    var result = method.Invoke(type, new object[] { });
                    isVersion = result.ToString();
                    isVersion = isVersion.Split('-')[0];
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
            
            return isVersion;
        }

        private static string GetUnityVersion() => Application.unityVersion;

        public int CompareVersions(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
        
            var versionA = VersionStringToInts(a);
            var versionB = VersionStringToInts(b);
            for (var i = 0; i < Mathf.Max(versionA.Length, versionB.Length); i++)
            {
                if (VersionPiece(versionA, i) < VersionPiece(versionB, i))
                    return -1;
                if (VersionPiece(versionA, i) > VersionPiece(versionB, i))
                    return 1;
            }

            return 0;
        }
        
        private int VersionPiece(IList<int> versionInts, int pieceIndex)
        {
            return pieceIndex < versionInts.Count ? versionInts[pieceIndex] : 0;
        }


        private int[] VersionStringToInts(string version)
        {
            int piece;
            if (version.Contains("_internal"))
            {
                version = version.Replace("_internal", string.Empty);
            }
            return version.Split('.')
                .Select(v => int.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out piece) ? piece : 0)
                .ToArray();
        }
    }
}