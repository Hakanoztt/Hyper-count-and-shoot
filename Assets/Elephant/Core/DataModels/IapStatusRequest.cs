using System;
using UnityEngine;

namespace ElephantSDK
{
    [Serializable]
    public class IapStatusRequest : BaseData
    {
        private IapStatusRequest() { }

        public static IapStatusRequest Create()
        {
            var iapStatusRequest = new IapStatusRequest();
            iapStatusRequest.FillBaseData(ElephantCore.Instance.GetCurrentSession().GetSessionID());
            
            return iapStatusRequest;
        }
    }
}