using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    [CustomEditor(typeof(RoadPieceGenerator))]
    public class ERoadPieceGenerator : Editor {

        private RoadPieceGenerator _go;

        private void OnEnable() {
            _go = target as RoadPieceGenerator;
        }

        public override void OnInspectorGUI() {
            if (_go == null) {
                return;
            }

            _go.pieceMesh = EditorLayoutDrawer.ObjectField("piece mesh", _go.pieceMesh);
            EditorLayoutDrawer.CustomArrayField("piece materials", ref _go.pieceMaterials, (l, i) => {
                _go.pieceMaterials[i] = EditorDrawer.ObjectField(l, i.ToString(), _go.pieceMaterials[i]);
            });

            _go.straightRoadLength = EditorGUILayout.FloatField("straigt piece length", _go.straightRoadLength);




            if (GUI.changed) {
                EditorExtensions.SetDirty(_go);
            }
        }

        private class PreviewBehaviour : MonoBehaviour {
            private static PreviewBehaviour _instance;
            public static PreviewBehaviour Instance {
                get {
                    if(_instance == null) {
                        _instance = FindObjectOfType<PreviewBehaviour>();
                        if (_instance == null) {
                            _instance = new GameObject("road generator instance").AddComponent<PreviewBehaviour>();
                        }
                        _instance.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    }
                    return _instance;
                }
            }
        }
    }
}