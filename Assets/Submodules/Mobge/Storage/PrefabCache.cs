using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace Mobge {
    public class PrefabCache<T> where T : Component {
        private Data _cache = new Data();
        private bool _deactivate;
        private bool _keepInstanceReferences;
        private Dictionary<T, T> _activeReferences;

        public PrefabCache(bool activateDeactivate = true, bool keepInstanceReferences = false){
            _deactivate = activateDeactivate;
            _keepInstanceReferences = keepInstanceReferences;
            if(_keepInstanceReferences){
                _activeReferences = new Dictionary<T, T>();
            }
        }
        public void EnsureCount(T reference, int count, Transform parent = null) {
            var l = GetList(reference);
            l.EnsureCapacity(count);
            for(int i = l.count; i < count; i++) {
                var ii = Instantiate(reference, parent);
                PrepareForCache(ii);
                l.array[i] = ii;
            }
            l.count = count;
        }
        private T Prepare(T instance, T reference){
            if(_deactivate){
                instance.gameObject.SetActive(true);
            }
            if(_keepInstanceReferences){
                _activeReferences[instance] = reference;
            }
            return instance;
        }
        private T Instantiate(T reference, Transform parent){
            return UnityEngine.Object.Instantiate(reference, parent, false);
        }
        public T Pop(T reference, Transform parent = null) {
            var l = GetList(reference);
            if(l.count == 0) {
                return Prepare(Instantiate(reference, parent), reference);
            }
            var a =  Prepare(l.RemoveLast(), reference);
            a.transform.SetParent(parent, false);
            return a;
        }
        private void PrepareForCache(T instance){
            if(_deactivate){
                instance.gameObject.SetActive(false);
            }
        }
        public void Push(T reference, T instance){
            PrepareForCache(instance);
            if(_keepInstanceReferences){
                _activeReferences.Remove(instance);
            }
            GetList(reference).Add(instance);
        }
        /// <summary>
        /// Only works if keepInstanceReferences is true.
        /// </summary>
        public void Push(T instance) {
            if(!_keepInstanceReferences){
                throw NewKeepInstanceException();
            }
            T reference;
            if(!_activeReferences.TryGetValue(instance, out reference)){
                throw new InvalidOperationException("Reference of specified instance cannot be found.");
            }
            Push(reference, instance);
        }
        public bool TryPush(T instance) {
            if (!_keepInstanceReferences) {
                throw NewKeepInstanceException();
            }
            T reference;
            if (!_activeReferences.TryGetValue(instance, out reference)) {
                return false;
            }
            Push(reference, instance);
            return true;
        }
        public bool ContainsInstance(T instance) {
            if(!_keepInstanceReferences) {
                throw NewKeepInstanceException();
            }
            return _activeReferences.ContainsKey(instance);
        }
        private ExposedList GetList(T reference){
            ExposedList l;
            if(!_cache.TryGetValue(reference, out l)){
                l = ExposedList.NewWithCapacity(4);
                _cache.Add(reference, l);
            }
            return l;
        }
        /// <summary>
        /// Only works if keepInstanceReferences is true.
        /// </summary>
        public void CacheAllInstances() {
            if(_keepInstanceReferences){
                foreach(var pair in _activeReferences) {
                    PrepareForCache(pair.Key);
                    GetList(pair.Value).Add(pair.Key);
                }
                _activeReferences.Clear();
            }
            else{
                throw NewKeepInstanceException();
            }
        }
        private Exception NewKeepInstanceException() => new InvalidOperationException("This function works only if keepInstanceReferences property true.");
        private class Data : Dictionary<T, ExposedList> {

        }
        public class ExposedList {
            public T[] array;
            public int count;
            public float lastSweepTime = 0;
            private ExposedList() {

            }
            public static ExposedList NewWithCapacity(int capacity){
                ExposedList l = new ExposedList();
                l.array = new T[capacity];
                l.count = 0;
                return l;
            }
            public T RemoveFast(int i){
                var t = array[i];
                count--;
                array[i] = array[count];
                return t;
            }
            public T RemoveLast(){
                count--;
                return array[count];
            }
            public int Capacity{
                set{
                    var newArray = new T[value];
                    Array.Copy(this.array, newArray, count);
                    array = newArray;
                }
            }
            public void EnsureCapacity(int count){
                int newSize = array.Length == 0 ? 4 : array.Length * 2;
                if(newSize < count){
                    newSize = count;
                }
                Capacity = newSize;
            }
            public void Add(T t){
                if(count == array.Length) {
                    EnsureCapacity(count + 1);
                }
                array[count] = t;
                count++;
            }
        }
    }
}