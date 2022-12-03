using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using static Mobge.HyperCasualSetup.RoadGenerator.RoadGeneratorComponent;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    [CustomEditor(typeof(BasicRoadPiece))]
    public class EBasicRoadPiece : Editor {
        private BasicRoadPiece _go;

        private RoadTracker _enumerator;
        private float _moveStep = 0.1f;

        private bool _editing;

        private void OnEnable() {
            _go = (BasicRoadPiece)target;
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (!_go) {
                return;
            }
            if (CanHaveBeziers) {
                _moveStep = EditorGUILayout.FloatField("move step", _moveStep);
                _moveStep = Mathf.Max(_moveStep, 0.002f);
                _editing = EditorGUILayout.Toggle("editing", _editing);
            }
            if (_go.endPoints != null) {
                for (int i = 0; i < _go.endPoints.Length; i++) {
                    var e = _go.endPoints[i];
                    if(!IsValid(e.rotation)) {
                        _go.endPoints[i].rotation = Quaternion.identity;
                        GUI.changed = true;
                    }
                }
            }
            if (GUI.changed) {
                EditorExtensions.SetDirty(_go);
            }
        }
        private bool IsValid(Quaternion q) {
            var sqr = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
            return sqr > 0.999f && sqr < 1.001f;
        }
        private bool CanHaveBeziers {
            get {
                if (!_go) return false;
                if (_go.endPoints == null || _go.endPoints.Length < 2) return false;
                return true;
            }
        }
        public static RoadTracker NewEnumerator(BasicRoadPiece data, Pose pose, int repeatCount = 1) {

            var roadGenerator = new Data();
            var rPos = data.endPoints[0].GetTransformedBy(pose);
            roadGenerator.position = rPos.position;
            roadGenerator.rotation = Data.Reverse(rPos.rotation);
            ComponentRef cr = new ComponentRef();
            cr.res = data;
            roadGenerator.prefabReferences = new ComponentRef[] { cr };
            roadGenerator.InitReferences();
            roadGenerator.defaultCubicness = data.roadCubicness;
            Item item;
            item.id = 0;
            item.flipZ = false;
            item.instance = null;
            roadGenerator.items = new Item[repeatCount];
            for (int i = 0; i < repeatCount; i++) {
                roadGenerator.items[i] = item;
            }

            return roadGenerator.NewTracker();
        }
        public static RoadTracker NewEnumerator(BasicRoadPiece data, int repeatCount = 1) {
            return NewEnumerator(data, new Pose(data.transform.position, data.transform.rotation), repeatCount);
        }

        private void OnSceneGUI() {

            if (CanHaveBeziers) {
                if (_editing) {
                    var e = NewEnumerator(_go, 3);
                    e.MoveForward(0);
                    var p = e.Current;
                    int index = 0;
                    int stop = 0;
                    while (e.MoveForwardSmallAmount(_moveStep) || stop++ == 0) {
                        var pn = e.Current;
                        Handles.color = index % 2 == 0 ? Color.black : Color.white;
                        Handles.DrawLine(p.position, pn.position);
                        Handles.DrawLine(pn.position, pn.position + pn.up);
                        index++;
                        p = pn;
                    }
                    if (_enumerator.IsValid) {
                        var pose = _enumerator.Current;
                        Handles.DrawWireDisc(pose.position, pose.rotation * Vector3.forward, 0.3f);
                    }
                }
            }
        }
    }
}