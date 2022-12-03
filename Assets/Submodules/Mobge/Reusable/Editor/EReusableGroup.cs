using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge {
    [CustomEditor(typeof(ReusableGroup))]
    public class EReusableGroup : Editor {
        private ReusableGroup _go;
        private List<AReusableItem> _tempItems = new List<AReusableItem>();
        protected void OnEnable() {
            _go = target as ReusableGroup;
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (!_go) {
                return;
            }
            if(_go.items == null) {
                _go.items = new AReusableItem[0];
            }
            var all = GatherList();
            if(!Compare(_go.items, all)) {
                _go.items = all.ToArray();
                GUI.changed = true;
            }


            if (GUI.changed) {
                EditorExtensions.SetDirty(_go);
            }
        }
        private List<AReusableItem> GatherList() {
            _tempItems.Clear();

            var all = _go.GetComponentsInChildren<AReusableItem>();
            for(int i = 0; i < all.Length; i++) {
                var item = all[i];
                var ru = item as ReusableGroup;
                if (ru != null) {
                    if(ru != _go) {
                        ru.DestroySelf();
                    }
                }
                else {
                    _tempItems.Add(item);
                }
            }
            return _tempItems;
        }
        private bool Compare<T>(T[] a1, List<T> a2) where T : class {
            if (a1.Length != a2.Count) return false;
            for (int i = 0; i < a1.Length; i++) {
                if (a1[i] != a2[i])
                    return false;
            }
            return true;
        }
    }
}