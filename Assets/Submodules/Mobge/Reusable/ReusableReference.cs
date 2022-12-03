using System;
using UnityEngine;

namespace Mobge {
    /// <summary>
    /// This class references an effect to play it on runtime efficiently. 
    /// For advanced usage <see cref="ReusableCache.PushCache(ReusableCache)"/> and <see cref="ReusableCache.ControlledPopCache(ReusableCache)"/>
    /// methods should be called before and after creating objects that has <see cref="ReusableReference"/>.</summary>
    [Serializable]
    public struct ReusableReference : ISerializationCallbackReceiver {
        public const string ReferenceFieldName = nameof(_reference);
        [SerializeField]
        private AReusableItem _reference;
        private ReusableCache _cache;
        private AReusableItem _item;
        private int _runId;
        private ReusableCache CacheSafe {
            get {
                if (_cache == null) {
                    _cache = ReusableCache.Fallback;
                }
                return _cache;
            }
        }
        public void CleanLeftOvers() {
            if (_reference != null) {
                CacheSafe.Sweep(_reference);
            }
        }
        public AReusableItem SpawnItem(Vector3 position, Transform parent = null) {
            if (_reference != null) {
                
                _item = CacheSafe.SpawnItem(_reference, position, parent);
                _runId = _item.RunId;
                return _item;
            }
            return null;
        }

        public AReusableItem ReferenceItem {
            get {
                return _reference;
            }
            set {
                _reference = value;
            }
        }
        public ReusableCache Cache {
            get => _cache;
            set {
                _cache = value;
            }
        }
        /// <summary>
        /// Returns currently instantiated instance.
        /// Return value of this function should not be cached, 
        /// because playing instance could be finished on further updates. </summary>
        public AReusableItem CurrentItem {
            get {
                if (!HasItem) return null;
                return _item;
            }
        }
        /// <summary>
        /// Generic version of <see cref="CurrentItem"/>.
        /// Throws exception if current item is not an instance of requested type. </summary>
        public T GetCurrentItem<T>() where T : AReusableItem {
            if (!HasItem) return null;
            return (T)_item;
        }
        public bool HasItem {
            get => _item != null && _item.IsActive && _item.RunId == _runId;
        }
        public void Stop() { 
            if (_item != null && _item.RunId == _runId) {
                _item.Stop();
                _item = null;
            }
        }

        public void StopImmediately() {
            if (_item != null && _item.RunId == _runId) {
                _item.StopImmediately();
                _item = null;
            }
        }

        public void OnBeforeSerialize() {

        }

        public void OnAfterDeserialize() {
            _cache = Register(_reference);
        }
        private static ReusableCache Register(AReusableItem reference) {
            var o = reference;
            ReusableCache cache;
            if (o != null) {
#if UNITY_EDITOR
                try {
                    if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) {
                        cache = ReusableCache.Current;
                        if (cache != null) {
                            cache.Register(reference);
                        }
                        return cache;
                    }
                }
                catch (Exception e) {
                    Debug.Log(e);
                }
#else
                cache = ReusableCache.Current;
                if (cache != null) {
                    cache.Register(reference);
                }
                return cache;
#endif
            }
            return null;
        }
    }
}