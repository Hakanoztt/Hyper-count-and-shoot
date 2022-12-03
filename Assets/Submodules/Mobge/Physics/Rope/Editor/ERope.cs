using System;
using System.Collections;
using System.Collections.Generic;
using Mobge;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Rope))]
public class ERope : Editor {
    private Rope r;
    private void OnEnable() {
        r = (target as Rope);
    }
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        if (r == null) return;;    
        r.LinkCount = EditorGUILayout.DelayedIntField("Link Count", r.LinkCount);
        r.Thickness = EditorGUILayout.DelayedFloatField("Thickness", r.Thickness);
        if (GUILayout.Button("Reconstruct")) {
            r.Reconstruct(r.LinkCount);
        }
    }
    
}
