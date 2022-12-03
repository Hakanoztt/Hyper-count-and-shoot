using UnityEditor;
using UnityEngine;

namespace Mobge {
    public static class HandlesExtensions{
        public static Rect RectHandle(Rect r){
            var c = r.center;
            var scl = Handles.matrix.lossyScale;
            var s = HandleUtility.GetHandleSize(c)/scl.x;
            //r.center = Handles.PositionHandle(r.center, Quaternion.identity,);
            r.center = Handles.FreeMoveHandle(c, Quaternion.identity, s/20, Vector3.zero, Handles.CircleHandleCap);
            r.yMax = Handles.Slider(v(c.x, r.yMax), new Vector3(0,1,0), s, Handles.ArrowHandleCap, 0).y;
            r.yMin = Handles.Slider(v(c.x, r.yMin), new Vector3(0,-1,0), s, Handles.ArrowHandleCap, 0).y;
            r.xMax = Handles.Slider(v(r.xMax, c.y), new Vector3(1,0,0), s, Handles.ArrowHandleCap, 0).x;
            r.xMin = Handles.Slider(v(r.xMin, c.y), new Vector3(-1,0,0), s, Handles.ArrowHandleCap, 0).x;
            return r;
        }
        private static Vector3 v(float x, float y, float z = 0){
            return new Vector3(x,y,z);
        }
        public static Pose PoseHandle(Pose pose) {
            var pos = Handles.PositionHandle(pose.position, Quaternion.identity);
            var rot = Handles.RotationHandle(pose.rotation, pose.position);
            return new Pose(pos, rot);
        }
    }
}