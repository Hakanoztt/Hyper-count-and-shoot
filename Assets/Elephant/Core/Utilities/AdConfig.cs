using System;
using System.Collections.Generic;
using System.Linq;

namespace ElephantSDK
{
    [Serializable]
    public class AdConfig
    {
        public string mopub_keyword;
        public bool ad_callback_logs;
        public bool backup_ads_enabled;
        public string backup_interstitial_ad_unit;
        public string backup_rewarded_ad_unit;
        public InterstitialAdLogic interstitial_ad_logic;
        public NetworkIds networks;
        public bool network_id_manipulation_enabled;
        public List<AdConfigParameter> parameters;

        private static AdConfig _instance;

        private AdConfig()
        {
            mopub_keyword = "";
            ad_callback_logs = false;
            backup_ads_enabled = false;
            backup_interstitial_ad_unit = "";
            backup_rewarded_ad_unit = "";
            interstitial_ad_logic = new InterstitialAdLogic();
            networks = null;
            network_id_manipulation_enabled = true;
            parameters = new List<AdConfigParameter>();
        }

        public static AdConfig GetInstance()
        {
            return _instance ?? (_instance = new AdConfig());
        }

        public void Init(AdConfig config)
        {
            if (config == null) return;
            
            mopub_keyword = config.mopub_keyword ;
            ad_callback_logs = config.ad_callback_logs;
            backup_ads_enabled = config.backup_ads_enabled;
            backup_interstitial_ad_unit = config.backup_interstitial_ad_unit;
            backup_rewarded_ad_unit = config.backup_rewarded_ad_unit;
            interstitial_ad_logic = config.interstitial_ad_logic;
            networks = config.networks;
            network_id_manipulation_enabled = config.network_id_manipulation_enabled;
            parameters = config.parameters;
        }

        public string Get(string key, string def = "")
        {
            if (parameters.Count < 0)  return def;

            AdConfigParameter adConfigParameter =
                parameters.Find(item => item.key.Equals(key));

            if (adConfigParameter == null) return def;

            return adConfigParameter.value;
        }
        
        public int GetInt(string key, int def = 0)
        {
            if (parameters.Count < 0)  return def;

            AdConfigParameter adConfigParameter =
                parameters.Find(item => item.key.Equals(key));

            if (adConfigParameter == null) return def;

            int returnVal = int.TryParse(adConfigParameter.value, out returnVal) ? returnVal : def;
            return returnVal;
        }

        public long GetLong(string key, long def = 0)
        {
            if (parameters.Count < 0)  return def;

            AdConfigParameter adConfigParameter =
                parameters.Find(item => item.key.Equals(key));

            if (adConfigParameter == null) return def;
            
            long returnVal = long.TryParse(adConfigParameter.value, out returnVal) ? returnVal : def;
            return returnVal;
        }


        public bool GetBool(string key, bool def = false)
        {
            if (parameters.Count < 0)  return def;

            AdConfigParameter adConfigParameter =
                parameters.Find(item => item.key.Equals(key));

            if (adConfigParameter == null) return def;
            
            bool returnVal = bool.TryParse(adConfigParameter.value, out returnVal) ? returnVal : def;
            return returnVal;
        }
        
        public List<string> GetList(string key, List<string> def = null)
        {
            if(parameters.Count < 0)  return def;

            AdConfigParameter adConfigParameter =
                parameters.Find(item => item.key.Equals(key));

            if (adConfigParameter == null) return def;
           

            var value = adConfigParameter.value;
            var list = value.Split(',').ToList();

            return list.Count > 0 ? list : def;
        }
    }
    
    [Serializable]
    public class InterstitialAdLogic
    {
        public int reduce_value = 0;
        public int display_time_interval = 0;
        public int first_level_to_display = -1;
        public int level_frequency = -1;
    }
    
    [Serializable]
    public class AdConfigParameter
    {
        public string key;
        public string value;
    }
}