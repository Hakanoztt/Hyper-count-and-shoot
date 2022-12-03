using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR

namespace Mobge.EditorUtilities {
    public class GizmoLineDrawer : MonoBehaviour {

        public Pair<Transform>[] transformLines;
        public Pair<Vector3>[] pointLines;
        public Color color = new Color(1, 1, 1, 1);

        private void OnDrawGizmos() {
            var tColor = Gizmos.color;
            Gizmos.color = color;

            if (transformLines != null) {
                for(int i = 0; i < transformLines.Length; i++) {
                    var l = transformLines[i];
                    if(l.p1 != null && l.p2 != null) {
                        Gizmos.DrawLine(l.p1.position, l.p2.position);
                    }
                }
            }
            if (pointLines != null) {
                for(int i = 0; i < pointLines.Length; i++) {
                    var p = pointLines[i];
                    Gizmos.DrawLine(p.p1, p.p2);
                }
            }

            Gizmos.color = tColor;
        }

        [Serializable]
        public struct Pair<T> {
            public T p1, p2;
        }
    }
}
#endif