using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    [Serializable]
    public class Pool<T> where T : Component {
        public T referenceObject;
        private Stack<T> _objects;
        public Pool() {

        }
        public Pool(T reference) {
            referenceObject = reference;
            Init();
        }
        public void Init() {
            if (_objects == null) {
                _objects = new Stack<T>();
                referenceObject.gameObject.SetActive(false);
            }
        }
        public void Recycle(T t) {
            t.gameObject.SetActive(false);
            _objects.Push(t);
        }
        public T New(Transform parent) {
            if (_objects.Count > 0) {
                var t = _objects.Pop();
                t.gameObject.SetActive(true);
                t.transform.SetParent(parent, false);
                return t;
            }
            else {
                var t = UnityEngine.Object.Instantiate(referenceObject, parent, false);
                t.gameObject.SetActive(true);
                return t;
            }
        }
    }
}

