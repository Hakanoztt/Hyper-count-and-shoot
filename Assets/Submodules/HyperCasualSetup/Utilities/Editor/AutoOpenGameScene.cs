using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Mobge.Game {
    public class AutoOpenGameScene {
        [InitializeOnLoadMethod]
        private static void Doit() {
            EditorApplication.delayCall += () => {
                if (!string.IsNullOrWhiteSpace(SceneManager.GetActiveScene().path)) return;
                var assets = AssetDatabase.FindAssets("t:Scene GameScene");
                EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(assets[0]));
            };
        }
    }
}
