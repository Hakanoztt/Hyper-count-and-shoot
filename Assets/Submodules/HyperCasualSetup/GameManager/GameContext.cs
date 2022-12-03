using Mobge.HyperCasualSetup.UI;
using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngine.AddressableAssets;

namespace Mobge.HyperCasualSetup {

    public class GameContext : BaseGameContext<LevelResult> {}

    public class BaseGameContext<T> : AGameContext where T : LevelResult {

        public LevelSet.ID startLevelOverride;
        protected new void Awake() {
            base.Awake();
            EnsureGameProgress();
        }

        public virtual void EnsureGameProgress() {
            if (GameProgress == null) {
                GameProgress = new GameProgressData(RoutineManager, saveFile, this);
                GameProgress.cooldown = saveCooldown;
            }
        }

        protected void OnGameDataReady(bool dataNewlyCreated) {
            if (dataNewlyCreated) {
                GameProgressValue.NextLevelToPlay = startLevelOverride;
            }
            StartGame();

        }

        public class GameProgressData : Mobge.Serialization.GameProgressData<Mobge.HyperCasualSetup.GameProgress<T>> {
            private BaseGameContext<T> _context;

            public GameProgressData(RoutineManager routineManager, string filePath, BaseGameContext<T> context) : base(routineManager, filePath) {
                _context = context;
            }
            public override void BecomeReady(bool dataNewlyCreated) {
                base.BecomeReady(dataNewlyCreated);
                _context.OnGameDataReady(dataNewlyCreated);
            }
        }
    }

    public abstract class AGameContext : MonoBehaviour {
        public bool ignoreAddressableMarkingsForEditor = true;
        public int targetFps = 120;
        public bool iOSHideHomeButton = true;
        public bool bypassLevelUnlocking;
        public GarbageCollector.Mode garbageCollectionMode = GarbageCollector.Mode.Enabled;
        public bool TestMode { get; private set; }
        public AssetReferenceT<GameObject>[] instantiateOnAwake;
        [LabelPicker, SerializeField] protected ALevelSet levelData;
        protected ALevelSet _modifiedLevelSet;
        public ALevelSet LevelData => _modifiedLevelSet ? _modifiedLevelSet : levelData;
        public string saveFile = "Progress/data";
        public float saveCooldown = 0;
        public AssetReferenceT<GameObject> menuManagerRes;
        public MenuManager MenuManager { get; private set; }
        public AudioMixerControl audioControl;

        [InterfaceConstraint(typeof(ILoadingHandler))] public Component initialLoadingHandler;

        private Action<MenuManager> _menuListener;


        private Dictionary<object, object> _extras;

        public RoutineManager RoutineManager { get; private set; }

        public void GetMenuManagerSafe(Action<MenuManager> menuListener) {
            if (MenuManager) {
                menuListener(MenuManager);
            }
            else {
                _menuListener += menuListener;
            }
        }
        protected void Awake() {
            RoutineManager = new RoutineManager(); if (!Application.isEditor) GarbageCollector.GCMode = garbageCollectionMode;
#if UNITY_EDITOR
            //
            if (ignoreAddressableMarkingsForEditor) {
                var provider = new UnityEngine.ResourceManagement.ResourceProviders.AssetDatabaseProvider(0.25f);
                UnityEngine.AddressableAssets.Addressables.AddResourceLocator(new EditorResourceLocator(provider));
                UnityEngine.AddressableAssets.Addressables.ResourceManager.ResourceProviders.Add(provider);
            }
#endif
            Application.targetFrameRate = targetFps;
#if UNITY_IOS
            UnityEngine.iOS.Device.hideHomeButton = iOSHideHomeButton;
#endif

            TestMode = true;
        }
        protected void Start() {
            if (audioControl) {
                audioControl.Initialize(this);
            }
        }
        protected virtual void StartGame() {


            AsyncOperationGroup initialLoadings = AsyncOperationGroup.New();
            var menuOp = menuManagerRes.InstantiateAsync();
            initialLoadings.Add(menuOp);
            if (instantiateOnAwake != null) {
                for (int i = 0; i < instantiateOnAwake.Length; i++) {
                    var op = instantiateOnAwake[i].InstantiateAsync();
                    initialLoadings.Add(op);
                    op.Completed += ExtensionLoaded;
                }
            }
            if (initialLoadingHandler is ILoadingHandler lh) {
                lh.HandleLoading(initialLoadings);
            }
            initialLoadings.OnCompleted += OnMenuLoaded;
            initialLoadings.Start();
        }

        protected void Update() {
            RoutineManager.Update();
        }

        protected void OnApplicationFocus(bool focusGained) {
            if (!focusGained) { GameProgress.ForceSave(); }
        }

        protected void OnApplicationPause(bool paused) {
            if (paused) { GameProgress.ForceSave(); }
        }

        protected void OnApplicationQuit() {
            GameProgress.ForceSave();
        }

        private void ExtensionLoaded(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> obj) {
            if(obj.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded) {
                if (obj.Result.TryGetComponent<IGameContextExtension>(out var gc)) {
                    gc.Init(this);
                }
            }
        }

        private void OnMenuLoaded(AsyncOperationGroup obj) {
            MenuManager = ((GameObject)obj.GetOperation(0).Result).GetComponent<HyperCasualSetup.UI.MenuManager>();
            menuManagerRes.ReleaseAsset();
            MenuManager.StartGame(this, out bool testMode);
            TestMode = testMode;
            if (_menuListener != null) {
                _menuListener(MenuManager);
            }
        }

        public Mobge.Serialization.GameProgressData GameProgress { get; protected set; }
        public AGameProgress GameProgressValue => (AGameProgress)GameProgress.ValueUnsafe;

        public virtual void ClaimReward(string eventName, Action<ClaimResult> onStateChanged) {
            StartCoroutine(DummyRewardClaim(onStateChanged));
        }
        private IEnumerator DummyRewardClaim(Action<ClaimResult> onResult) {
            yield return new WaitForSecondsRealtime(0.3f);
#if UNITY_EDITOR
            onResult(ClaimResult.Claimed);
#else
            onResult(ClaimResult.Failed);

#endif
        }
        public enum ClaimResult {
            Claimed,
            Failed,
            Canceled,
        }
        public virtual void ShowInterstitial(string eventName) { }
        public virtual void FireAnalyticsEvent(string eventName, Dictionary<string, string> extraParams = null, int levelIndex = -1) { }
        public interface ILoadingHandler {
            void HandleLoading(AsyncOperationGroup operationGroup);
        }

        public bool TryGetExtra<T>(object key, out T t) where T : class {
            if (_extras == null || !_extras.TryGetValue(key, out object o)) {
                t = default(T);
                return false;
            }
            t = o as T;
            return true;
        }
        public void SetExtra(object key, object extra) {
            if (_extras == null) {
                _extras = new Dictionary<object, object>();
            }
            _extras[key] = extra;
        }
#if UNITY_EDITOR
        private class EditorResourceLocator : UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator {
            private UnityEngine.ResourceManagement.ResourceProviders.AssetDatabaseProvider provider;
            private List<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation> _locations = new List<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>();
            public EditorResourceLocator(UnityEngine.ResourceManagement.ResourceProviders.AssetDatabaseProvider provider) {
                this.provider = provider;
                _locations.Add(null);
            }
            public string LocatorId => GetType().ToString();
            public IEnumerable<object> Keys => null;
            private class Location : UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation {
                private int _hash;
                public Location(string provideId, string key, string path, Type type) {
                    ProviderId = provideId;
                    _hash = key.GetHashCode();
                    InternalId = path;
                    ResourceType = type;
                }
                public string InternalId { get; private set; }
                public string ProviderId { get; private set; }
                public IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation> Dependencies => null;
                public int DependencyHashCode => -1;
                public bool HasDependencies => false;
                public object Data => null;
                public string PrimaryKey => null;
                public Type ResourceType { get; private set; }
                public int Hash(Type resultType) {
                    return _hash + 23*resultType.GetHashCode();
                }
            }
            public bool Locate(object key, Type type, out IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation> locations) {
                var sKey = (string)key;
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(sKey);
                if (string.IsNullOrEmpty(path)) {
                    locations = null;
                    return false;
                }
                _locations[0] = new Location(this.provider.ProviderId, sKey, path, type);
                locations = _locations;
                return true;
            }
        }
#endif
    }
    public interface IGameContextExtension {
        void Init(AGameContext context);
    }
}
