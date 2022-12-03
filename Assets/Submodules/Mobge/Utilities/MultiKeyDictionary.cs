using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public class MultiKeyDictionary<TKey, TValue> : IEqualityComparer<List<TKey>> {
        private Stack<List<TKey>> _keyCache = new Stack<List<TKey>>();
        private Dictionary<List<TKey>, Pair> _dictionary;
        private int _keyCount;
        public MultiKeyDictionary(int keyCount) {
            _dictionary = new Dictionary<List<TKey>, Pair>(this);
            _keyCount = keyCount;
        }
        public void Clear(int newKeyCount) {
            _keyCount = newKeyCount;
            _dictionary.Clear();
        }
        public void Clear() {
            Clear(_keyCount);
        }

        bool IEqualityComparer<List<TKey>>.Equals(List<TKey> x, List<TKey> y) {
            for(int i = 0; i < _keyCount; i++) {
                if(!x[i].Equals(y[i])) {
                    return false;
                }
            }
            return true;
        }

        int IEqualityComparer<List<TKey>>.GetHashCode(List<TKey> obj) {
            int h = 0;
            for(int i = 0; i < _keyCount; i++) {
                var val = obj[i].GetHashCode();
                h += val;
                h *= 23;
            }
            return h;
        }
        public Key NewKey() {
            Key p;
            if(_keyCache.Count == 0) {
                p = new Key(new List<TKey>(), this);
            }
            else {
                var k = _keyCache.Pop();
                k.Clear();
                p = new Key(k, this);
            }
            return p;
        }
        public int Count => _dictionary.Count;
        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }
        public struct Enumerator: IEnumerator<Pair> {
            private Dictionary<List<TKey>, Pair>.ValueCollection.Enumerator _e;

            internal Enumerator(MultiKeyDictionary<TKey,TValue> d) {
                _e = d._dictionary.Values.GetEnumerator();
            }
            public bool MoveNext() {
                return _e.MoveNext();
            }

            public void Reset() {
                ((IEnumerator<Pair>)_e).Reset();
            }

            public void Dispose() {
                _e.Dispose();
            }

            public Pair Current => _e.Current;


            object IEnumerator.Current => _e.Current;
        }
        public struct Key {
            private List<TKey> _keys;
            private MultiKeyDictionary<TKey, TValue> _dic;
            internal Key(List<TKey> keys, MultiKeyDictionary<TKey,TValue> dic) {
                _keys = keys;
                _dic = dic;
            }
            public Key AddKey(TKey key) {
                _keys.Add(key);
                return this;
            }
            public void Add(TValue value) {
                if(_keys.Count != _dic._keyCount) {
                    throw new KeyCountMismatchException();
                }
                _dic._dictionary.Add(_keys,new Pair(_keys, value));
            }
            public bool Remove(bool disposeKey = true) {
                if (_keys.Count != _dic._keyCount) {
                    throw new KeyCountMismatchException();
                }
                var k = _keys;
                if (disposeKey) {
                    _keys = null;
                    _dic._keyCache.Push(k);
                }
                if (!_dic._dictionary.TryGetValue(k, out Pair p)) {
                    return false;
                }
                _dic._keyCache.Push(p.key);
                _dic._dictionary.Remove(k);
                return true;
            }
            public bool TryGet(out TValue value, bool disposeKey = true) {
                if (_keys.Count != _dic._keyCount) {
                    throw new KeyCountMismatchException();
                }
                var k = _keys;
                if (disposeKey) {
                    _keys = null;
                    _dic._keyCache.Push(k);
                }
                var rv = _dic._dictionary.TryGetValue(k, out Pair p);
                value = p.value;
                return rv;
            }
            public bool Contains(bool disposeKey = true) {
                if (_keys.Count != _dic._keyCount) {
                    throw new KeyCountMismatchException();
                }
                var k = _keys;
                if (disposeKey) {
                    _keys = null;
                    _dic._keyCache.Push(k);
                }
                return _dic._dictionary.ContainsKey(k);
            }
            public void Dispose() {
                _dic._keyCache.Push(_keys);
                _keys = null;
            }
        }
        public class KeyCountMismatchException : Exception {

        }
        public struct Pair {
            public List<TKey> key;
            public TValue value;

            public Pair(List<TKey> key, TValue value) {
                this.key = key;
                this.value = value;
            }
        }
    }
}