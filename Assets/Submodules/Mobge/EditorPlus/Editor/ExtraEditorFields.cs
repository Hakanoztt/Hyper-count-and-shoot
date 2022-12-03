using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public class ExtraEditorFields {
#if UNITY_EDITOR
        static ExtraEditorFields _shared;
        public static ExtraEditorFields Shared {
            get {
                if (_shared == null) {
                    _shared = new ExtraEditorFields();
                }
                return _shared;
            }
        }
        private Dictionary<GameObject, Dictionary<string, object>> _fields = new Dictionary<GameObject, Dictionary<string, object>>();
        public void AttachField(GameObject target, string name, IDisposable obj) {
            AttachField(target, name, (object)obj);
        }
        public void AttachField(GameObject target, string name, object obj) {
            var d = target.GetComponent<OnDestroyNotifier>();
            if (d == null) {
                d = target.AddComponent<OnDestroyNotifier>();
            }
            else {
                d.onDestroy -= OnObjectDestroy;
            }
            d.onDestroy += OnObjectDestroy;
            var fs = GetOrCreateFields(target);
            if(fs.TryGetValue(name, out object old)) {
                if(old == obj) {
                    return;
                }
                fs.Remove(name);
                Dispose(old);
            }
            fs.Add(name, obj);
        }
        Dictionary<string, object> GetOrCreateFields(GameObject target) {
            if (!_fields.TryGetValue(target, out Dictionary<string, object> fields)) {
                fields = new Dictionary<string, object>();
                _fields.Add(target, fields);
            }
            return fields;
        }
        public bool TryGetField(GameObject target, string name, out object o) {
            if(_fields.TryGetValue(target, out Dictionary<string, object> fields)) {
                if(fields.TryGetValue(name, out o)) {
                    return true;
                }
            }
            o = null;
            return false;
        }
        public bool TryGetField<T>(GameObject target, string name, out T o) {
            if(TryGetField(target, name, out object result)) {
                o = (T)result;
                return true;
            }
            o = default(T);
            return false;
        }
        private void Dispose(object o) {
            var dis = o as IDisposable;
            if (dis != null) {
                dis.Dispose();
            }
        }
        private void OnObjectDestroy(OnDestroyNotifier obj) {
            if(_fields.TryGetValue(obj.gameObject, out Dictionary<string, object> fields)) {
                var e = fields.GetEnumerator();
                while (e.MoveNext()) {
                    Dispose(e.Current.Value);
                }
            }
        }
#endif
    }
}