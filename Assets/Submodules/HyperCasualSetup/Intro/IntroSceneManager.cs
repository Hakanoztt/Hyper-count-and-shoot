using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Mobge.HyperCasualSetup {
    public class IntroSceneManager : MonoBehaviour {
        [OwnComponent(true)] public VideoPlayer introVideoPlayer;
        public Sprite fallbackImage;
        public float fallbackImageDisplaySecond;
        private AsyncOperation loadSceneOp;
        public void Start() {
            introVideoPlayer.loopPointReached += NextScene;
            introVideoPlayer.errorReceived += delegate(VideoPlayer source, string message) 
            {
                Debug.LogWarning("[VideoPlayer] Play Movie Error: " + message);
                
                if (fallbackImage)
                    Fallback();
                else
                    NextScene();
            };
            var currentIndex = SceneManager.GetActiveScene().buildIndex;
            var nextIndex = currentIndex + 1;
            loadSceneOp = SceneManager.LoadSceneAsync(nextIndex, LoadSceneMode.Single);
            loadSceneOp.allowSceneActivation = false;
        }
        private void NextScene(UnityEngine.Video.VideoPlayer source) {
            loadSceneOp.allowSceneActivation = true;
        }

        private void NextScene()
        {
            loadSceneOp.allowSceneActivation = true;
        }

        private void Fallback()
        {
            var image = new GameObject("Fallback Image").AddComponent<SpriteRenderer>();
            image.sprite = fallbackImage;
            
            var tr = image.transform;
            tr.SetParent(transform);
            tr.localPosition = new Vector3(0, 0, tr.localPosition.z);
            
            Invoke("NextScene", fallbackImageDisplaySecond);
        }
    }
}