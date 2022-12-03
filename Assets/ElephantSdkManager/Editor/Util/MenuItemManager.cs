using System.IO;
using System.Reflection;
using UnityEngine;

namespace ElephantSdkManager.Util
{
    public class MenuItemManager
    {
        [UnityEditor.Callbacks.DidReloadScripts]
        public static void OnReloadScripts()
        {
            Assembly assemblyForAds = Assembly.GetExecutingAssembly();
            foreach (var type in assemblyForAds.GetTypes())
            {
                if (type.FullName == null) return;
                if (type.FullName.Equals("MoPubMenu"))
                {
                    RemoveMoPubSdkManager();
                }
            }
        }

        private static void RemoveMoPubSdkManager()
        {
            string mopubMenuPath = Application.dataPath + "/Mopub/Scripts/Editor/MopubMenu.cs";

            string[] lines = File.ReadAllLines(mopubMenuPath);
            File.Delete(mopubMenuPath);
            using (StreamWriter sw = File.AppendText(mopubMenuPath))
            {
                foreach (string line in lines)
                {
                    if (!line.Contains("Manage SDKs"))
                    {
                        sw.WriteLine(line);
                    }
                }
            }
        }
    }
}