using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Facebook.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace ElephantSDK
{
    public delegate void OnInitialized();

    public delegate void OnOpenResult(bool gdprRequired);

    public delegate void OnRemoteConfigLoaded();

    public class ElephantCore : MonoBehaviour
    {
        private string GameID = "";
        private string GameSecret = "";


        private string defaultGameID = "";
        private string defaultGameSecret = "";


        public static ElephantCore Instance = null;

#if ELEPHANT_DEBUG
        private static bool debug = true;
        private const string ELEPHANT_BASE_URL = "http://localhost:3100/v2";
#elif UNITY_EDITOR
        private static bool debug = true;
        private const string ELEPHANT_BASE_URL = "https://newapi.rollic.gs/v2";
#else
        private static bool debug = true;
        private const string ELEPHANT_BASE_URL = "https://newapi.rollic.gs/v2";
#endif

        public const string OPEN_EP = ELEPHANT_BASE_URL + "/open";
        public const string GDPR_EP = ELEPHANT_BASE_URL + "/gdpr";
        public const string EVENT_EP = ELEPHANT_BASE_URL + "/event";
        public const string SESSION_EP = ELEPHANT_BASE_URL + "/session";
        public const string MONITORING_EP = ELEPHANT_BASE_URL + "/monitoring";
        public const string TRANSACTION_EP = ELEPHANT_BASE_URL + "/transaction";
        public const string AD_REVENUE_EP = ELEPHANT_BASE_URL + "/adrevenue";
        public const string IAP_STATUS_EP = ELEPHANT_BASE_URL + "/user/iap/status";


        private Queue<ElephantRequest> _queue = new Queue<ElephantRequest>();
        private List<ElephantRequest> _failedQueue = new List<ElephantRequest>();
        private bool processQueues = false;

        private static string QUEUE_DATA_FILE = "ELEPHANT_DATA_QUEUE_";


        private bool sdkIsReady = false;

        private bool openRequestWaiting;
        private bool openRequestSucceded;
        private SessionData currentSession;
        internal long realSessionId;
        internal string idfa = "";
        internal string idfv = "";
        internal string adjustId = "";
        internal string buildNumber = "";
        internal string consentStatus = "NotDetermined";
        internal string userId = "";
        internal List<MirrorData> mirrorData;
        internal int eventOrder = 0;
        internal float focusLostTime = 0;
        
        public bool isIapBanned;
        
        private OpenResponse openResponse;
        private string cachedOpenResponse;

        private static int MAX_FAILED_COUNT = 100;

        private static string OLD_ELEPHANT_FILE = "elephant.json";
#if ELEPHANT_DEBUG
        private static string REMOTE_CONFIG_FILE = "ELEPHANT_REMOTE_CONFIG_DATA_6";
        private static string USER_DB_ID = "USER_DB_ID_6";
        private static string CACHED_OPEN_RESPONSE = "CACHED_OPEN_RESPONSE_DEBUG";
#else
        private static string REMOTE_CONFIG_FILE = "ELEPHANT_REMOTE_CONFIG_DATA";
        private static string USER_DB_ID = "USER_DB_ID";
        private static string CACHED_OPEN_RESPONSE = "CACHED_OPEN_RESPONSE";
#endif


        internal bool gdprSupported = false;


        public static event OnInitialized onInitialized;
        public static event OnOpenResult onOpen;
        public static event OnRemoteConfigLoaded onRemoteConfigLoaded;
        
        private float _nextActionTime = 0.0f;
        private float _period = 2.0F;
        private float _fps;


        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
#if !UNITY_EDITOR && UNITY_ANDROID
            ElephantAndroid.Init();
#endif
        }

        void Start()
        {
            RebuildQueue();
            processQueues = true;
            
            Application.lowMemory += OnLowMemory;
            ReportLatestCrashLog();
        }
        
        void Update()
        {
            if (!InternalConfig.GetInstance().monitoring_enabled) return;
            
            LogMonitoringData();
        }

        private void LogMonitoringData()
        {
            _fps = (1f / Time.unscaledDeltaTime);
            if (float.IsInfinity(_fps) || float.IsNaN(_fps) ) return;

            if (Time.time > _nextActionTime)
            {
                _nextActionTime += _period;
                MonitoringUtils.GetInstance().LogFps(Math.Round(_fps, 1));
                MonitoringUtils.GetInstance().LogCurrentLevel();
            }
        }

        public void Init(bool isOldUser, bool gdprSupported)
        {
            var settings = Resources.Load<ElephantSettings>("ElephantSettings");

            if (settings == null)
            {
                Debug.LogError(
                    "[Elephant SDK]  Elephant SDK settings isn't setup, use Window -> Elephant -> Edit Settings to enter your Game ID and Game Secret");
            }
            else
            {
                this.GameID = settings.GameID;
                this.GameSecret = settings.GameSecret;
            }


            if (GameID.Equals(defaultGameID) || GameSecret.Equals(defaultGameSecret) || GameID.Trim().Length == 0 ||
                GameSecret.Trim().Length == 0)
            {
                Debug.LogError(
                    "[Elephant SDK]  Game ID and Game Secret are not present, make sure you replace them with yours using Window -> Elephant -> Edit Settings");
            }

            VersionCheckUtils.GetInstance();
            
#if UNITY_EDITOR
            buildNumber = "";
#elif UNITY_ANDROID
            buildNumber = ElephantAndroid.getBuildNumber();
#elif UNITY_IOS
            buildNumber = ElephantIOS.getBuildNumber();
#else 
            buildNumber = "";
#endif

            if (!FB.IsInitialized)
            {
                FB.Init(OnFbInitComplete);
            }
            else
            {
                FB.ActivateApp();
            }

            this.gdprSupported = gdprSupported;
            StartCoroutine(InitSDK(isOldUser));
        }

        private IEnumerator InitSDK(bool isOldUser)
        {
            if (Utils.IsFileExists(OLD_ELEPHANT_FILE))
            {
                isOldUser = true;
            }

            string savedConfig = Utils.ReadFromFile(REMOTE_CONFIG_FILE);
            userId = Utils.ReadFromFile(USER_DB_ID) ?? "";

            Log("Remote Config From File --> " + savedConfig);
            
            var isUsingRemoteConfig = 0;

            openResponse = new OpenResponse();

            if (savedConfig != null)
            {
                RemoteConfig.GetInstance().Init(savedConfig);
                RemoteConfig.GetInstance().SetFirstOpen(false);
                openResponse.remote_config_json = savedConfig;
            }
            else
            {
                // First open 
                RemoteConfig.GetInstance().SetFirstOpen(true);
            }

            openResponse.user_id = userId;
            
            openRequestWaiting = true;
            openRequestSucceded = false;

            float startTime = Time.time;
            var realTimeSinceStartup = Time.realtimeSinceStartup;
            var realTimeBeforeRequest = DateTime.Now;

            RequestIDFAAndOpen(isOldUser);

            while (openRequestWaiting && (Time.time - startTime) < 5f)
            {
                yield return null;
            }

            isUsingRemoteConfig = openRequestSucceded ? 1 : -1;

            Log(JsonUtility.ToJson(openResponse));

            var parameters = Params.New()
                .Set("real_duration", (DateTime.Now - realTimeBeforeRequest).TotalMilliseconds)
                .Set("game_duration", (Time.time - startTime) * 1000)
                .Set("real_time_since_startup", (Time.realtimeSinceStartup - realTimeSinceStartup) * 1000)
                .Set("is_using_remote_config", isUsingRemoteConfig)
                .CustomString(JsonUtility.ToJson(openResponse));
            
            Elephant.Event("open_request", -1, parameters);

            RemoteConfig.GetInstance().Init(openResponse.remote_config_json);
            AdConfig.GetInstance().Init(openResponse.ad_config);
            Utils.SaveToFile(REMOTE_CONFIG_FILE, openResponse.remote_config_json);
            Utils.SaveToFile(USER_DB_ID, openResponse.user_id);
            Utils.SaveToFile(CACHED_OPEN_RESPONSE, JsonUtility.ToJson(openResponse));
            userId = openResponse.user_id;
            mirrorData = openResponse.mirror_data ?? new List<MirrorData>();
            currentSession.user_tag = RemoteConfig.GetInstance().GetTag();
            
            if (IsForceUpdateNeeded())
            {
                var forceUpdateEventParams = Params.New()
                    .Set("version_seen", Application.version);
                
                Elephant.Event("force_update_seen", -1, forceUpdateEventParams);
                
#if UNITY_EDITOR
                // no-op
#elif UNITY_ANDROID
                ElephantAndroid.showForceUpdate("Update needed", "Please update your application");
#elif UNITY_IOS
                ElephantIOS.showForceUpdate("Update needed", "Please update your application");
#else 
                // no-op
#endif

                yield break;
            }

            if (onOpen != null)
            {
                if (openResponse.consent_required)
                {
                    onOpen(true);
                }
                else
                {
                    OpenIdfaConsent();
                    onOpen(false);
                }
            }
            else
            {
                Debug.LogWarning("ElephantSDK onOpen event is not handled");
            }

            sdkIsReady = true;
            if (onRemoteConfigLoaded != null)
                onRemoteConfigLoaded();
        }
        
        public void OpenIdfaConsent()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (InternalConfig.GetInstance().idfa_consent_enabled)
            {
                InternalConfig internalConfig = InternalConfig.GetInstance();

                Elephant.Event("ask_idfa_consent", -1);
                ElephantIOS.showIdfaConsent(internalConfig.idfa_consent_type,
                    internalConfig.idfa_consent_delay, internalConfig.idfa_consent_position,
                    internalConfig.consent_text_body, internalConfig.consent_text_action_body,
                    internalConfig.consent_text_action_button, internalConfig.terms_of_service_text,
                    internalConfig.privacy_policy_text, internalConfig.terms_of_service_url,
                    internalConfig.privacy_policy_url);
            }
#endif
        }

        private void SendVersionsEvent()
        {
            var versionCheckUtils = VersionCheckUtils.GetInstance();
            var versionData = new VersionData(Application.version, ElephantVersion.SDK_VERSION,
                SystemInfo.operatingSystem, versionCheckUtils.AdSdkVersion,
                versionCheckUtils.MediationVersion, versionCheckUtils.UnityVersion, versionCheckUtils.Mediation);

            var parameters = Params.New()
                .CustomString(JsonUtility.ToJson(versionData));
            
            Elephant.Event("elephant_sdk_versions_info", -1, parameters);
        }
        
        private bool IsForceUpdateNeeded()
        {
            var internalConfig = openResponse?.internal_config;
            if (internalConfig == null) return false;

            if (string.IsNullOrEmpty(internalConfig.min_app_version)) return false;

            return VersionCheckUtils.GetInstance()
                .CompareVersions(Application.version, internalConfig.min_app_version) < 0;
            
        }

        private void OnFbInitComplete()
        {
            if (FB.IsInitialized) {
                FB.ActivateApp();
            } else {
                Debug.Log("Failed to Initialize the Facebook SDK");
            }
        }
        
        public OpenResponse GetOpenResponse()
        {
            return openResponse;
        }

        private void RequestIDFAAndOpen(bool isOldUser)
        {
            idfv = SystemInfo.deviceUniqueIdentifier;
#if UNITY_EDITOR
            idfa = "UNITY_EDITOR_IDFA";
            StartCoroutine(OpenRequest(isOldUser));
#elif UNITY_IOS
            idfa = ElephantIOS.IDFA();
            consentStatus = ElephantIOS.getConsentStatus();
            Debug.Log("Native IDFA -> " + idfa);
            StartCoroutine(OpenRequest(isOldUser));
#elif UNITY_ANDROID
            idfa = ElephantAndroid.FetchAdId();
            Debug.Log("Native IDFA -> " + idfa);
            StartCoroutine(OpenRequest(isOldUser));
#else
            idfa = "UNITY_UNKOWN_IDFA";
            StartCoroutine(OpenRequest(isOldUser));
#endif
        }

        private IEnumerator OpenRequest(bool isOldUser)
        {
            // initialized event
            if (onInitialized != null)
                onInitialized();


            currentSession = SessionData.CreateSessionData();
            realSessionId = currentSession.GetSessionID();
            SendVersionsEvent();

            var openData = OpenData.CreateOpenData();
            openData.is_old_user = isOldUser;
            openData.gdpr_supported = gdprSupported;
            openData.session_id = currentSession.GetSessionID();
            openData.idfv = idfv;
            openData.idfa = idfa;
            openData.user_id = userId;
            cachedOpenResponse = Utils.ReadFromFile(CACHED_OPEN_RESPONSE);
            if (!string.IsNullOrEmpty(cachedOpenResponse))
            {
                var tempOpenResponse = JsonUtility.FromJson<OpenResponse>(cachedOpenResponse);
                if (tempOpenResponse != null)
                {
                    // Previous open response has successfully saved. Send Hash..
                    openData.hash = tempOpenResponse.hash;
                }
            }

            var json = JsonUtility.ToJson(openData);
            var bodyJson = JsonUtility.ToJson(new ElephantData(json, GetCurrentSession().GetSessionID()));
            yield return PostWithResponse(OPEN_EP, bodyJson);
        }

        IEnumerator PostWithResponse(string url, string bodyJsonString)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            request.SetRequestHeader("Authorization", Utils.SignString(bodyJsonString, GameSecret));
            request.SetRequestHeader("GameID", GameID);

            yield return request.SendWebRequest();

            Log("Status Code: " + request.responseCode);
            Log("Body: " + request.downloadHandler.text);

            if (request.isNetworkError)
            {
                Log("Request Error");
            }
            else
            {
                try
                {
                    if (request.responseCode == 200)
                    {
                        var a = JsonUtility.FromJson<OpenResponse>(request.downloadHandler.text);
                        if (a != null)
                        {
                            openRequestSucceded = true;
                            openResponse = a;
                        }
                    }
                    else if (request.responseCode == 204)
                    {
                        var a = JsonUtility.FromJson<OpenResponse>(cachedOpenResponse);
                        if (a != null)
                        {
                            Elephant.Event("hashed_open_response", -1);
                            openRequestSucceded = true;
                            openResponse = a;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
            }


            openRequestWaiting = false;
        }

        public void IsIapBanned(Action<bool, string> callback)
        {
            StartCoroutine(IsIapBannedRequest(callback));
        }

        private IEnumerator IsIapBannedRequest(Action<bool, string> callback)
        {
            var iapStatusRequest = IapStatusRequest.Create();

            var json = JsonUtility.ToJson(iapStatusRequest);
            var bodyJson = JsonUtility.ToJson(new ElephantData(json, GetCurrentSession().GetSessionID()));
            
            var request = new UnityWebRequest(IAP_STATUS_EP, UnityWebRequest.kHttpVerbPOST);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            request.SetRequestHeader("Authorization", Utils.SignString(bodyJson, GameSecret));
            request.SetRequestHeader("GameID", GameID);

            yield return request.SendWebRequest();

            if (request.isNetworkError)
            {
                Log("Iap Status Check failed");
                callback(true, "Something went wrong. Please try again.");
                Elephant.ShowAlertDialog("Error", "Something went wrong. Please try again.");
            }
            else
            {
                try
                {
                    var iapStatusResponse = JsonUtility.FromJson<IapStatusResponse>(request.downloadHandler.text);
                    if (iapStatusResponse != null)
                    {
                        isIapBanned = iapStatusResponse.is_banned;
                        callback(isIapBanned, iapStatusResponse.message);
                        if (iapStatusResponse.is_banned)
                        {
                            // Sending ToS link via title param.
                            Elephant.ShowAlertDialog(iapStatusResponse.link, iapStatusResponse.message);
                        }
                        
                    }
                    else
                    {
                        callback(true, "Something went wrong. Please try again.");
                        Elephant.ShowAlertDialog("Error", "Something went wrong. Please try again.");
                    }
                }
                catch (Exception e)
                {
                    callback(true, "Something went wrong. Please try again.");
                    Elephant.ShowAlertDialog("Error", "Something went wrong. Please try again.");
                    Log("Iap Status Check failed with exception: " + e.Message);
                }
            }
        }

        public bool UserGDPRConsent()
        {
            return this.openResponse.consent_status;
        }

        public SessionData GetCurrentSession()
        {
            return currentSession;
        }

        public void AddToQueue(ElephantRequest data)
        {
            this._queue.Enqueue(data);
        }


        void OnApplicationFocus(bool focus)
        {
            if (focus)
            {
                currentSession = SessionData.CreateSessionData();
                
                // Reset real session id and event order if necessary time passes
                // Sometimes unity won't reset game after a long focus-free session
                if (Time.unscaledDeltaTime - Instance.focusLostTime > InternalConfig.GetInstance().focus_interval)
                {
                    Instance.realSessionId = currentSession.GetSessionID();
                    Instance.eventOrder = 0;
                }

                Log("Focus Gained");
                // rebuild queues from disk..
                RebuildQueue();

                // start queue processing
                processQueues = true;
            }
            else
            {
                // Time saved for next focus
                Instance.focusLostTime = Time.unscaledDeltaTime;
                
                Log("Focus Lost");
                // pause late update
                processQueues = false;

                // send session log
                var sessionEndCurrentSession = ElephantCore.Instance.GetCurrentSession();
                sessionEndCurrentSession.RefreshBaseData();
                sessionEndCurrentSession.end_time = Utils.Timestamp();

                var sessionReq = new ElephantRequest(SESSION_EP, sessionEndCurrentSession);
                AddToQueue(sessionReq);
                
                var monitoringReq = new ElephantRequest(MONITORING_EP, MonitoringData.CreateMonitoringData());
                AddToQueue(monitoringReq);

                // process queues
                ProcessQueues(true);

                // drain queues and persist them to send after gaining focus
                SaveQueues();
            }
        }

        private void RebuildQueue()
        {
            string json = Utils.ReadFromFile(QUEUE_DATA_FILE);
            if (json != null)
            {
                Log("QUEUE <- " + json);
                var d = JsonUtility.FromJson<QueueData>(json);
                if (d?.queue != null)
                {
                    _failedQueue = d.queue;
                    foreach (var r in _failedQueue)
                    {
                        r.tryCount = 0;
                    }
                }
            }
        }

        private void SaveQueues()
        {
            while (_queue.Count > 0)
            {
                ElephantRequest data = _queue.Dequeue();
                _failedQueue.Add(data);
            }

            var queueJson = JsonUtility.ToJson(new QueueData(_failedQueue));
            Log("QUEUE -> " + queueJson);

            Utils.SaveToFile(QUEUE_DATA_FILE, queueJson);

            _failedQueue.Clear();
        }

        private void LateUpdate()
        {
            ProcessQueues(false);
        }


        private void ProcessQueues(bool forceToSend)
        {
            if (forceToSend || (processQueues && sdkIsReady))
            {
                int failedCount = _failedQueue.Count;
                for (int i = failedCount - 1; i >= 0; --i)
                {
                    ElephantRequest data = _failedQueue[i];
                    int tc = data.tryCount % 6;
                    int backoff = (int) (Math.Pow(2, tc) * 1000);

                    if (Utils.Timestamp() - data.lastTryTS > backoff)
                    {
                        _failedQueue.RemoveAt(i);
                        StartCoroutine(Post(data));
                    }
                }

                while (_queue.Count > 0)
                {
                    ElephantRequest data = _queue.Dequeue();
                    StartCoroutine(Post(data));
                }
            }
        }

        IEnumerator Post(ElephantRequest elephantRequest)
        {
            Log(elephantRequest.tryCount + " - " + (Utils.Timestamp() - elephantRequest.lastTryTS) + " -> " +
                elephantRequest.url + " : " + elephantRequest.data);

            elephantRequest.tryCount++;
            elephantRequest.lastTryTS = Utils.Timestamp();

            var elephantData = new ElephantData(elephantRequest.data, GetCurrentSession().GetSessionID());

            string bodyJsonString = JsonUtility.ToJson(elephantData);

            string authToken = Utils.SignString(bodyJsonString, GameSecret);


#if UNITY_EDITOR

            var request = new UnityWebRequest(elephantRequest.url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            request.SetRequestHeader("Authorization", authToken);
            request.SetRequestHeader("GameID", ElephantCore.Instance.GameID);

            yield return request.SendWebRequest();

            Log("Status Code: " + request.responseCode);

            if (request.responseCode != 200)
            {
                // failed will be retried
                if (_failedQueue.Count < MAX_FAILED_COUNT)
                {
                    _failedQueue.Add(elephantRequest);
                }
                else
                {
                    Log("Failed Queue size -> " + _failedQueue.Count);
                }
            }

#else
#if UNITY_IOS
            ElephantIOS.ElephantPost(elephantRequest.url, bodyJsonString, GameID, authToken, elephantRequest.tryCount);
#elif UNITY_ANDROID
            ElephantAndroid.ElephantPost(elephantRequest.url, bodyJsonString, GameID, authToken, elephantRequest.tryCount);
#endif
            yield return null;
#endif
        }

        public void FailedRequest(string reqJson)
        {
            try
            {
                var req = JsonUtility.FromJson<ElephantRequest>(reqJson);
                req.lastTryTS = Utils.Timestamp();

                // trick..
                var body = JsonUtility.FromJson<ElephantData>(req.data);
                req.data = body.data;

//                Log("Request Failed -> " + reqJson);
//                Log("Request Failed Parsed -> " + req.tryCount + " => " + req.url + " - " + req.data);

                if (_failedQueue.Count < MAX_FAILED_COUNT)
                {
                    _failedQueue.Add(req);
                }
                else
                {
                    Log("Failed Queue size -> " + _failedQueue.Count);
                }
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
        }

        public static void Log(string s)
        {
            if (debug)
            {
                Debug.Log(s);
            }
        }

        public void AcceptGDPR()
        {
            openResponse.consent_required = false;
            openResponse.consent_status = true;
        }
        
        // iOS only!
        // No-op for Android.
        private void ReportLatestCrashLog()
        {
            if (!InternalConfig.GetInstance().crash_log_enabled) return;
            
            var report = CrashReport.lastReport;
            if (report == null) return;
            
            var parameters = Params.New();
            parameters.Set("time", report.time.ToString(CultureInfo.CurrentCulture));
            parameters.Set("text", report.text);
            Elephant.Event("Application_last_crash_log", MonitoringUtils.GetInstance().GetCurrentLevel(), parameters);
        }
        
        private void OnLowMemory()
        {
            if (!InternalConfig.GetInstance().low_memory_logging_enabled) return;
            
            Elephant.Event("Application_low_memory", MonitoringUtils.GetInstance().GetCurrentLevel());
        }
        
        void setConsentStatus(string message)
        {
#if UNITY_IOS && !UNITY_EDITOR
            idfa = ElephantIOS.IDFA();
#endif
            triggerConsentResult(message);
            var parameters = Params.New();
            parameters.Set("status", message);
            Elephant.Event("idfa_consent_change", -1, parameters);
            consentStatus = message;
        }
        
        void sendUiConsentStatus(string message)
        {
            if (message.Equals("denied"))
            {
                triggerConsentResult(message);
            }
            var parameters = Params.New();
            parameters.Set("status", message);
            Elephant.Event("idfa_ui_consent_change", -1, parameters);
            consentStatus = message;
        }

        void triggerConsentResult(string message)
        {
            var parameters = Params.New();
            parameters.Set("status", message);
            Elephant.Event("set_idfa_consent_result", -1, parameters);
            IdfaConsentResult.GetInstance().SetIdfaResultValue(message);
            IdfaConsentResult.GetInstance().SetStatus(IdfaConsentResult.Status.Resolved);
        }
    }
}