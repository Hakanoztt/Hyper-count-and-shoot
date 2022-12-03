using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public static class CameraEditorExtensions {
        public static Ray CameraToWorldPointRay(this Camera camera, Vector3 worldPoint) {
            var viewPortPoint = camera.WorldToViewportPoint(worldPoint);
            return camera.ViewportPointToRay(viewPortPoint);
        }
    }
}