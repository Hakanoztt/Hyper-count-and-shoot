#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Mobge.Core;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mobge {
    public static class AddressableFixer {
        private static readonly HashSet<Object> AddressableObjects = new HashSet<Object>();
        private static string AddressablePath => $"Assets{Path.DirectorySeparatorChar}AddressableAssetsData";
        public static void BulkMarkLevelsAndLevelResourcesAddressable(IEnumerable<Level> levels) {
            AddressableObjects.Clear();
            var e = levels.GetEnumerator();
            while (e.MoveNext()) {
                MarkPieceResourcesAddressable(e.Current);
                MarkAssetAddressable(e.Current, true);
            }
            e.Dispose();
        }
        public static void MarkLevelAndLevelResourcesAddressable(Level level) {
            AddressableObjects.Clear();
            MarkPieceResourcesAddressable(level);
            MarkAssetAddressable(level, true);
        }
        public static void CleanAddressables(){
            ProperSave.DoProperSave();
            try { UnityDeleteRecursivelyFromAssetDatabase(AddressablePath); } catch { /* ignored */ }
            try { Directory.Delete(AddressablePath, true); } catch { /* ignored */ }
            ProperSave.DoProperSave();
        }
        public static void BuildAddressables() {
            AddressableAssetSettings.CleanPlayerContent();
            AddressableAssetSettings.BuildPlayerContent();
            ProperSave.DoProperSave();
        }
        private static void MarkPieceResourcesAddressable(Piece piece) {
            if (AddressableObjects.Contains(piece)) return;
            if (piece == null) return;
            var e = piece.Components.GenericEnumerator();
            while (e.MoveNext()) {
                var d = e.Current;
                if (d == null) continue;
                try {
                    var o = d.GetObject();
                    if (o is IResourceOwner ro) {
                        for (int i = 0; i < ro.ResourceCount; i++) {
                            var res = ro.GetResource(i);
                            if (res.editorAsset) {
                                MarkAssetAddressable(res.editorAsset);
                            }
                        }
                    }
                    if (o is PieceComponent.Data pieceComponent) {
                        MarkPieceResourcesAddressable(pieceComponent.piece);
                    }
                }
                catch (Exception exception) {
                    Debug.LogError($"Exception on {nameof(AddressableFixer)}: Something wrong on piece: {piece.name}");
                    Console.WriteLine(exception);
                    throw;
                }
            }
            var level = piece as Level;
            if (level != null) {
                var decorationSetReference = level.decorationSet;
                if (decorationSetReference != null) {
                    var dsEditorAsset = decorationSetReference.editorAsset;
                    if (dsEditorAsset != null) {
                        MarkAssetAddressable(dsEditorAsset);
                    }
                }
            }
            AddressableObjects.Add(piece);
        }
        public static void MarkAssetAddressable(Object asset, bool forceDo = false) {
            if (!forceDo && AddressableObjects.Contains(asset)) return;
            var settings = GetDefaultAddressableAssetSettings();
            var group = settings.DefaultGroup;
            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath)) return;
            Debug.Log(assetPath);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = assetPath;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, asset, true);
            AddressableObjects.Add(asset);
        }
        private static AddressableAssetSettings GetDefaultAddressableAssetSettings() {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null) return settings;
            settings = AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, false, true);
            AddressableAssetSettingsDefaultObject.Settings = settings;
            var defaultGroup = settings.DefaultGroup;
            AssetDatabase.SaveAssets();
            return settings;
        }
        private static void UnityDeleteRecursivelyFromAssetDatabase(string path) {
            var files = Directory.GetFiles(path);
            foreach (var file in files) {
                if (file.EndsWith("meta")) continue;
                AssetDatabase.DeleteAsset(file);
            }            
            var directories = Directory.GetDirectories(path);
            foreach (var directory in directories) {
                UnityDeleteRecursivelyFromAssetDatabase(directory);
            }
        }
    }
}
#endif
