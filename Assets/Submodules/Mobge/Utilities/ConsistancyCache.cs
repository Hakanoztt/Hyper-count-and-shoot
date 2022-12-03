using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge
{
    public struct ConsistancyCache<Resource, Requirement>
    {
        private Resource[] _resources;
        private Dictionary<int, Requirement> _requirements;
        private ExposedList<int> _indexesToRemove;
        private Dictionary<int, int> _map;
        private HashSet<int> _usedResources;
        public ConsistancyCache(Resource[] resoueces) {
            _resources = resoueces;
            _requirements = new Dictionary<int, Requirement>();
            _map = new Dictionary<int, int>();
            _indexesToRemove = new ExposedList<int>();
            _usedResources = new HashSet<int>();
            
        }
        public void AddRequirement(int id, Requirement req) {
            _requirements.Add(id, req);
        }
        public struct Pair
        {
            public int id;
            public Requirement requirement;
            public Resource resource;
        }
        public Enumerator ConsumePairs() {
            var e = _map.GetEnumerator();
            _indexesToRemove.ClearFast();
            _usedResources.Clear();
            while (e.MoveNext()) {
                var p = e.Current;
                if (!_requirements.ContainsKey(p.Key)) {
                    _indexesToRemove.Add(p.Key);
                }
            }
            var ra = _indexesToRemove.array;
            for (int i = 0; i < _indexesToRemove.Count; i++) {
                _map.Remove(ra[i]);
            }


            return new Enumerator(this);
        }
        public bool IsResourceUsed(int index) {
            return _usedResources.Contains(index);
        }
        public struct Enumerator
        {
            private int _nextResource;
            private Dictionary<int, Requirement>.Enumerator _enum;
            private ConsistancyCache<Resource, Requirement> _cache;

            internal Enumerator(ConsistancyCache<Resource, Requirement> cache) {
                _enum = cache._requirements.GetEnumerator();
                _cache = cache;
                _nextResource = 0;
            }
            public bool MoveNext() {
                bool b = _enum.MoveNext();
                if (!b) {
                    _cache._requirements.Clear();
                }
                return b;
            }
            public Pair Current {
                get {
                    var val = _enum.Current;
                    var r = _cache.GetResource(val.Key);
                    _cache._usedResources.Add(r);
                    return new Pair {
                        id = val.Key,
                        requirement = val.Value,
                        resource =_cache._resources[r],
                    };
                }
            }
        }
        private int GetResource(int id) {
            int resId;
            if (!_map.TryGetValue(id, out resId)) {
                while (_usedResources.Contains(resId)) {
                    resId++;
                }
                _map.Add(id, resId);
                resId++;

                return resId-1;
            }
            return resId;
        }
    }
}