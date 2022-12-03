using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge {
    [CustomEditor(typeof(AnimationReusable))]
    public class EAnimationReusable : Editor {
        private AnimationReusable _target;
        protected void OnEnable() {
            _target = target as AnimationReusable;
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (!_target) {
                return;
            }
            if (_target.startClip) _target.startClip.wrapMode = WrapMode.Once;
            if (_target.loopClip) _target.loopClip.wrapMode = WrapMode.Loop;
            if (_target.endClip) _target.endClip.wrapMode = WrapMode.Once;

            if (GUILayout.Button("Start")) {
                if(Application.isPlaying)
                    _target.Play();
            }
            if (GUILayout.Button("Stop")) {
                if (Application.isPlaying)
                    _target.Stop();
            }

            if (GUI.changed) {
                EditorExtensions.SetDirty(_target);
            }
        }
    }
}