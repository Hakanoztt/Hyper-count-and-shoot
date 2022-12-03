using Mobge.Graph;
using Mobge.IdleGame;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.IkeaIdle.Tools {
    public class ToolsWindow : EditorWindow {
        [MenuItem("Window/ToolsWindow")]
        public static void Init() {
            GetWindow<ToolsWindow>("Tools");
        }


        EditorFoldGroups _groups;

        private void OnEnable() {
            _groups = new EditorFoldGroups(EditorFoldGroups.FilterMode.NoFilter);
        }

        private void OnGUI() {
            _groups.GuilayoutField(CreateGroups);
        }

        private void CreateGroups(EditorFoldGroups.Group obj) {
            obj.AddChild("scale road meshes", () => {
                Line3D[] lines = Selection.GetFiltered<Line3D>(SelectionMode.Deep);
                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.LabelField("selected lines");
                for(int i=0; i < lines.Length; i++) {
                    EditorLayoutDrawer.ObjectField(lines[i]);
                }
                EditorGUILayout.EndVertical();

                Vector3 meshScaleMultiplayer = _groups.Vector3Field("mesh scale multiplayer", Vector3.one * 2);
                if(GUILayout.Button("scale meshes")) {
                    for (int i = 0; i < lines.Length; i++) {
                        lines[i].MeshScale = Vector3.Scale(lines[i].MeshScale, meshScaleMultiplayer);
                        EditorExtensions.SetDirty(lines[i]);
                    }
                }
            });
            obj.AddChild("graph test", () => {

                if (GUILayout.Button("add 100 random 1 dimensional test data")) {
                    float[] d = new float[1];
                    for (int i = 0; i < 100; i++) {
                        d[0] = UnityEngine.Random.Range(-100f, 100f);
                        GraphDataManager.Instance.AddData("random 1 dimensional test data", d);
                    }
                }
                if (GUILayout.Button("add 100 random test data")) {
                    float[] d = new float[2];
                    for (int i = 0; i < 100; i++) {
                        d[0] = UnityEngine.Random.Range(-100f, 100f);
                        d[1] = UnityEngine.Random.Range(-100f, 100f);
                        GraphDataManager.Instance.AddData("random test data", d);
                    }
                }
                if (GUILayout.Button("add 100 ordered random test data")) {
                    float[] d = new float[2];
                    float lastValue = -100f;
                    for (int i = 0; i < 100; i++) {
                        d[0] = UnityEngine.Random.Range(-100f, 100f);
                        lastValue += UnityEngine.Random.Range(0f, 4f);
                        d[1] = lastValue;
                        GraphDataManager.Instance.AddData("ordered random test data", d);
                    }
                }
            });
        }
    }
}