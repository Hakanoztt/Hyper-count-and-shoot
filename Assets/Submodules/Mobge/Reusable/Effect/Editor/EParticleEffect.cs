using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge {
    [CustomEditor(typeof(ParticleEffect))]
    public class EParticleEffect : Editor {
        private ParticleEffect _go;
        private List<ParticleSystem> _tempParticles = new List<ParticleSystem>();
        protected void OnEnable(){
            _go = target as ParticleEffect;
        }
        public override void OnInspectorGUI(){
            base.OnInspectorGUI();
            if(!_go){
                return;
            }
            if(_go.effects == null){
                _go.effects = new ParticleSystem[0];
            }

            var all = _go.GetComponentsInChildren<ParticleSystem>();
            if(!Compare(all, _go.effects)){
                _go.effects = all;
                GUI.changed = true;
            }
            for(int i = 0; i< _go.effects.Length; i++){
                var m = _go.effects[i].main;
                if (m.playOnAwake) {
                    m.playOnAwake = false;
                }
            }
            var allOthers = _go.GetComponentsInChildren<ParticleEffect>();
            if(allOthers.Length > 1){
                var c = GUI.contentColor;
                GUI.contentColor = Color.red;
                if(GUILayout.Button("Delete unnecessary " + nameof(ParticleEffect) + " childrens.")){
                    for(int i = 0; i < allOthers.Length; i++){
                        if(allOthers[i] != _go){
                            DestroyImmediate(allOthers[i]);
                        }
                    }
                }
                GUI.contentColor = c;
            }
            if (GUILayout.Button("Play")) {
                _go.Play();
            }
            if (GUILayout.Button("Stop")) {
                _go.Stop();
            }
            if (GUILayout.Button("StopImmediately")) {
                _go.StopImmediately();
            }
            if (GUILayout.Button("IsActive")) {
                Debug.LogError("IsActive: " + _go.IsActive);
            }
            if(GUI.changed){
                EditorExtensions.SetDirty(_go);
            }
        }
        private bool Compare<T>(T[] a1, T[] a2) where T : class{
            if(a1.Length!=a2.Length) return false;
            for(int i = 0; i < a1.Length; i++){
                if(a1[i] != a2[i])
                    return false;
            }
            return true;
        }
    }
}