using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public static class RectUtilities {
        /// <summary>
        /// (<see cref="Rect.xMin"/>, <see cref="Rect.yMin"/>)
        /// </summary>
        public static Vector2 TopLeft(this Rect rect) {
            return new Vector2(rect.xMin, rect.yMin);
        }
        /// <summary>
        /// (<see cref="Rect.xMin"/>, <see cref="Rect.yMax"/>)
        /// </summary>
        public static Vector2 BottomLeft(this Rect rect) {
            return new Vector2(rect.xMin, rect.yMax);
        }
        /// <summary>
        /// (<see cref="Rect.xMax"/>, <see cref="Rect.yMin"/>)
        /// </summary>
        public static Vector2 TopRight(this Rect rect) {
            return new Vector2(rect.xMax, rect.yMin);
        }
        /// <summary>
        /// (<see cref="Rect.xMax"/>, <see cref="Rect.yMax"/>)
        /// </summary>
        public static Vector2 BottomRight(this Rect rect) {
            return new Vector2(rect.xMax, rect.yMax);
        }
        public static Vector2 UniformToRect(this Rect rect, Vector2 uniformPoint) {
            return rect.position + Vector2.Scale(rect.size, uniformPoint);
        }
        public static Vector2 RectToUniform(this Rect rect, Vector2 worldPoint) {
            var relative = worldPoint - rect.position;
            relative.x /= rect.width;
            relative.y /= rect.height;
            return relative;
        }
    }
}