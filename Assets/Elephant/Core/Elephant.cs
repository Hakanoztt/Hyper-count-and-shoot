using System;
using System.Collections;
using UnityEngine;

namespace ElephantSDK
{
    public class Elephant
    {
        private static string LEVEL_STARTED = "level_started";
        private static string LEVEL_FAILED = "level_failed";
        private static string LEVEL_COMPLETED = "level_completed";


        public static void Init(bool isOldUser = false, bool gdprSupported = false)
        {
            ElephantCore.Instance.Init(isOldUser, gdprSupported);
        }

        public static bool UserGDPRConsent()
        {
            if (ElephantCore.Instance == null)
            {
                Debug.LogWarning("Elephant SDK isn't working correctly, make sure you put Elephant prefab into your first scene..");
                return false;
            }

            return ElephantCore.Instance.UserGDPRConsent();
        }
        
        public static void ShowSupportView(string subject, string body)
        {
            subject = Utils.ReplaceEscapeCharsForUrl(subject);
            body = body + "\n\n" + "Elephant ID: " + ElephantCore.Instance.userId + "\nIDFV: " +
                   ElephantCore.Instance.idfv;
            body = Utils.ReplaceEscapeCharsForUrl(body);
            Application.OpenURL("mailto:" + "support@rollicgames.com" + "?subject=" + subject + "&body=" + body);
        }

        public static void IsIapBanned(Action<bool, string> callback)
        {
            if (ElephantCore.Instance == null)
            {
                Debug.LogWarning("Elephant SDK isn't working correctly, make sure you put Elephant prefab into your first scene..");
                return;
            }

            if (callback == null)
            {
                Debug.LogError("IsIapBanned callback cannot be null!");
                return;
            }

            ElephantCore.Instance.IsIapBanned(callback);
        }

        public static void ShowAlertDialog(string title, string message)
        {
#if UNITY_IOS
            ElephantIOS.showAlertDialog(title, message);
#elif UNITY_ANDROID
            ElephantAndroid.showAlertDialog(title, message);
#else
            Debug.LogError(message); 
#endif
        }

        public static void LevelStarted(int level, Params parameters = null)
        {
            MonitoringUtils.GetInstance().SetCurrentLevel(level);
            CustomEvent(LEVEL_STARTED, level, parameters);
        }

        public static void LevelCompleted(int level, Params parameters = null)
        {
            CustomEvent(LEVEL_COMPLETED, level, parameters);
        }

        public static void LevelFailed(int level, Params parameters = null)
        {
            CustomEvent(LEVEL_FAILED, level, parameters);
        }

        public static void Event(string type, int level, Params parameters = null)
        {
            CustomEvent(type, level, parameters);
        }

        public static void AdEvent(string type, string adUnitId = "", string errorCode = "")
        {
            if (!AdConfig.GetInstance().ad_callback_logs) return;
            
            Params param = Params.New();
            if (!string.IsNullOrEmpty(adUnitId)) param.Set("adUnitId", adUnitId);
            if (!string.IsNullOrEmpty(errorCode)) param.Set("error", errorCode);
                
            CustomEvent("ads_sdk_" + type, -1, param);
        }

        public static void Transaction(string type, int level, long amount, long finalAmount, string source)
        {
            if (ElephantCore.Instance == null)
            {
                Debug.LogWarning("Elephant SDK isn't working correctly, make sure you put Elephant prefab into your first scene..");
                return;
            }
            
            var t = TransactionData.CreateTransactionData();
            t.type = type;
            t.level = level;
            t.amount = amount;
            t.final_amount = finalAmount;
            t.source = source;
            
            var req = new ElephantRequest(ElephantCore.TRANSACTION_EP, t);
            ElephantCore.Instance.AddToQueue(req);
        }

        public static void AdRevenueEvent(string mopubRevenueData)
        {
            if (ElephantCore.Instance == null)
            {
                Debug.LogWarning("Elephant SDK isn't working correctly, make sure you put Elephant prefab into your first scene..");
                return;
            }

            var adRevenueRequest = AdRevenueRequest.CreateAdRevenueRequest(mopubRevenueData);

            var req = new ElephantRequest(ElephantCore.AD_REVENUE_EP, adRevenueRequest);
            ElephantCore.Instance.AddToQueue(req);
        }
        
        // For IS integrated apps.
        public static void IronsourceAdRevenueEvent(string ironsourceRevenueData)
        {
            if (ElephantCore.Instance == null)
            {
                Debug.LogWarning("Elephant SDK isn't working correctly, make sure you put Elephant prefab into your first scene..");
                return;
            }

            var adRevenueRequest = AdRevenueRequest.CreateIronSourceAdRevenueRequest(ironsourceRevenueData);

            var req = new ElephantRequest(ElephantCore.AD_REVENUE_EP, adRevenueRequest);
            ElephantCore.Instance.AddToQueue(req);
        }

        private static void CustomEvent(string type, int level, Params param = null)
        {
            if (ElephantCore.Instance == null)
            {
                Debug.LogWarning("Elephant SDK isn't working correctly, make sure you put Elephant prefab into your first scene..");
                return;
            }

            var ev = EventData.CreateEventData();
            ev.type = type;
            ev.level = level;

            if (param != null)
            {
                MapParams(param, ev);
            }

            var req = new ElephantRequest(ElephantCore.EVENT_EP, ev);
            ElephantCore.Instance.AddToQueue(req);
        }
        
        
        
    


        private static void MapParams(Params param, EventData ev)
        {
            ev.custom_data = param.customData;

            int c = 0;
            foreach (var k in param.stringVals.Keys)
            {
                var v = param.stringVals[k];
                if (c == 0)
                {
                    ev.key_string1 = k;
                    ev.value_string1 = v;
                }
                else if (c == 1)
                {
                    ev.key_string2 = k;
                    ev.value_string2 = v;
                }
                else if (c == 2)
                {
                    ev.key_string3 = k;
                    ev.value_string3 = v;
                }
                else if (c == 3)
                {
                    ev.key_string4 = k;
                    ev.value_string4 = v;
                }

                c++;
            }


            c = 0;
            foreach (var k in param.intVals.Keys)
            {
                var v = param.intVals[k];
                if (c == 0)
                {
                    ev.key_int1 = k;
                    ev.value_int1 = v;
                }
                else if (c == 1)
                {
                    ev.key_int2 = k;
                    ev.value_int2 = v;
                }
                else if (c == 2)
                {
                    ev.key_int3 = k;
                    ev.value_int3 = v;
                }
                else if (c == 3)
                {
                    ev.key_int4 = k;
                    ev.value_int4 = v;
                }

                c++;
            }

            c = 0;
            foreach (var k in param.doubleVals.Keys)
            {
                var v = param.doubleVals[k];
                if (c == 0)
                {
                    ev.key_double1 = k;
                    ev.value_double1 = v;
                }
                else if (c == 1)
                {
                    ev.key_double2 = k;
                    ev.value_double2 = v;
                }
                else if (c == 2)
                {
                    ev.key_double3 = k;
                    ev.value_double3 = v;
                }
                else if (c == 3)
                {
                    ev.key_double4 = k;
                    ev.value_double4 = v;
                }

                c++;
            }
        }
    }
}