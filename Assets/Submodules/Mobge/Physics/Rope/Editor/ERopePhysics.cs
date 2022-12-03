using System;
using System.Collections;
using System.Collections.Generic;
using Mobge;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RopePhysics))]
public class ERopePhysics : Editor {
    private RopePhysics r;
    private void OnEnable() {
        r = (target as RopePhysics);
    }
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        if (r == null) return;;    
        if (GUILayout.Button("Reconstruct")) {
            r.Construct(r.linkCount);
        }
    }
    
}
