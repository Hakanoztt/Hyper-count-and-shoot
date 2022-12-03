using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(ComponentFix), true)]
public class EComponentFix : Editor {
    public ComponentFix _fix;
    protected void OnEnable() {
        _fix = target as ComponentFix;
    }
    
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        if (GUILayout.Button("Show Fixables")) {
            _fix.ShowFixables();
        }
        if (GUILayout.Button("Do Fix")) {
            _fix.DoFix();
        }
    }
}
