using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Mobge.HyperCasualSetup.Tests {
    public class GameIntegrityTests {
        private const string GAME_PREFAB_NAME = "OnEveryScene";
        [Test]
        public void BuildOnlyHasTwoScenes() {
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            Assert.AreEqual(2, sceneCount);
        }
        [Test]
        public void SceneZeroIsTelemetryInitScene() {
            var sceneName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(0));
            Assert.AreEqual("telemetry_init_scene", sceneName);
        }
        [Test]
        public void SceneOneIsGameScene() {
            var sceneName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(1));
            Assert.AreEqual("GameScene", sceneName);
        }
        [Test]
        public void GamePrefabExists() {
            var guids = AssetDatabase.FindAssets($"t:Prefab {GAME_PREFAB_NAME}");
            Assert.AreEqual(1, guids.Length, $"there should be a prefab named {GAME_PREFAB_NAME}, and there can only be one {GAME_PREFAB_NAME}");
        }
        [Test]
        public void GameSceneOnlyHasGamePrefab() {
            EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(1));
            var rootObjects = new List<GameObject>();
            var scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(rootObjects);
            Assert.AreEqual(1, rootObjects.Count);
            var oneGameObject = rootObjects[0];
            Assert.IsTrue(PrefabUtility.IsAnyPrefabInstanceRoot(oneGameObject));
            var prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(oneGameObject);
            var prefabName = Path.GetFileNameWithoutExtension(prefabAssetPath);
            Assert.AreEqual(GAME_PREFAB_NAME, prefabName);
        }
        [Test]
        public void GamePrefabDoesNotHaveAnyOverridesOnGameScene() {
            EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(1));
            var rootObjects = new List<GameObject>();
            var scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(rootObjects);
            var oneGameObject = rootObjects[0];
            var hasAnyOverrides = PrefabUtility.HasPrefabInstanceAnyOverrides(oneGameObject, false);
            Assert.IsFalse(hasAnyOverrides);
        }
        [Test]
        public void GamePrefabTransformationIsZero() {
            EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(1));
            var rootObjects = new List<GameObject>();
            var scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(rootObjects);
            var oneGameObject = rootObjects[0];
            Assert.IsTrue(oneGameObject.transform.position.magnitude < .001f);
            Assert.IsTrue(oneGameObject.transform.rotation.eulerAngles.magnitude < .001f);
            Assert.IsTrue(Vector3.Distance(oneGameObject.transform.localScale, Vector3.one) < .001f);
        }
        [Test]
        public void ThereIsGameContextInGamePrefab() {
            EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(1));
            var rootObjects = new List<GameObject>();
            var scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(rootObjects);
            var oneGameObject = rootObjects[0];
            // oneGameObject.GetComponentInChildren<GameContext>();         
            // note: referencing GameContext in this assembly requires Mobge library to be defined with asmdef structure
            // this is unacceptable for ferhat, therefore this code has left unfinished.
            Assert.Fail("test not implemented yet");
        }
        [Test]
        public void ThereIsEventSystemOnGamePrefab() {
            Assert.Fail("test not implemented yet");
        }
        [Test]
        public void ThereIsDestroyScriptOnEventSystemOnGamePrefab() {
            Assert.Fail("test not implemented yet");
        }
        [Test]
        public void ThereIsMainCameraOnGamePrefab() {
            Assert.Fail("test not implemented yet");
        }
        [Test]
        public void ThereIsAtLeastOneLevelInContext() {
            Assert.Fail("test not implemented yet");
        }
        [Test]
        public void LevelsDoesNotHaveAnyMobgeComponentWithNullDefinition() {
            Assert.Fail("test not implemented yet");
        }
        [Test]
        public void NoLevelInBuildHasPieceSpawnerWithoutPiece() {
            Assert.Fail("test not implemented yet");
        }
        [Test]
        public void NoLevelInBuildHasPrefabSpawnerWithoutPrefab() {
            Assert.Fail("test not implemented yet");
        }
        [UnityTest]
        public IEnumerator TestAllLevelsToOpenSuccessAndFail() {
            // open game scene
            // play
            // find game context
            // for each level on level context
            // open level wait 10 frames
            // destroy level
            // open level wait 10 frames 
            // level fail wait 10 frames
            // level success wait 10 frames
            // destroy level
            // stop
            for (int i = 0; i < 10; i++) {
                yield return null;
            }
            for (int i = 0; i < 10; i++) {
                yield return null;
            }
            for (int i = 0; i < 10; i++) {
                yield return null;
            }
            Assert.Fail("test not implemented yet");
        }
    }
}
