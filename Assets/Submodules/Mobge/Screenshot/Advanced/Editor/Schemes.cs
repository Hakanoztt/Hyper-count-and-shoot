using System;
using UnityEngine;

namespace Mobge.AdvancedScreenshot
{
    public class Schemes : ScriptableObject
    {

        public KeyCode pauseKey = KeyCode.P;
        public KeyCode screenshotKey = KeyCode.O;
        public Scheme[] schemes;
        public int selectedSchemeIndex;

        public Scheme selectedScheme {
            get {
                return Get(schemes, selectedSchemeIndex);
            }
        }

        private T Get<T>(T[] array, int index) {
            if(array == null) {
                return default(T);
            }
            if(index< 0 || index >= array.Length) {
                return default(T);
            }
            return array[index];
        }


        public ResolutionOption[] resolutionOptions;
        public int selectedResolutionIndex;


        public ResolutionOption selectedResolution {
            get {
                return Get(resolutionOptions, selectedResolutionIndex);
            }
        }


        [Serializable]
        public class Scheme
        {
            public string name;
            public CameraLimits[] limits;

            public bool hasCameraPosition;
            public Vector3 cameraPos;
            public bool disableCanvasses;

            public override string ToString() {
                return name;
            }
        }

        [Serializable]
        public class CameraLimits
        {
            public float far, near;
            public Plane worldPlane;
            public CameraLimitType limitType;
            public string name;
            public int selectedResolution;
            public bool dontUseAlpha;

            public CameraLimits() {
                worldPlane = new Plane(new Vector3(0, 0, -1), 0);
            }

            public void apply(Camera cam) {
                switch (limitType) {
                    case CameraLimitType.RelativeToCamera:
                        cam.nearClipPlane = near;
                        cam.farClipPlane = far;
                        break;
                    case CameraLimitType.RelativeToWorldPlane:

                        float offset = worldPlane.GetDistanceToPoint(cam.transform.position);

                        cam.nearClipPlane = near + offset;
                        cam.farClipPlane = far + offset;
                        break;
                    default:
                        break;
                }
                if (!dontUseAlpha) {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                }
            }
        }
        [Serializable]
        public class ResolutionOption
        {
            public int height;
            public int width;
            public string name;
            public override string ToString() {
                return name;
            }
        }

        public enum CameraLimitType
        {
            RelativeToCamera = 0,
            RelativeToWorldPlane = 1,
        }

    }
}