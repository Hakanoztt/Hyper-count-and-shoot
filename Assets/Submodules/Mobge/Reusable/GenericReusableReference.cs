using System;
using UnityEngine;

namespace Mobge {
    /// <summary>
    /// This class references an effect to play it on runtime efficiently. 
    /// For advanced usage <see cref="ReusableCache.PushCache(ReusableCache)"/> and <see cref="ReusableCache.ControlledPopCache(ReusableCache)"/>
    /// methods should be called before and after creating objects that has <see cref="GenericReusableReference"/>.</summary>
    [Serializable]
    public struct ReusableReference<T> : ISerializationCallbackReceiver where T : AReusableItem {
        public const string ReferenceFieldName = nameof(_reference);
        [SerializeField]
        private T _reference;
        private ReusableCache _cache;
        private T _item;
        private int _runId;

        public T SpawnItem(Vector3 position, Transform parent = null) {
            if (_reference != null) {
                if (_cache == null) {
                    _cache = ReusableCache.Fallback;
                }
                _item = _cache.SpawnItem(_reference, position, parent) as T;
                _runId = _item.RunId;
                return _item;
            }
            return null;
        }

        public T ReferenceItem => _reference;
        /// <summary>
        /// Returns currently instantiated instance.
        /// Return value of this function should not be cached, 
        /// because playing instance could be finished on further updates. </summary>
        public T CurrentItem {
            get {
                if (!HasItem) return null;
                return _item;
            }
        }
        /// <summary>
        /// Generic version of <see cref="CurrentItem"/>.
        /// Throws exception if current item is not an instance of requested type. </summary>
        public T GetCurrentItem() {

            if (!HasItem) return null;
            return (T)_item;

        }
        private bool HasItem {
            get => _item != null && !_item.IsActive && _item.RunId == _runId;
        }
        public void Stop() {
            if (HasItem) {
                _item.Stop();
            }
            _item = null;
        }

        public void OnBeforeSerialize() {

        }

        public void OnAfterDeserialize() {
            _cache = Register(_reference);
        }
        private static ReusableCache Register(T reference) {
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
                catch (Exception) {
                    //Debug.Log(e);
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