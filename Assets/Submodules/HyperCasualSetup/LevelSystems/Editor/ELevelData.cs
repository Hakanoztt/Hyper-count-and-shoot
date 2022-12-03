using Mobge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Mobge.HyperCasualSetup
{
    [CustomEditor(typeof(LevelData))]
    public class ELevelData : Editor
    {
        private LevelData _go;
        private int _levelIndex = -1, _worldIndex = -1;

        private void OnEnable() {
            _go = target as LevelData;
        }
        public override void OnInspectorGUI() {
            if (!_go) {
                return;
            }
            if (_go.worlds == null) {
                _go.worlds = new List<LevelData.World>();
            }
            EditorLayoutDrawer.CustomListField<LevelData.World>("worlds", _go.worlds, (layout, t) => {
                WorldField(layout, t);
                return t;
            }, ref _worldIndex);

            if (GUI.changed) {
                EditorExtensions.SetDirty(_go);
            }
        }
        private void WorldField(LayoutRectSource layout, LevelData.World world) {
            if (world.levels == null) {
                world.levels = new List<ALevelSet.AddressableLevel>();
            }
            layout.Indent++;
            world.name = EditorGUI.TextField(layout.NextRect(), "name", world.name);
            EditorDrawer.CustomListField(layout, world.name, world.levels, (l, t) => {
                t.SetEditorAsset(EditorDrawer.ObjectField(l, "level", t.editorAsset, false));
                return t;
            }, ref _levelIndex);
            layout.Indent--;
        }
    }
}