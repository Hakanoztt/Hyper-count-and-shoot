using UnityEngine;

namespace Mobge {
    public static class UnityExtensions {
        public static void DestroyAllChildren(this Transform mb) {
            if (mb == null || mb.childCount<1) return;

            for(int i = mb.childCount - 1; i >= 0; i--) {
                DestroyObj(mb.GetChild(i).gameObject);
            }
        }
        public static void DestroyObj(this UnityEngine.Object mb, UnityEngine.Object o) {
            DestroyObj(o);
        }
        public static void DestroySelf(this UnityEngine.Object mb) {
            DestroyObj(mb);
        }
        public static void DestroyObj(UnityEngine.Object o) {
            if (Application.isEditor && !Application.isPlaying) 
                UnityEngine.Object.DestroyImmediate(o);
            else 
                UnityEngine.Object.Destroy(o);
        }
        public static void Look2D(this Transform @this, bool right){
            var scl = @this.localScale;
            if(scl.x > 0 != right) {
                scl.x = -scl.x;
                @this.localScale = scl;
            }
        }
        public static Vector3 GetPosition(in this Matrix4x4 matrix) {
            return new Vector3(matrix.m03, matrix.m13, matrix.m23);
        }
        public static void SetPosition(ref this Matrix4x4 matrix, Vector3 position) {
            matrix.m03 = position.x; matrix.m13 = position.y; matrix.m23 = position.z;
        }
        public static Vector3 PositionOnZ(ref this Ray ray, float z) {
            var difz = z - ray.origin.z;
            var mul = difz / ray.direction.z;
            return ray.origin + ray.direction * mul;
        }

        /// <summary>
        /// This method is necessary because unity version of it does not work for prefab objects. </summary>
        public static T GetComponentInParent<T>(Transform target) where T : class {
            while (true) {
                if (target == null) {
                    return null;
                }
                T t = target.GetComponent<T>();
                if (t != null) {
                    return t;
                }
                target = target.parent;
            }
        }
        
        /// <summary>
        /// This method is necessary because unity version of it does not work for prefab objects. </summary>
        public static T GetComponentInChildren<T>(Transform target) where T : class {
            if (target == null) {
                return null;
            }
            T t = target.GetComponent<T>();
            if (t != null) {
                return t;
            }
            for (int i = 0; i < target.childCount; i++) {
                t = GetComponentInChildren<T>(target.GetChild(i));
                if (t != null) {
                    return t;
                }
            }
            return null;
        }
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component {
            var t = go.GetComponent<T>();
            return t ? t : go.AddComponent<T>();
        }
    } 
}