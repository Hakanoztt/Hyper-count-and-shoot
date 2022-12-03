using System.IO;
using UnityEditor;
using UnityEngine.Networking;

namespace ElephantSdkManager
{
    public class RegionSwitch
    {
        private const string AssetsPathPrefix = "Assets/";
        private const string DownloadDirectory = AssetsPathPrefix + "ElephantSdkManager";
        public const string KeyIsRollicAdsChinaInstalled = AssetsPathPrefix + "isRollicAdsChinaInstalled";

        // TODO create urls dynamically!!
        private const string ChinaUrl = "https://elephant-sdks.s3.amazonaws.com/china/RollicGamesMopubChina.unitypackage";
        private const string WwUrl = "https://elephant-sdks.s3.amazonaws.com/RollicGamesMopub.unitypackage";
        
        public static void SwitchRegions(bool isChina)
        {
            var path = Path.Combine(DownloadDirectory, "RollicGames");

            UnityWebRequest downloader;
            if (isChina)
            {
                downloader =  UnityWebRequest.Get(ChinaUrl);
                downloader.downloadHandler = new DownloadHandlerFile(path);
                downloader.timeout = 60;
            }
            else
            {
                downloader = UnityWebRequest.Get(WwUrl);
                downloader.downloadHandler = new DownloadHandlerFile(path);
                downloader.timeout = 60;
            }
            
            downloader.SendWebRequest();
            while (!downloader.isDone && !downloader.isHttpError && !downloader.isNetworkError)
            {
                // no-op
            }

            if (downloader.isHttpError || downloader.isNetworkError || !string.IsNullOrWhiteSpace(downloader.error))
            {
                return;
            }

            if (string.IsNullOrEmpty(downloader.error))
            {

                AssetDatabase.Refresh();
                AssetDatabase.ImportPackage(path, false);
                AssetDatabase.Refresh();
                FileUtil.DeleteFileOrDirectory(path);
            }

            EditorPrefs.SetBool(KeyIsRollicAdsChinaInstalled, isChina);
            
            downloader.Dispose();
        }
    }
}