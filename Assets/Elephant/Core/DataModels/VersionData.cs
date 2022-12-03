using System;
using System.Collections.Generic;

namespace ElephantSDK
{
    [Serializable]
    public class VersionData
    {
        public string appVersion = "";
        public string sdkVersion = "";
        public string osVersion = "";
        public string adsSdkVersion = "";
        public string mediationVersion = "";
        public string unityVersion = "";
        public string mediationName = "";

        public VersionData(string appVersion, string sdkVersion, string osVersion,
            string adsSdkVersion, string mediationVersion, string unityVersion, string mediationName)
        {
            this.appVersion = appVersion;
            this.sdkVersion = sdkVersion;
            this.osVersion = osVersion;
            this.adsSdkVersion = adsSdkVersion;
            this.mediationVersion = mediationVersion;
            this.unityVersion = unityVersion;
            this.mediationName = mediationName;
        }
        
        [Serializable]
        public class MopubNetworkData
        {
            public string name = "";
            public string version = "";
        
            public MopubNetworkData(string name, string version)
            {
                this.name = name;
                this.version = version;
            }
        }
    }
    
}