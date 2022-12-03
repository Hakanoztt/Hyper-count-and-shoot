using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public class CombinationKeyDictionary<TKey, TValue> : IEqualityComparer<HashSet<TKey>> {
        private Stack<HashSet<TKey>> _keyCache = new Stack<HashSet<TKey>>();
        private Dictionary<HashSet<TKey>, Pair> _dictionary = new Dictionary<HashSet<TKey>, Pair>();
        bool IEqualityComparer<HashSet<TKey>>.Equals(HashSet<TKey> x, HashSet<TKey> y) {
            if (x.Count != y.Count) return false;
            var e = x.GetEnumerator();
            while (e.MoveNext()) {
                if (!y.Contains(e.Current)) {
                    return false;
                }
            }
            return true;
        }

        int IEqualityComparer<HashSet<TKey>>.GetHashCode(HashSet<TKey> obj) {

            var e = obj.GetEnumerator();
            int code = 0;
            while (e.MoveNext()) {
                int cCode = e.Current.GetHashCode();
                code += cCode * cCode;
            }
            return code;
        }
        public Key NewKey() {
            Key p;
            if(_keyCache.Count == 0) {
                p = new Key(new HashSet<TKey>(), this);
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
        public void Clear(bool recycleKeys) {
            if (recycleKeys) {
                var e = GetEnumerator();
                while (e.MoveNext()) {
                    _keyCache.Push(e.Current.key);
                }
            }
            _dictionary.Clear();
        }
        public struct Enumerator : IEnumerator<Pair> {
            private Dictionary<HashSet<TKey>, Pair>.ValueCollection.Enumerator _e;
            internal Enumerator(CombinationKeyDictionary<TKey, TValue> d) {
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
            private HashSet<TKey> _keys;
            private CombinationKeyDictionary<TKey, TValue> _dic;
            internal Key(HashSet<TKey> keys, CombinationKeyDictionary<TKey, TValue> dic) {
                _keys = keys;
                _dic = dic;
            }
            public Key AddKey(TKey key) {
                _keys.Add(key);
                return this;
            }
            public void Add(TValue value) {
                _dic._dictionary.Add(_keys, new Pair(_keys, value));
            }
            public bool Remove(bool disposeKey = true) {
                var k = _keys;
                if (disposeKey) {
                    _keys = null;
                    _dic._keyCache.Push(k);
                }
                if(!_dic._dictionary.TryGetValue(k, out var p)) {
                    return false;
                }
                _dic._keyCache.Push(p.key);
                _dic._dictionary.Remove(k);
                return true;
            }
            public bool TryGet(out TValue value, bool disposeKey = true) {
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

        public struct Pair {
            public HashSet<TKey> key;
            public TValue value;

            public Pair(HashSet<TKey> key, TValue value) {
                this.key = key;
                this.value = value;
            }
        }
    }
}