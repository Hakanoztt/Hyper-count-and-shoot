using UnityEngine;

namespace Mobge {
    public class ShaderTest : MonoBehaviour {
        public ComputeShader shader;
        public Vector2Int textureSize;
        [OwnComponent] public new Renderer renderer;

        private RenderTexture _renderTexture;

        private int _mainFunction;
        protected void Awake() {
            _renderTexture = NewTexture(textureSize);

            InitShader();
            InitRenderer();
        }

        private void InitRenderer() {
            renderer.material.mainTexture = _renderTexture;
        }

        private void InitShader() {

            _mainFunction = shader.FindKernel("CSMain");
            shader.SetTexture(_mainFunction, "Result", _renderTexture);
        }

        private RenderTexture NewTexture(Vector2Int size) {
            var t = new RenderTexture(size.x, size.y, 24);
            t.enableRandomWrite = true;
            t.format = RenderTextureFormat.ARGBFloat;
            t.Create();
            Debug.Log(t.format);
            return t;
        }

        protected void Update() {
            shader.Dispatch(_mainFunction, _renderTexture.width / 8, _renderTexture.height / 8, 1);
        }

    }
}