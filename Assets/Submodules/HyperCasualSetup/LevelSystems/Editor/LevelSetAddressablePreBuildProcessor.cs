using System.Collections;
using System.Collections.Generic;
using Mobge;
using Mobge.Core;
using Mobge.HyperCasualSetup;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Mobge.HyperCasualSetup {
    public static class LevelSetAddressablePreBuildProcessor {
        [InitializeOnLoadMethod]
        private static void Initialize() {
            BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerHandler);
        }
        private static void BuildPlayerHandler(BuildPlayerOptions options) {
            if (EditorUtility.DisplayDialog("Build with Addressables",
                "Do you want to build a clean addressables before export?\n \n" +
                "This will not work if there is no addressable assets folder.",
                "Build with Addressables", "Skip"))
            {
                PreExport();
            }
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
        }
        public static void PreExport() {
            Debug.Log($"{nameof(LevelSetAddressablePreBuildProcessor)}.{nameof(PreExport)} start");
            CleanAndMarkStuffAddressable();
            AddressableFixer.BuildAddressables();
            Debug.Log($"{nameof(LevelSetAddressablePreBuildProcessor)}.{nameof(PreExport)} done");
        }
        [MenuItem("Mobge/Prepare Addressables For Build")]
        public static void PrepareForBuildButtonOnMenu() {
            PreExport();
        }
        // [MenuItem("Mobge/Addressables/Deep Fix Addressable")]
        // public static void FixAddressableButtonOnMenu() {
        //     Debug.Log($"{nameof(LevelSetAddressablePreBuildProcessor)}.{nameof(FixAddressableButtonOnMenu)} start");
        //     CleanAndMarkStuffAddressable();
        //     Debug.Log($"{nameof(LevelSetAddressablePreBuildProcessor)}.{nameof(FixAddressableButtonOnMenu)} done");
        // }
        private static void CleanAndMarkStuffAddressable() {
            AddressableFixer.CleanAddressables();

            var sceneCount = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++) {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                try {
                    var roots = scene.GetRootGameObjects();
                    foreach (var root in roots) {
                        var contexts = root.GetComponentsInChildren<GameContext>();

                        foreach (var context in contexts) {
                            if (context.menuManagerRes.editorAsset != null) {
                                AddressableFixer.MarkAssetAddressable(context.menuManagerRes.editorAsset);
                            }
                            foreach(var ins in context.instantiateOnAwake) {
                                if (ins.editorAsset != null) {
                                    AddressableFixer.MarkAssetAddressable(ins.editorAsset);
                                }
                            }
                            var levelSet = context.LevelData as LevelSet;
                            if (levelSet != null) {
                                AddressableFixer.BulkMarkLevelsAndLevelResourcesAddressable(new LevelSet.LevelDataEnumerable(levelSet.GetAllLevels()));
                                if (levelSet.extraAddressables != null) {
                                    for (int j = 0; j < levelSet.extraAddressables.Length; j++) {
                                        var asset = levelSet.extraAddressables[j].editorAsset;
                                        if (asset is Level level) {
                                            AddressableFixer.MarkLevelAndLevelResourcesAddressable(level);
                                        }
                                        else {
                                            AddressableFixer.MarkAssetAddressable(levelSet.extraAddressables[j].editorAsset);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                finally {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            
        }
    }
}

