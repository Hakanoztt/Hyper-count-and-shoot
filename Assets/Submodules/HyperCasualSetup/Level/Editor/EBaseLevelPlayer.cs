using Mobge.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Mobge.HyperCasualSetup
{
    [CustomEditor(typeof(BaseLevelPlayer), true)]
    public class EBaseLevelPlayer : ELevelPlayer
    {
        [NonSerialized] BaseLevelPlayer _go;
        protected override void OnEnable() {
            base.OnEnable();
            _go = target as BaseLevelPlayer;
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if(_go == null) {
                return;
            }
            var level = _go.level as BaseLevel;
            if (level == null) {
                return;
            }
            level.tag = EditorGUILayout.IntField("tag", level.tag);
            if (GUI.changed) {
                EditorExtensions.SetDirty(level);
                EditorExtensions.SetDirty(_go);
            }
        }
    }
}