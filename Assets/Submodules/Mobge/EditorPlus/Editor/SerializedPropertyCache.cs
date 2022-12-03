using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge {
    public class SerializedPropertyCache {

        private static SerializedPropertyCache _instance;
        public static SerializedPropertyCache Instance {
            get {
                if(_instance == null) {
                    _instance = new SerializedPropertyCache();
                }
                return _instance;
            }
        }

        private Dictionary<int, Cache> _caches;


        public Cache this[SerializedProperty p] {
            get {
                int key = GetKey(p);
                if (!_caches.TryGetValue(key, out var c)) {
                    c = Cache.New();
                    _caches[key] = c;
                }
                return c;
            }

        }

        private SerializedPropertyCache() {
            _caches = new Dictionary<int, Cache>();
        }
        public static Cache GetCache(SerializedProperty p) {
            return Instance[p];
        }

        private int GetKey(SerializedProperty p) {
            return p.CountRemaining() + p.depth * (1 << 8);
        }

        

        public struct Cache {
            private Dictionary<string, object> _properties;
            public static Cache New() {
                Cache c;
                c._properties = new Dictionary<string, object>();
                return c;
            }

            public T Get<T>(string key, T defaultValue) {
                if (!_properties.TryGetValue(key, out object val)) {
                    if(val is T t) {
                        return t;
                    }
                }
                return defaultValue;
            }
            public void Set(string key, object value) {
                _properties[key] = value;
            }
        }
    }
}