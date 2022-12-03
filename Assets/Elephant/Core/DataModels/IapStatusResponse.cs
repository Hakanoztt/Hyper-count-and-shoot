using System;

namespace ElephantSDK
{
    [Serializable]
    public class IapStatusResponse
    {
        public bool is_banned;
        public string message;
        public string link;
    }
}