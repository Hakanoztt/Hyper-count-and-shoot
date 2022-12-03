using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mobge.Core
{
    public class LevelPlayer : MonoBehaviour
    {
        private static readonly BasicPool<ExposedList<bool>> s_startedIndexesPool = new BasicPool<ExposedList<bool>>();
        private static uint s_nextSession = 1;


        public struct LoadedComponent
        {
            public bool isStarted;
            public List<BaseComponent> startedList;
            public BaseComponent initQueue;
        }

        [Obsolete("Use RoutineManager instead")] public ActionManager ActionManager { get; private set; }
        [Obsolete("Use FixedRoutineManager instead")] public ActionManager FixedActionManager { get; private set; }
        public RoutineManager RoutineManager { get; private set; }
        public RoutineManager FixedRoutineManager { get; private set; }


#if UNITY_EDITOR
        public EditorGrid editorGrid;
#endif
        [HideInInspector]
        public Level level;
        [HideInInspector]
        [SerializeField]
        private Transform _decorationRoot;
        private Transform _physicsRoot;
        private List<BaseComponent> _components;
        private LevelStateManager _levelStateManager;
        private LoadingModule _loadingModule;
        private bool _levelStarted;
        private Dictionary<object, object> _extras;
        public Dictionary<int, BaseComponent> RootComponents { get; private set; }
        public List<BaseComponent> AllComponents => _components;
        public LevelStateManager StateManager => _levelStateManager;
        public Action<LevelPlayer> OnGameStart;

        private uint _session;

        public uint Session => _session;
        public bool TryGetExtra<T>(object key, out T t) where T : class {
            if (_extras == null || !_extras.TryGetValue(key, out object o)) {
                t = default(T);
                return false;
            }
            t = o as T;
            return true;
        }
        public T GetOwner<T>(GameObject go) where T : BaseComponent {
            var key = _levelStateManager.GetKey(go);
            if(key.id < 0) {
                return null;
            }
            return _components[key.owner] as T;
        }
        public bool TryGetExtraByType<T>(out T t) where T : class {
            if(_extras == null) {
                t = null;
                return false;
            }
            var e = _extras.GetEnumerator();
            while(e.MoveNext()) {
                var c = e.Current;
                t = c.Value as T;
                if(t != null) {
                    return true;
                }
            }
            t = null;
            return false;
        }
        public void SetExtra(object key, object extra) {
            if(_extras == null) {
                _extras = new Dictionary<object, object>();
            }
            _extras[key] = extra;
        }
        public T RemoveExtra<T>(object key) where T : class {
            if (_extras == null) {
                return default(T);
            }
            if(_extras.TryGetValue(key, out object value)) {
                _extras.Remove(key);
                return value as T;
            }
            return default(T);
        }
        ///// <summary>
        ///// This implementation should be overrided.
        ///// Default implementation tries to find a camera in the scene and returns it each time it called.
        ///// </summary>
        //public virtual ACameraController Camera {
        //    get => ACameraController.FindOrCreate<Side2DCamera>();
        //}
        /// <summary>
        /// Clears the drawn data. All children objects under LevelPlayer is destroyed.
        /// </summary>
        /// <remarks>It does not discriminate beyond destroying all child objects.
        /// Maybe it should decide wheter what is getting destroyed is what we want to destroy.
        /// Possibly dangerous; can cause data loss in case where a GameObject is put under(accidently) the LevelPlayer.</remarks>
        protected void Awake() {
            _levelStarted = false;
            ActionManager = new ActionManager();
            FixedActionManager = new ActionManager();
            RoutineManager = new RoutineManager();
            FixedRoutineManager = new RoutineManager();
            _loadingModule = new LoadingModule();
            ResetData();
            if(level != null) {
                LoadLevel(level);
            }
        }
        protected void FixedUpdate() {
            FixedActionManager.Update(Time.fixedDeltaTime);
            FixedRoutineManager.Update(Time.fixedDeltaTime);
        }
        protected void Update() {
            ActionManager.Update(Time.deltaTime);
            RoutineManager.Update(Time.deltaTime);
        }
        public void ResetData()
        {
            transform.DestroyAllChildren();
        }
        /// <summary>
        /// Purge and Regenerate the mesh containing GameObjects.
        /// </summary>
        public Transform DecorationRoot {
            get {
                if (!_decorationRoot) 
                    InitDecorationRoot();
                return _decorationRoot;
            }
        }
        public Transform PhysicsRoot {
            get => _physicsRoot;
        }
        public Dictionary<int, BaseComponent> LoadComponents(AsyncOperationGroup operationGroup, Piece root) {
            if (root == null) {
                return null;
            }
            Dictionary<int, BaseComponent> components = new Dictionary<int, BaseComponent>();
            LoadArgs loadArgs = new LoadArgs() {
                levelPlayer = this,
                operationGroup = operationGroup,
            };
            if(root.Components == null) {
                root.Components = new Piece.LevelComponentMap();
            }
            var e = root.Components.GetPairEnumerator();
            while(e.MoveNext()) {
                var c = e.Current;
                var comp = c.Value.GetObject<BaseComponent>();
                components.Add(c.Key, comp);
                comp.Load(loadArgs);
                if(comp is IResourceOwner ro) {
                    for (int j = 0; j < ro.ResourceCount; j++) {
                        var r = ro.GetResource(j);
                        // Debug.Log(comp.position + " " + comp + " " + r.editorAsset);
                        operationGroup.Add(r.LoadAssetAsync<object>());
                    }
                }
            }
            return components;
        }
        private void InitComponents() {
            if(!level) return;
            _components = new List<BaseComponent>();
            var offset = transform.position;
            transform.position = Vector3.zero;
            InitComponents(level, offset, Quaternion.identity, RootComponents);
        }
        public void InitComponents(Piece root, Vector3 offset, Quaternion rotation, Dictionary<int, BaseComponent> rootComponents) {
            StartParams sp;
            sp.initArgs = new BaseComponent.InitArgs() {
                parentTr = transform,
                player = this,
				components = rootComponents,
			};
            var startedIndexes = s_startedIndexesPool.Get();
            startedIndexes.SetCountFast(root.Components.Capacity);
            sp.starteds = startedIndexes.array;
            for(int i = 0; i < startedIndexes.Count; i++) {
                sp.starteds[i] = false;
            }
            sp.matrix = Matrix4x4.TRS(offset, rotation, Vector3.one);
            sp.rotation = rotation;
            if(root.Components == null) {
                root.Components = new Piece.LevelComponentMap();
            }
            //var e = components.GetEnumerator();
            
            var ke = root.Components.GetKeyEnumerator();
            while(ke.MoveNext()) {
                var key = ke.Current;
                this.StartElement(key, sp);
            }
            s_startedIndexesPool.Release(startedIndexes);
        }
        private void StartElement(int index, in StartParams sp) {
            if(sp.starteds[index]) {
                return;
            }
            var comps = sp.initArgs.components;
            sp.starteds[index] = true;
            var c = comps[index];
            var cc = c as IChild;
            Transform parentTr = null;
            if(cc != null) {
                int pindex = cc.Parent;
                if(pindex >= 0) {
                    if (comps.TryGetValue(pindex, out BaseComponent p)&&p is IParent) {

                        StartElement(pindex, sp);
                        parentTr = ((IParent)p).Transform;
                    }
                    else {
#if UNITY_EDITOR
                        Debug.LogError("parent of " + cc + " (pos:"+ c.position+") is set incorrectly.");
#endif
                    }
                }
            }
            if(parentTr == null) {
                parentTr = transform;
                c.position = sp.matrix.MultiplyPoint3x4(c.position);
                var ro = c as IRotationOwner;
                if(ro != null) {
                    ro.Rotation = sp.rotation * ro.Rotation;
                }
            }
            var ia = sp.initArgs;
            ia.parentTr = parentTr;
            ia.id = _components.Count;
            ia.componentId = index;
            _components.Add(c);
            try {
                c.Start(ia);
            }
            catch (Exception e) {
                Debug.LogError("Exception on component start (" + c.GetType() + " id: " + index + " pos:" + (c.position) + ")");
                Debug.LogError(e);
                throw;
            }
        }
        public object GameState
        {
            get
            {
                Dictionary<int, object> state = new Dictionary<int, object>();
                for (int i = 0; i < this._components.Count; i++)
                {
                    var ca = _components[i];
                    var s = ca as Mobge.Serialization.ISaveable;
                    if (s != null)
                    {
                        state.Add(i, s);
                    }
                }
                return state;
            }
        }
        public ComponentEnumerator<T> AllComponentsOfType<T>() where T : class{
            return new ComponentEnumerator<T>(_components);
        }
        private void InitPhysicsRoot()
        {
            if (_physicsRoot)
            {
                throw new Exception("Physics root cannot be created multiple times.");
            }
            _physicsRoot = new GameObject("physics").transform;
            _physicsRoot.SetParent(transform, false);
            var rb = _physicsRoot.gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
        }

        private void InitDecorationRoot()
        {
            if (_decorationRoot)
            {
                _decorationRoot.gameObject.DestroySelf();
            }
            _decorationRoot = new GameObject("decoration").transform;
            _decorationRoot.SetParent(transform, false);
        }
        public bool LoadLevel(Level level, Dictionary<object, object> args = null, bool startGame = true, Action<LevelPlayer> onLoad = null) {
            this.level = level;
            _session = s_nextSession++;
            if (_extras != null) {
                _extras.Clear();
            }
            if (args != null) {
                var e = args.GetEnumerator();
                while (e.MoveNext()) {
                    var p = e.Current;
                    SetExtra(p.Key, p.Value);
                }
            }
            if (startGame) {
                onLoad += _loadingModule.StartLevel;
            }
            bool b = _loadingModule.StartLoading(level, this, onLoad);
            if(!b) {
                this.level = null;
            }
            return b;
        }

        public virtual void StartGame()
        {
            _levelStarted = true;
            _levelStateManager = new LevelStateManager();
            InitPhysicsRoot();
            InitDecorationRoot();
            InitComponents();
            OnGameStart?.Invoke(this);
        }
        public bool IsLevelStarted => _levelStarted;
        public virtual void DestroyLevel() {
            if(_levelStarted) {
                _levelStarted = false;
                for(int i = 0; i < _components.Count; i++) {
                    var component = _components[i];
                    component.End();
                    if(component is IResourceOwner resourceOwner) {
                        for (int j = 0; j < resourceOwner.ResourceCount; j++) {
                            var assetReference = resourceOwner.GetResource(j);
                            assetReference.ReleaseAsset();
                        }
                    }
                }
            }
            gameObject.DestroySelf();
        }
        #region Nested Types
        public struct LoadArgs {
            public LevelPlayer levelPlayer;
            public AsyncOperationGroup operationGroup;
        }
        private struct StartParams {
            public BaseComponent.InitArgs initArgs;
            public bool[] starteds;
            public Matrix4x4 matrix;
            internal Quaternion rotation;
        }
        public struct ComponentEnumerator<T> : IEnumerator<T> where T : class
        {
            private List<BaseComponent> _components;
            private int _index;
            private T _current;
			/// <summary>
			/// Initializes a new instance of the <see cref="T:Mobge.Core.LevelPlayer.ComponentEnumerator`1"/> struct.
			/// </summary>
			/// <param name="components">Components.</param>
			public ComponentEnumerator(List<BaseComponent> components) {
                _index = -1;
                _components = components;
                _current = null;
            }
            public T Current => _components[_index] as T;

            object IEnumerator.Current => _components[_index];

            public void Dispose() {
                
            }
			/// <summary>
			/// Moves to the next component
			/// </summary>
			/// <returns><c>true</c>, if next was moved, <c>false</c> otherwise.</returns>
			/// <remarks>list index is increased when all components in the current dict have been iterated</remarks>
            public bool MoveNext() {
                do {
                    _index++;
                    if (_index >= _components.Count) {
                        return false;
                    }
                    _current = _components[_index] as T;
                } while (_current == null);
                return true;
            }

            public void Reset()
            {
                _index = -1;
			}
        }
        private class LoadingModule {
            
            private AsyncOperationGroup _loadingOperation;
            private AsyncOperationStatus _levelLoadStatus;
            private LevelPlayer _player;
            private Action<LevelPlayer> _onLoad;
            internal LoadingModule() {
                _levelLoadStatus = AsyncOperationStatus.None;
            }
            public bool StartLoading(Level level, LevelPlayer player, Action<LevelPlayer> onLoad)
            {
                if (_levelLoadStatus != AsyncOperationStatus.None || _loadingOperation != null)
                {
                    return false;
                }
                _player = player;
                _loadingOperation = AsyncOperationGroup.New();
                level.LoadReferences(_loadingOperation);
                player.RootComponents = player.LoadComponents(_loadingOperation, level);
                _onLoad = onLoad;
                _loadingOperation.OnCompleted += PreLoaded;
                _loadingOperation.Start();
                return true;
            }

            internal void PreLoaded(AsyncOperationGroup obj)
            {
                _player.RoutineManager.DoAction(Loaded, 0);
            }

            private void Loaded(bool complete, object data) {
                if (complete) {
                    _levelLoadStatus = _loadingOperation.Status;
                    _loadingOperation = null;
                    if (_onLoad != null) {
                        if (_levelLoadStatus == AsyncOperationStatus.Succeeded) {
                            _onLoad(_player);
                        }
                        else {
                            _onLoad(null);
                        }
                    }
                    _player = null;
                }
            }
            public void StartLevel(LevelPlayer player) {
                if (_levelLoadStatus == AsyncOperationStatus.Succeeded) {
                    player.StartGame();
                }
            }

        }
        #endregion Nested Types
    }
}

