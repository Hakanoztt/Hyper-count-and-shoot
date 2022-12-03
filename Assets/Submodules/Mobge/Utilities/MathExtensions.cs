using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public static class MathExtensions {
        public const float Epsilon = 0.0001f;
        public static float DeltaDegree(float degree) {
            var d = Mathf.Repeat(degree, 360);
            if(d > 180f) {
                return d - 360f;
            }
            return d;
        }
        public static float DeltaRadian(float angle) {
            var d = Mathf.Repeat(angle, Mathf.PI * 2);
            if (d > Mathf.PI) {
                return d - Mathf.PI * 2;
            }
            return d;
        }
        public static float CalculateLerpAmount(float targetRateFor60Fps, float deltaTime) {

            return 1 - Mathf.Pow(1 - targetRateFor60Fps, deltaTime * 60);

        }
        public static bool ApproximatelyClose(float a, float b) {
            float c;
            float d;
            if (a > b) {
                c = a;
                d = b;
            }
            else {
                c = b;
                d = a;
            }
            return c - Epsilon < d;
            // Mathf.Approximately => return (double) Mathf.Abs(b - a) < (double) Mathf.Max(1E-06f * Mathf.Max(Mathf.Abs(a), Mathf.Abs(b)), Mathf.Epsilon * 8f);
        }
        public static bool ApproximatelyClose(Vector3 a, Vector3 b) {
            return ApproximatelyClose(a.x, b.x) && ApproximatelyClose(a.y, b.y) && ApproximatelyClose(a.z, b.z);
        }
        public static bool LayerMaskContains(LayerMask layerMask, int objectLayer) {
            return layerMask == (layerMask | (1 << objectLayer));
        }
    }
}