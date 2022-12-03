using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mobge;
using Mobge.Animation;

namespace Mobge.Core.Components
{
    [System.Serializable]
    public class Movement3D
    {
        public AnimationVectorData positionData;
        [HideInInspector] public float totalTime;
        public AnimationVectorData rotationData;
#if UNITY_EDITOR
        public void EditorInit(Vector3 pos, Vector3 eulerAngles, Vector3 scale, bool force = false)
        {
            if (positionData == null || force)
            {
                positionData = new AnimationVectorData();
                rotationData = new AnimationVectorData();
            }
            if (!positionData.isReady || force)
            {
                positionData.Init(pos);
                rotationData.Init(eulerAngles);
            }
        }
#endif
        [System.Serializable]
        public class AnimationData
        {
            public Mobge.Animation.Curve[] curves;
            public bool isReady => curves != null && curves.Length > 0;
        }
        [System.Serializable]
        public class AnimationVectorData : AnimationData
        {
            public void Init(Vector3 v)
            {
                curves = new Curve[3];
                curves[0] = new Curve(new Mobge.Animation.Keyframe[] { new Mobge.Animation.Keyframe(0, v.x, 0, 0) });
                curves[1] = new Curve(new Mobge.Animation.Keyframe[] { new Mobge.Animation.Keyframe(0, v.y, 0, 0) });
                curves[2] = new Curve(new Mobge.Animation.Keyframe[] { new Mobge.Animation.Keyframe(0, v.z, 0, 0) });
            }
            public Vector3 Evaluate(float time)
            {
                return new Vector3(curves[0].ToAnimationCurve().Evaluate(time), curves[1].ToAnimationCurve().Evaluate(time), curves[2].ToAnimationCurve().Evaluate(time));
            }
            public void Update(Vector3 v, float time)
            {
                var xCurve = curves[0].ToAnimationCurve();
                xCurve.AddKey(time, v.x);
                curves[0].UpdateKeys(xCurve);

                var yCurve = curves[1].ToAnimationCurve();
                yCurve.AddKey(time, v.y);
                curves[1].UpdateKeys(yCurve);

                var zCurve = curves[2].ToAnimationCurve();
                zCurve.AddKey(time, v.z);
                curves[2].UpdateKeys(zCurve);
            }
        }
        //[System.Serializable]
        //public class AnimationRotationData : AnimationData
        //{
        //    public void Init(Vector3 euler)
        //    {
        //        curves = new Curve[3];
        //        curves[0] = new Curve(new Mobge.Animation.Keyframe[] { new Mobge.Animation.Keyframe(0, q.x, 0, 0) });
        //        curves[1] = new Curve(new Mobge.Animation.Keyframe[] { new Mobge.Animation.Keyframe(0, q.y, 0, 0) });
        //        curves[2] = new Curve(new Mobge.Animation.Keyframe[] { new Mobge.Animation.Keyframe(0, q.z, 0, 0) });
        //        curves[3] = new Curve(new Mobge.Animation.Keyframe[] { new Mobge.Animation.Keyframe(0, q.w, 0, 0) });
        //    }
        //    public Quaternion Evaluate(float time)
        //    {
        //        return new Quaternion(curves[0].ToAnimationCurve().Evaluate(time), curves[1].ToAnimationCurve().Evaluate(time), curves[2].ToAnimationCurve().Evaluate(time), curves[3].ToAnimationCurve().Evaluate(time));
        //    }
        //    public void Update(Quaternion q, float time)
        //    {
        //        var xCurve = curves[0].ToAnimationCurve();
        //        xCurve.AddKey(time, q.x);
        //        curves[0].UpdateKeys(xCurve);

        //        var yCurve = curves[1].ToAnimationCurve();
        //        yCurve.AddKey(time, q.y);
        //        curves[1].UpdateKeys(yCurve);

        //        var zCurve = curves[2].ToAnimationCurve();
        //        zCurve.AddKey(time, q.z);
        //        curves[2].UpdateKeys(zCurve);

        //        var wCurve = curves[3].ToAnimationCurve();
        //        wCurve.AddKey(time, q.w);
        //        curves[3].UpdateKeys(wCurve);
        //    }
        //}
    }
}