using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.AnimatedValues;

namespace Mobge.HyperCasualSetup.UI.ChallengeSystem {
    [CustomEditor(typeof(ChallengeData))]
    public class EChallengeData : Editor {
        private ChallengeData _go;

        private int _testTime;
        private int _maxTime = 1000;

        private AnimBool _graphics = new AnimBool();

        private static LayoutRectSource s_layoutRect = new LayoutRectSource();

        private void OnEnable() {
            _go = target as ChallengeData;
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (_go == null) {
                return;
            }
            if(_go.levels == null) {
                _go.levels = new ChallengeData.Level[0];
            }
            _testTime = EditorGUILayout.IntField("Test Time", _testTime);
            ErrorsField();
            GraphicsField();
            
            if (GUI.changed) {
                _go.UpdateInternalStructure();
                EditorExtensions.SetDirty(_go);
            }
        }

        private void ErrorsField() {
            bool broken = false;
            for(int i = 0; i < _go.levels.Length; i++) {
                var l = _go.levels[i];
                if (l.intervals == null) {
                    l.intervals = new ChallengeData.Interval[0];
                }
                else {
                    if (l.intervals.Length > 1) {
                        var end = l.intervals[0].End;
                        for (int j = 1; j < l.intervals.Length; j++) {
                            var interval = l.intervals[j];
                            if(interval.start < end) {
                                broken = true;
                            }
                            end = interval.End;
                        }
                    }
                }
                _go.levels[i] = l;
            }

            if (broken) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Interval error");
                if(GUILayout.Button("Try auto fix")) {
                    for(int i = 0; i < _go.levels.Length; i++) {
                        var l = _go.levels[i];
                        Array.Sort(l.intervals, ChallengeData.s_intervalComparer);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }


        private void GraphicsField() {
            _graphics.target = EditorGUILayout.Foldout(_graphics.target, "Graphics", true);
            if (_graphics.isAnimating) {
                Repaint();
            }
            if (EditorGUILayout.BeginFadeGroup(_graphics.faded)) {
                try {
                    var activeLevels = _go.GetActiveLevels(_testTime);
                    while (activeLevels.MoveNext()) {
                        var c = activeLevels.Current;
                        var level = _go.levels[c];
                        EditorGUILayout.LabelField(c.ToString(), level.name);
                    }

                }
                catch (System.Exception e) {
                    Debug.LogError(e);
                    EditorGUILayout.HelpBox("An error occured while sampling time.", MessageType.Error);
                }

                _maxTime = EditorGUILayout.IntField("Max Time", _maxTime);
                _maxTime = Mathf.Max(_maxTime, 1);


                s_layoutRect.ResetInLayout(10);


                float scale = s_layoutRect.CurrentRect.width / _maxTime;

                var r = s_layoutRect.NextRect();
                EditorGUI.DrawRect(r, new Color(0, 0, 1, 0.5f));
                EditorGUI.LabelField(r, 0.ToString());
                var gcr = new GUIContent(((int)(r.width / scale)).ToString());
                var size = GUI.skin.label.CalcSize(gcr);
                var rr = r;
                rr.x += rr.width - size.x;
                EditorGUI.LabelField(rr, gcr);

                float minY = s_layoutRect.AbsoluteHeight;
                for (int i = 0; i < _go.levels.Length; i++) {
                    var lr = s_layoutRect.NextRect(15);
                    var level = _go.levels[i];
                    for (int j = 0; j < level.intervals.Length; j++) {
                        var it = level.intervals[j];
                        float start = it.start * scale + lr.x;
                        float width = it.duration * scale;
                        Rect ir = lr;
                        ir.x = start;
                        ir.width = width;
                        EditorGUI.DrawRect(ir, new Color(0, 0, 1, 0.3f));
                    }
                }
                float maxY = s_layoutRect.AbsoluteHeight;
                float timeX = _testTime * scale + r.x;
                EditorGUI.DrawRect(new Rect(timeX - 1, minY, 2, maxY - minY), new Color(1, 0, 0, 0.5f));

                s_layoutRect.ConvertToLayout();
            }
            EditorGUILayout.EndFadeGroup();
        }
    }
}