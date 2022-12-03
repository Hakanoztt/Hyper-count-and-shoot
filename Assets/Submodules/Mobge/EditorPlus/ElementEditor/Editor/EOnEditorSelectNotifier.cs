using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mobge {
    [CustomEditor(typeof(OnEditorSelectNotifier)), CanEditMultipleObjects]
    public class EOnEditorSelectNotifier : Editor {
        private void OnEnable() {
            var go = target as OnEditorSelectNotifier;
            if (go != null) {
                if (go.onSelect != null) {
                    go.onSelect();
                }
            }
        }
    }
}