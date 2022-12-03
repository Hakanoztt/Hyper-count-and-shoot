using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.UI
{
    public class CanvasCameraSetter : MonoBehaviour
    {
        [OwnComponent(true)]
        public Canvas canvas;
        private void Awake() {
            canvas.worldCamera = Camera.main;
        }
    }
}