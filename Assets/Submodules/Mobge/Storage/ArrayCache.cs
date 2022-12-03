using System.Collections;
using UnityEngine;

namespace System.Collections.Generic {
    public struct ArrayCache<T>
    {
        private Dictionary<int, Stack<T[]>> _cache;
        public static ArrayCache<T> NewCache() {
            ArrayCache<T> cache;
            cache._cache = new Dictionary<int, Stack<T[]>>();
            return cache;
        }
        public T[] NewArray(int size) {
            Stack<T[]> deads;
            if(!_cache.TryGetValue(size, out deads)) {
                deads = new Stack<T[]>();
                _cache.Add(size, deads);
            }
            if(deads.Count == 0) {
                return new T[size];
            }
            return deads.Pop();
        }
        public void DestroyArray(T[] array, bool clear = false) {
            if(clear) {
                var d = default(T);
                for(int i = 0; i < array.Length; i++) {
                    array[i] = d;
                }
            }
            var size = array.Length;
            Stack<T[]> deads;
            if(!_cache.TryGetValue(size, out deads)) {
                deads = new Stack<T[]>();
                _cache.Add(size, deads);
            }
            deads.Push(array);
        }
    }
}