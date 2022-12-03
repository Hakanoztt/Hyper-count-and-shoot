using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.Platformer.Character {
    [CustomEditor(typeof(Character2D))]
    public class ECharacter2D : Editor
    {
        private Character2D _go;
        protected void OnEnable() {
            _go = target as Character2D;
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if(!_go) {
                return;
            }
            var cstt = _go.CurrentState;
            EditorGUILayout.LabelField("current state", cstt == null ? "none" : cstt.GetType().ToString());
            BaseMoveModule mm = null;
            if (_go.CurrentMoveModuleIndex >= 0) {
                mm = _go.MoveModules[_go.CurrentMoveModuleIndex];
            }
            EditorGUILayout.LabelField("current move module", mm == null ? "none" : mm.GetType().ToString());
            if (GUI.changed) {
                EditorExtensions.SetDirty(_go);
            }
        }
    }
}