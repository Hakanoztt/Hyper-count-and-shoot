using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge
{
    public class TemporaryEditorObjects : MonoBehaviour
    {
#if UNITY_EDITOR
        public bool printToConsole;
        public bool debugMode;
        private static TemporaryEditorObjects _shared;
        public static TemporaryEditorObjects Shared {
            get {
                if (_shared != null) return _shared;
                if ((_shared = FindObjectOfType<TemporaryEditorObjects>()) != null) return _shared;
                var flags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
                var temporaryObjects = EditorUtility.CreateGameObjectWithHideFlags("temporary objects", flags);
                _shared = temporaryObjects.AddComponent<TemporaryEditorObjects>();
                return _shared;
            }
        }
        public static bool SharedExists => _shared != null;
        private static PrefabCache<Transform> NewCache() {
            return new PrefabCache<Transform>(true, true);
        }
        private PrefabCache<Transform> _cache = NewCache();
        private Dictionary<object, Transform> _unityObjects = new Dictionary<object, Transform>();
        private bool _needsHardClear;

        public PrefabCache<Transform> PrefabCache => _cache;

        private void Print(object obj) {
            if (printToConsole) {
                Debug.Log(GetType().ToString() + ": " + obj);
            }
        }
        public void Clear(bool destroyCache = false) {
            Print("Clear");
            if (destroyCache) {
                _cache = NewCache();
                transform.DestroyAllChildren();
            }
            else {
                _cache.CacheAllInstances();
            }
        }
        public bool TryGetInstance(object owner, out Transform instance) {
            if (_unityObjects.TryGetValue(owner, out instance)) {
                if (instance == null) {
                    _unityObjects.Remove(owner);
                    return false;
                }
                return true;
            }
            return false;
        }
        private void Destroy(GameObject go) {
            go.DestroySelf();
        }
        public void SetObject(object owner, Transform obj, Vector3 position, Action onSelect = null, object editor = null) {
            SetObject(owner, obj, onSelect, editor);
            obj.position = position;
        }
        public void SetObject(object owner, Transform obj, Action onSelect = null, object editor = null) {
            Print("SetObject: " + owner + "-" + obj);
            UpdateHardClear();
            if (_unityObjects.TryGetValue(owner, out Transform old)) {
                Print("Destroy old: " + old.gameObject);
                Destroy(old.gameObject);
            }
            SetHideFlags(obj, this.gameObject.hideFlags);
             _unityObjects[owner] = obj;
            obj.SetParent(transform, false);
            SetSelectAction(obj, owner, onSelect, editor);
        }
        public static void SetHideFlags(Transform tr, HideFlags hideFlags) {
            tr.gameObject.hideFlags = hideFlags;
            for (int i = 0; i < tr.childCount; i++) {
                SetHideFlags(tr.GetChild(i), hideFlags);
            }
        }
        public void SetHideFlagsForAdding(Transform tr) {
            tr.gameObject.hideFlags = gameObject.hideFlags;
            for (int i = 0; i < tr.childCount; i++) {
                SetHideFlagsForAdding(tr.GetChild(i));
            }
        }
        public Transform EnsureObject(object owner, Transform reference, Action onSelect = null, object editor = null) {
            UpdateHardClear();
            Transform instance;
            if (!_unityObjects.TryGetValue(owner, out instance)) {
                instance = _cache.Pop(reference);
                Print("Create by ensure: " + owner + "-" + instance);
                instance.SetParent(transform, false);
                SetHideFlags(instance, this.gameObject.hideFlags);
                _unityObjects.Add(owner, instance);
                SetSelectAction(instance, owner, onSelect, editor);
            }
            return instance;
        }
        private void SetSelectAction(Transform tr, object editableElement, Action onSelect, object editor) {
            if (editableElement != null && onSelect != null) {
                var sh = tr.GetComponent<OnEditorSelectNotifier>();
                if (sh == null) {
                    sh = tr.gameObject.AddComponent<OnEditorSelectNotifier>();
                }
                sh.onSelect = onSelect;
                sh.editableElement = editableElement;
                sh.editor = editor;
            }
        }
        void UpdateHardClear() {
            if (_needsHardClear) {
                Clear(true);
                _needsHardClear = false;
            }
        }
        public void ReplaceOwner(object oldOwner, object newOwner) {
            if (_unityObjects.TryGetValue(oldOwner, out Transform value)) {
                _unityObjects.Remove(oldOwner);
                SetHideFlags(value, this.gameObject.hideFlags);
                _unityObjects.Add(newOwner, value);
            }
        }
        public bool RemoveObject(object owner) {
            Transform instance;
            bool r = _unityObjects.TryGetValue(owner, out instance);
            if (r) {
                if (_cache.ContainsInstance(instance)) {
                    _cache.Push(instance);
                    _unityObjects.Remove(owner);
                }
                else {
                    if (instance) {
                        Print("Destroy by remove: " + instance.gameObject);
                        Destroy(instance.gameObject);
                        _unityObjects.Remove(owner);
                    }
                }
            }
            return r;
        }
        protected void OnValidate() {
            if (_shared == null) _shared = this;
            else if (_shared != this) {
                EditorApplication.delayCall += () => { this.gameObject.DestroySelf(); };
                return;
            }
            _needsHardClear = true;
            for (int i = 0; i < transform.childCount; i++) {
                transform.GetChild(i).gameObject.SetActive(false);
            }
        }
#endif
    }
}