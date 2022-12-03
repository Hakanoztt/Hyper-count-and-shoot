using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.UI {
    [RequireComponent(typeof(Canvas))]
    public class CanvasLookAtCamera : MonoBehaviour {
        private Canvas _canvas;

        private void Start() {
            _canvas = GetComponent<Canvas>();
            if (_canvas.worldCamera == null) {
                _canvas.worldCamera = Camera.main;
            }


        }
        protected void Update() {
            transform.forward =_canvas.worldCamera.transform.forward;
        }
    }
}
