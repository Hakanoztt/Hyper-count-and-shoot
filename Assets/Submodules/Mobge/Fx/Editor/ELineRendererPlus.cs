using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Mobge {
    [CustomEditor(typeof(LineRendererPlus))]
    public class ELineRendererPlus : Editor{
        LineRendererPlus _go;
        protected void OnEnable() {
            _go = target as LineRendererPlus;
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (_go == null) {
                return;
            }

            _go.PieceCount = EditorGUILayout.IntField("piece count", _go.PieceCount);
            _go.PieceLength = EditorGUILayout.FloatField("piece length", _go.PieceLength);
            _go.Width = EditorGUILayout.FloatField("width", _go.Width);
            _go.SpriteSet = SpriteSetField("sprite set", _go.SpriteSet);
            _go.Quality = (LineRendererPlus.QualityMode)EditorGUILayout.EnumPopup("quality", _go.Quality);
            _go.HasColors = EditorGUILayout.Toggle("has colors", _go.HasColors);
            if (_go.HasColors) {
                EditorGUI.BeginChangeCheck();
                _go.color = EditorGUILayout.GradientField("color", _go.color);
                if (EditorGUI.EndChangeCheck()) {
                    _go.RefreshColors();
                }
            }
            var poses = _go.StitchPositions;
            int newSize = EditorGUILayout.IntField("numbew of stitches", poses.Length);
            Array.Resize(ref poses, newSize);
            LayoutRectSource lr = new LayoutRectSource(GUILayoutUtility.GetAspectRect(float.PositiveInfinity));
            EditorDrawer.CustomArrayField(lr, "stitch positoins", ref poses, (layout, p) => {
                var r = layout.NextRect();
                r.xMin += 20;
                p = EditorGUI.FloatField(r, "value", p);
                return p;
            });
            lr.ConvertToLayout();
            _go.StitchPositions = poses;
            bool valid = _go.IsValuesValid(out string error);
            if (!valid) {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }
            if (GUI.changed) {
                if (valid) {
                    _go.ReconstructImmediate();
                }
                EditorExtensions.SetDirty(_go);
            }
        }
        private LineRendererPlus.Sprites SpriteSetField(string label, LineRendererPlus.Sprites spriteSet) {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField(label);
            EditorGUI.indentLevel++;
            spriteSet.top = EditorLayoutDrawer.ObjectField("top", spriteSet.top);
            spriteSet.middle = EditorLayoutDrawer.ObjectField("middle", spriteSet.middle);
            spriteSet.bottom = EditorLayoutDrawer.ObjectField("bottom", spriteSet.bottom);
            spriteSet.loopMiddle = EditorGUILayout.Toggle("loop middle", spriteSet.loopMiddle);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            return spriteSet;
        }
    }
}