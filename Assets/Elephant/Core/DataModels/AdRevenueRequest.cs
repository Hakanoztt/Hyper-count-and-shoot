using System;

namespace ElephantSDK
{
    [Serializable]
    public class AdRevenueRequest : BaseData
    {
        public string mopubRevenueData;
        public string ironsourceRevenueData;
        
        private AdRevenueRequest()
        {
        }
        
        public static AdRevenueRequest CreateAdRevenueRequest(string mopubRevenueData)
        {
            var a = new AdRevenueRequest();
            a.FillBaseData(ElephantCore.Instance.GetCurrentSession().GetSessionID());
            a.mopubRevenueData = mopubRevenueData;
            a.ironsourceRevenueData = "";
            return a;
        }
        
        public static AdRevenueRequest CreateIronSourceAdRevenueRequest(string ironsourceRevenueData)
        {
            var a = new AdRevenueRequest();
            a.FillBaseData(ElephantCore.Instance.GetCurrentSession().GetSessionID());
            a.mopubRevenueData = "";
            a.ironsourceRevenueData = ironsourceRevenueData;
            return a;
        }
    }
}