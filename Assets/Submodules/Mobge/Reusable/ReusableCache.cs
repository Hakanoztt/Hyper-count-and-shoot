using UnityEngine;
using System;
using System.Collections.Generic;

namespace Mobge {
    /// <summary>
    /// This class act as a context for instantiated items. It efficiently
    /// keeps track of the instantiated items and pools them when they finish their purpose.
    /// When you destroy this class it removes all the pools that are created for items.
    /// See <see cref="PushCache"/> and <see cref="ControlledPopCache"/>.
    /// </summary>
    public class ReusableCache : MonoBehaviour {
        #region static part
        private static ReusableCache _fallback;
        public static ReusableCache Fallback{
            get{
                if(!_fallback) {
                    _fallback = new GameObject("fallback effect player").AddComponent<ReusableCache>();
                }
                return _fallback;
            }
        }
        private static Stack<ReusableCache> _stack = new Stack<ReusableCache>();
        public static ReusableCache Current {
            get {
                if(_stack.Count == 0) {
                    return null;
                }
                return _stack.Peek();
            }
        }
        public static ReusableCache CurrentOrFallback {
            get {
                if(_stack.Count == 0) {
                    return Fallback;
                }
                return _stack.Peek();
            }
        }
#if UNITY_EDITOR && UNITY_2019_3_OR_NEWER
        [UnityEditor.InitializeOnEnterPlayMode]
        static void ResetStatic() {
            _fallback = null;
        }
#endif
        /// <summary>
        /// Call this function before creating objects that has <see cref="ReusableItem"/> structs on them.
        /// </summary>
        public static void PushCache(ReusableCache player){
            _stack.Push(player);
        }
        /// <summary>
        /// Call this function after creating objects that has <see cref="ReusableItem"/> structs on them.
        /// </summary>
        public static void ControlledPopCache(ReusableCache player){
            var p = _stack.Pop();
            if(p != player) {
                throw new InvalidOperationException("Control fails while pop player from stack. (" + player + " != " + p + ")");
            }
        }
        #endregion // static part

        #region functionality
        Cache _activeList;
        Cache _disabledList;
        protected void Awake(){
            _activeList = new Cache();
            _disabledList = new Cache();
        }
        private AReusableItem Prepare(AReusableItem reference, AReusableItem effect, Vector3 position, Transform parent = null){
            var etr = effect.transform;
            if(parent == null) {
                parent = transform;
            }
            etr.SetParent(parent, false);
            etr.localPosition = position;
            _activeList.Add(reference, effect);
            effect.gameObject.SetActive(true);
            effect.Play();
            return effect;
        }
        public AReusableItem SpawnItem(AReusableItem reference, Vector3 position, Transform parent = null){
            AReusableItem effect;
            if(_disabledList.TryPop(reference, out effect, _activeList, transform)) {
                return Prepare(reference, effect, position, parent);
            }
            return Prepare(reference, Instantiate(reference), position, parent);
        }
        public void Register(AReusableItem reference) {
            _disabledList.EnsureCache(reference, 2, transform);
        }
        private static AReusableItem Ins(AReusableItem reference){
            return Instantiate(reference);
        }
        public void Sweep(AReusableItem reference) {
            this._disabledList.Sweep(reference, _activeList, transform);
        }
        private class Cache : Dictionary<AReusableItem, ExposedList<AReusableItem>> {
            private const float c_minSweepDelay = 0.15f;
            public void Sweep(AReusableItem reference, Cache activeList, Transform cacheParent) {
                if(TryGetValue(reference, out var stack)) {
                    activeList.SweepTo(reference, stack, cacheParent);
                }
            }
            public bool TryPop(AReusableItem reference, out AReusableItem effect, Cache activeList, Transform cacheParent) {
                var stack = GetList(reference);
                while(stack.Count > 0) {
                    effect = stack.RemoveLast();
                    if(effect != null) {
                        return true;
                    }
                }
                if(activeList.SweepTo(reference, stack, cacheParent)){
                    effect = stack.RemoveLast();
                    return true;
                }
                effect = null;
                return false;
            }
            public void EnsureCache(AReusableItem reference, int count, Transform cache){
                var l = GetList(reference);
                while(l.Count < count){
                    var i = Ins(reference);
                    i.transform.SetParent(cache, false);
                    l.Add(i);
                }
            }
            private void Reset(AReusableItem effect, Transform parent){
                effect.StopImmediately();
                effect.transform.SetParent(parent, false);
            }
            private ExposedList<AReusableItem> GetList(AReusableItem reference) {
                ExposedList<AReusableItem> stack;
                if(!TryGetValue(reference, out stack)) {
                    stack = new ExposedList<AReusableItem>(2);
                    Add(reference, stack);
                }
                return stack;
            }
            private bool SweepTo(AReusableItem reference, ExposedList<AReusableItem> target, Transform parent) {
                bool result = false;
                var ct = Time.unscaledTime;
                var source = GetList(reference);
                if(ct - source.lastSweepTime < c_minSweepDelay){
                    return false;
                }
                source.lastSweepTime = ct;
                var sa = source.array;
                for(int i = 0; i < source.Count; ) {
                    var e = sa[i];
                    if(e == null) {
                        source.RemoveFast(i);
                    }
                    else if(!e.IsActive || !e.gameObject.activeInHierarchy) {
                        e.gameObject.SetActive(false);
                        //Debug.Log("deactivating: " + e.gameObject);
                        target.Add(e);
                        Reset(e, parent);
                        source.RemoveFast(i);
                        result = true;
                    }
                    else{
                        i++;
                    }
                }
                return result;
            }
            public void Add(AReusableItem reference, AReusableItem effect){
                GetList(reference).Add(effect);
            }
        }
        
        #endregion // functionality
    }
}