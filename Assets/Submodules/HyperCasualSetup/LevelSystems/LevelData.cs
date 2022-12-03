using Mobge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup {
    [CreateAssetMenu(menuName = "Hyper Casual/LevelData")]
    public class LevelData : ALevelSet {
        public List<World> worlds;
        
        [Serializable]
        public class World
        {
            public int LinearIdStart { get; internal set; }
            public string name;
            public List<AddressableLevel> levels;
            public bool completed;
            public override string ToString()
            {
                return name.Equals("") ? "Please enter a name for this world" : name;
            }
        }
        public override AddressableLevel this[ID id] {
            get {
                return worlds[id[0]].levels[id[1]];
            }
        }
        private void OnEnable() {
            int next = 0;
            for(int i = 0; i < worlds.Count; i++) {
                worlds[i].LinearIdStart = next;
                next += worlds[i].levels.Count;
            }
        }
        public override ID ToNearestLevelId(ID id) {
            if(id.World < 0) {
                id = ID.New(0, 0);
            }
            return id;
        }
        public override bool TryGetLinearIndex(ID id, out int index) {
            index = worlds[id.World].LinearIdStart + id.Level;
            return true;
        }

        public override bool TryDecreaseLevel(ref ID id)
        {
            throw new NotImplementedException();
        }

        public override IEnumerator<ID> GetDependencies(ID target) {
            return new DependenyEnumerator(this, target);
        }
        public override int GetLevelCount(int world) {
            return worlds[world].levels.Count;
        }
        public override int WorldCount => worlds.Count;
        
        public override bool TryIncreaseLevel(ref ID id) {
            var lc = worlds[id.World];
            if (lc.levels.Count > id.Level + 1)
            {
                id[1]++;
                return true;
            }
            else if (worlds.Count > id.World + 1)
            {
                id[0]++;
                id[1] = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        public struct DependenyEnumerator : IEnumerator<ID>
        {
            private ID _current;
            private LevelData _d;
            private ID _id;
            private bool _moved;
            public DependenyEnumerator(LevelData d, ID id) {
                _d = d;
                _id = id;
                _current = default(ID);
                _moved = false;
            }

            public ID Current => _current;

            object IEnumerator.Current => _current;

            public void Dispose() {

            }

            public bool MoveNext() {
                if (_moved) {
                    return false;
                }
                _moved = true;
                
                if(_id.Level == 0) {
                    if (_id.World == 0) {
                        return false;
                    }
                    _current = ID.FromWorldLevel(_id.World - 1, _d.worlds[_id.World - 1].levels.Count - 1);
                }
                else {
                    _current = ID.FromWorldLevel(_id.World, _id.Level - 1);
                }
                return true;
            }

            public void Reset() {

            }
        }

    }

    /*
    public class AssetReferenceT<TObject> : AssetReference where TObject : UnityEngine.Object
    {

        /// <summary>
        /// Construct a new AssetReference object.
        /// </summary>
        /// <param name="guid">The guid of the asset.</param>
        public AssetReferenceT(string guid) : base(guid) {
        }

        /// <summary>
        /// Load the referenced asset as type TObject.
        /// </summary>
        /// <returns>The load operation.</returns>
        //[Obsolete("We have added Async to the name of all asycn methods (UnityUpgradable) -> LoadAssetAsync(*)", true)]
        [Obsolete]
        public UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<TObject> LoadAsset() {
            return LoadAssetAsync();
        }

        /// <summary>
        /// Load the referenced asset as type TObject.
        /// </summary>
        /// <returns>The load operation.</returns>
        public virtual UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<TObject> LoadAssetAsync() {
            return LoadAssetAsync<TObject>();
        }

        /// <inheritdoc/>
        public override bool ValidateAsset(UnityEngine.Object obj) {
            var type = obj.GetType();
            return typeof(TObject).IsAssignableFrom(type);
        }

        /// <inheritdoc/>
        public override bool ValidateAsset(string path) {
#if UNITY_EDITOR
            var type = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(path);
            return typeof(TObject).IsAssignableFrom(type);
#else
            return false;
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Type-specific override of parent editorAsset.  Used by the editor to represent the asset referenced.
        /// </summary>
        public new TObject editorAsset => (TObject)base.editorAsset;
#endif


    }


    [Serializable]
    public class AssetReference : IKeyEvaluator
    {
        [UnityEngine.Serialization.FormerlySerializedAs("m_assetGUID")]
        [SerializeField]
        string m_AssetGUID = "";
        [SerializeField]
        string m_SubObjectName;

        UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle m_Operation;
        /// <summary>
        /// The actual key used to request the asset at runtime. RuntimeKeyIsValid() can be used to determine if this reference was set.
        /// </summary>
        public virtual object RuntimeKey {
            get {
                if (m_AssetGUID == null)
                    m_AssetGUID = string.Empty;
                if (!string.IsNullOrEmpty(m_SubObjectName))
                    return string.Format("{0}[{1}]", m_AssetGUID, m_SubObjectName);
                return m_AssetGUID;
            }
        }

        public virtual string AssetGUID { get { return m_AssetGUID; } }
        public virtual string SubObjectName { get { return m_SubObjectName; } set { m_SubObjectName = value; } }


        /// <summary>
        /// Returns the state of the internal operation.
        /// </summary>
        /// <returns>True if the operation is valid.</returns>
        public bool IsValid() {
            return m_Operation.IsValid();
        }

        /// <summary>
        /// Get the loading status of the internal operation.
        /// </summary>
        public bool IsDone {
            get {
                return m_Operation.IsDone;
            }
        }

        /// <summary>
        /// Construct a new AssetReference object.
        /// </summary>
        public AssetReference() {
        }

        /// <summary>
        /// Construct a new AssetReference object.
        /// </summary>
        /// <param name="guid">The guid of the asset.</param>
        public AssetReference(string guid) {
            m_AssetGUID = guid;
        }

        /// <summary>
        /// The loaded asset.  This value is only set after the AsyncOperationHandle returned from LoadAssetAsync completes.  It will not be set if only InstantiateAsync is called.  It will be set to null if release is called.
        /// </summary>
        public virtual UnityEngine.Object Asset {
            get {
                if (!m_Operation.IsValid())
                    return null;

                return m_Operation.Result as UnityEngine.Object;
            }
        }

#if UNITY_EDITOR
        UnityEngine.Object m_CachedAsset;
#endif
        /// <summary>
        /// String representation of asset reference.
        /// </summary>
        /// <returns>The asset guid as a string.</returns>
        public override string ToString() {
#if UNITY_EDITOR
            return "[" + m_AssetGUID + "]" + m_CachedAsset;
#else
            return "[" + m_AssetGUID + "]";
#endif
        }

        /// <summary>
        /// Load the referenced asset as type TObject.
        /// </summary>
        /// <typeparam name="TObject">The object type.</typeparam>
        /// <returns>The load operation.</returns>
        //[Obsolete("We have added Async to the name of all asycn methods (UnityUpgradable) -> LoadAssetAsync(*)", true)]
        [Obsolete]
        public UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<TObject> LoadAsset<TObject>() {
            return LoadAssetAsync<TObject>();
        }

        /// <summary>
        /// Loads the reference as a scene.
        /// </summary>
        /// <returns>The operation handle for the scene load.</returns>
        //[Obsolete("We have added Async to the name of all asycn methods (UnityUpgradable) -> LoadSceneAsync(*)", true)]
        [Obsolete]
        public UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<SceneInstance> LoadScene() {
            return LoadSceneAsync();
        }
        /// <summary>
        /// InstantiateAsync the referenced asset as type TObject.
        /// </summary>
        /// <param name="position">Position of the instantiated object.</param>
        /// <param name="rotation">Rotation of the instantiated object.</param>
        /// <param name="parent">The parent of the instantiated object.</param>
        /// <returns></returns>
        //[Obsolete("We have added Async to the name of all asycn methods (UnityUpgradable) -> InstantiateAsync(*)", true)]
        [Obsolete]
        public UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> Instantiate(Vector3 position, Quaternion rotation, Transform parent = null) {
            return InstantiateAsync(position, rotation, parent);
        }

        /// <summary>
        /// InstantiateAsync the referenced asset as type TObject.
        /// </summary>
        /// <typeparam name="TObject">The object type.</typeparam>
        /// <param name="parent">The parent of the instantiated object.</param>
        /// <param name="instantiateInWorldSpace">Option to retain world space when instantiated with a parent.</param>
        /// <returns></returns>
        //[Obsolete("We have added Async to the name of all asycn methods (UnityUpgradable) -> InstantiateAsync(*)", true)]
        [Obsolete]
        public UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> Instantiate(Transform parent = null, bool instantiateInWorldSpace = false) {
            return InstantiateAsync(parent, instantiateInWorldSpace);
        }

        /// <summary>
        /// Load the referenced asset as type TObject.
        /// </summary>
        /// <typeparam name="TObject">The object type.</typeparam>
        /// <returns>The load operation.</returns>
        public virtual UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<TObject> LoadAssetAsync<TObject>() {
            UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<TObject> result = Addressables.LoadAssetAsync<TObject>(RuntimeKey);
            m_Operation = result;
            return result;
        }

        /// <summary>
        /// Loads the reference as a scene.
        /// </summary>
        /// <param name="loadMode">Scene load mode.</param>
        /// <param name="activateOnLoad">If false, the scene will load but not activate (for background loading).  The SceneInstance returned has an Activate() method that can be called to do this at a later point.</param>
        /// <param name="priority">Async operation priority for scene loading.</param>
        /// <returns>The operation handle for the request.</returns>
        public virtual AsyncOperationHandle<SceneInstance> LoadSceneAsync(LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100) {

            var result = Addressables.LoadSceneAsync(RuntimeKey, loadMode, activateOnLoad, priority);
            m_Operation = result;
            return result;
        }
        /// <summary>
        /// Unloads the reference as a scene.
        /// </summary>
        /// <returns>The operation handle for the scene load.</returns>
        public virtual AsyncOperationHandle<SceneInstance> UnLoadScene() {
            return Addressables.UnloadSceneAsync(m_Operation, true);
        }
        /// <summary>
        /// InstantiateAsync the referenced asset as type TObject.
        /// </summary>
        /// <param name="position">Position of the instantiated object.</param>
        /// <param name="rotation">Rotation of the instantiated object.</param>
        /// <param name="parent">The parent of the instantiated object.</param>
        /// <returns></returns>
        public virtual AsyncOperationHandle<GameObject> InstantiateAsync(Vector3 position, Quaternion rotation, Transform parent = null) {
            return Addressables.InstantiateAsync(RuntimeKey, position, rotation, parent, true);
        }

        /// <summary>
        /// InstantiateAsync the referenced asset as type TObject.
        /// </summary>
        /// <typeparam name="TObject">The object type.</typeparam>
        /// <param name="parent">The parent of the instantiated object.</param>
        /// <param name="instantiateInWorldSpace">Option to retain world space when instantiated with a parent.</param>
        /// <returns></returns>
        public virtual AsyncOperationHandle<GameObject> InstantiateAsync(Transform parent = null, bool instantiateInWorldSpace = false) {
            return Addressables.InstantiateAsync(RuntimeKey, parent, instantiateInWorldSpace, true);
        }

        /// <inheritdoc/>
        public virtual bool RuntimeKeyIsValid() {
            Guid result;
            return Guid.TryParse(RuntimeKey.ToString(), out result);
        }

        /// <summary>
        /// Release the internal operation handle.
        /// </summary>
        public virtual void ReleaseAsset() {
            if (!m_Operation.IsValid()) {
                Debug.LogWarning("Cannot release a null or unloaded asset.");
                return;
            }
            Addressables.Release(m_Operation);
            m_Operation = default(AsyncOperationHandle);
        }


        /// <summary>
        /// Release an instantiated object.
        /// </summary>
        /// <param name="obj">The object to release.</param>
        public virtual void ReleaseInstance(GameObject obj) {
            Addressables.ReleaseInstance(obj);
        }

        /// <summary>
        /// Validates that the referenced asset allowable for this asset reference.
        /// </summary>
        /// <param name="obj">The Object to validate.</param>
        /// <returns>Whether the referenced asset is valid.</returns>
        public virtual bool ValidateAsset(UnityEngine.Object obj) {
            return true;
        }

        /// <summary>
        /// Validates that the referenced asset allowable for this asset reference.
        /// </summary>
        /// <param name="path">The path to the asset in question.</param>
        /// <returns>Whether the referenced asset is valid.</returns>
        public virtual bool ValidateAsset(string path) {
            return true;
        }

#if UNITY_EDITOR

        /// <summary>
        /// Used by the editor to represent the asset referenced.
        /// </summary>
        public virtual UnityEngine.Object editorAsset {
            get {
                if (m_CachedAsset != null || string.IsNullOrEmpty(m_AssetGUID))
                    return m_CachedAsset;
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(m_AssetGUID);
                var mainType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                return (m_CachedAsset = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, mainType));
            }
        }
        /// <summary>
        /// Sets the asset on the AssetReference.  Only valid in the editor, this sets both the editorAsset attribute,
        ///   and the internal asset GUID, which drives the RuntimeKey attribute.
        /// <param name="value">Object to reference</param>
        /// </summary>
        public virtual bool SetEditorAsset(UnityEngine.Object value) {
            if (value == null) {
                m_CachedAsset = null;
                m_AssetGUID = string.Empty;
                m_SubObjectName = null;
                return true;
            }

            if (m_CachedAsset != value) {
                var path = UnityEditor.AssetDatabase.GetAssetOrScenePath(value);
                if (string.IsNullOrEmpty(path)) {
                    Addressables.LogWarningFormat("Invalid object for AssetReference {0}.", value);
                    return false;
                }
                if (!ValidateAsset(path)) {
                    Addressables.LogWarningFormat("Invalid asset for AssetReference path = '{0}'.", path);
                    return false;
                }
                else {
                    m_AssetGUID = UnityEditor.AssetDatabase.AssetPathToGUID(path);
                    var mainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
                    m_CachedAsset = mainAsset;
                    if (value != mainAsset)
                        SetEditorSubObject(value);
                }
            }

            return true;
        }

        /// <summary>
        /// Sets the sub object for this asset reference.
        /// </summary>
        /// <param name="value">The sub object.</param>
        /// <returns>True if set correctly.</returns>
        public virtual bool SetEditorSubObject(UnityEngine.Object value) {
            if (value == null) {
                m_SubObjectName = null;
                return true;
            }

            if (editorAsset == null)
                return false;
            if (editorAsset.GetType() == typeof(UnityEngine.U2D.SpriteAtlas)) {
                var spriteName = value.name;
                if (spriteName.EndsWith("(Clone)"))
                    spriteName = spriteName.Replace("(Clone)", "");
                if ((editorAsset as UnityEngine.U2D.SpriteAtlas).GetSprite(spriteName) == null) {
                    Debug.LogWarningFormat("Unable to find sprite {0} in atlas {1}.", spriteName, editorAsset.name);
                    return false;
                }
                m_SubObjectName = spriteName;
                return true;
            }

            var subAssets = UnityEditor.AssetDatabase.LoadAllAssetRepresentationsAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(m_AssetGUID));
            foreach (var s in subAssets) {
                if (s.name == value.name) {
                    m_SubObjectName = value.name;
                    return true;
                }
            }
            return false;
        }
#endif
    }
    */
}