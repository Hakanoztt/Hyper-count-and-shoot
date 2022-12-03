using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.StateMachineAI {
    [CustomEditor(typeof(AIActionState))]
    public class EAIConditionState : Editor {

        private static List<string> s_tempActions = new List<string>();
        
        private AIActionState _go;

        private void OnEnable() {
            _go = target as AIActionState;
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if(_go == null) {
                return;
            }
            bool editorDrawed = false;
            if (EStateAI.TryGetAI(_go, out var ai)) {
                if (_go.action.editor_TryGetComponent(ai, out var t)) {
                    editorDrawed = true;
                    s_tempActions.Clear();
                    for(int i = 0; i < t.ActionCount; i++) {
                        s_tempActions.Add(t.GetActionName(i));
                    }
                    _go.actionIndex = EditorLayoutDrawer.Popup("action index", s_tempActions, _go.actionIndex);
                }
            }
            if(!editorDrawed){
                _go.actionIndex = EditorGUILayout.IntField("action index", _go.actionIndex);
            }

            if (GUI.changed) {
                EditorExtensions.SetDirty(_go);
            }
        }
    }
}