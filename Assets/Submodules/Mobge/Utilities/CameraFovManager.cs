using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Mortar {
    [ExecuteInEditMode]
    public class CameraFovManager : MonoBehaviour {
        [OwnComponent] public new Camera camera;
        public Vector2 referenceFovs = new Vector2(60, 60);
        public Mode mode;
        [Range(0, 1)] public float widthHeightRate;

        protected void LateUpdate() {
            if (!camera) {
                return;
            }
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            var rf = referenceFovs * (Mathf.Deg2Rad * 0.5f);
            Vector2 fovRatio = new Vector2(Mathf.Tan(rf.x), Mathf.Tan(rf.y));

            float scaleX = screenSize.x / fovRatio.x;
            float scaleY = screenSize.y / fovRatio.y;
            // float scaleY = 1f;
            float wh;
            switch (mode) {
                default:
                case Mode.MatchWidthOrHeight:
                    wh = widthHeightRate;
                    break;
                case Mode.FinInside:
                    if(scaleX > scaleY) {
                        wh = 1;
                    }
                    else {
                        wh = 0;
                    }
                    break;
                case Mode.FitOutside:
                    if (scaleX > scaleY) {
                        wh = 0;
                    }
                    else {
                        wh = 1;
                    }
                    break;
            }
            float hR = fovRatio.x / screenSize.x * screenSize.y;

            float fov = Mathf.LerpUnclamped(hR, fovRatio.y, wh);
            this.camera.fieldOfView = Mathf.Atan(fov) * (Mathf.Rad2Deg * 2);
        }

        public enum Mode {
            MatchWidthOrHeight = 0,
            FinInside,
            FitOutside
        }
    }
}