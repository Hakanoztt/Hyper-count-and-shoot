using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame {

    public abstract class TriggerTracker<T> : MonoBehaviour {

        public delegate bool TriggerEnterHandler(Collider c, out T t);

        protected static List<Pair> s_tempRemoveColliders = new List<Pair>();

        public static K Add<K>(GameObject gameObject, string tag, TriggerEnterHandler triggerHandler) where K : TriggerTracker<T> {
            var comp = gameObject.AddComponent<K>();
            comp.triggerTag = tag;
            comp.triggerHandler = triggerHandler;
            return comp;
        }


        public string triggerTag;
        public TriggerEnterHandler triggerHandler;
        public Action<T> onTriggerExit;

        private Dictionary<GameObject, TriggerPair> _colliders;


        protected void Awake() {
            _colliders = new Dictionary<GameObject, TriggerPair>();
        }

        

        protected void OnTriggerEnter(Collider other) {
            if (other.isTrigger && other.CompareTag(triggerTag)) {
                if (!_colliders.ContainsKey(other.gameObject)) {
                    if (triggerHandler(other, out var t)) {
                        TriggerPair tp;
                        tp.value = t;
                        tp.trigger = other;
                        _colliders.Add(other.gameObject, tp);
                    }
                }
            }
        }

        protected void OnTriggerExit(Collider other) {
            if(_colliders.TryGetValue(other.gameObject,out var tp)) {
                _colliders.Remove(other.gameObject);
                if (onTriggerExit != null) {
                    onTriggerExit(tp.value);
                }
            }

            var en = _colliders.GetEnumerator();
            while (en.MoveNext()) {
                var c = en.Current;
                if (c.Value.value == null || !c.Value.trigger.enabled) {
                    Pair p;
                    p.gameObject = c.Key;
                    p.t = c.Value.value;
                    s_tempRemoveColliders.Add(p);
                }
            }
            en.Dispose();

            for (int i = 0; i < s_tempRemoveColliders.Count; i++) {
                var c = s_tempRemoveColliders[i];
                _colliders.Remove(c.gameObject);
                if (onTriggerExit != null) {
                    onTriggerExit(c.t);
                }
            }
            s_tempRemoveColliders.Clear();

        }

        public Dictionary<GameObject,TriggerPair>.ValueCollection.Enumerator GetEnumerator() {
            return this._colliders.Values.GetEnumerator();
        }

        public struct TriggerPair {
            public T value;
            public Collider trigger;
        }

        protected struct Pair {
            public GameObject gameObject;
            public T t;
        }
    }
}