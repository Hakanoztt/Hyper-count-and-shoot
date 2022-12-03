using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Test {
    public class FPSScrambler : MonoBehaviour {

        public AnimationCurve fpsWaveCurve;

        protected void Start() {
            fpsWaveCurve.postWrapMode = WrapMode.Loop;
        }

#if UNITY_EDITOR
        void Update() {
            float targetFps = fpsWaveCurve.Evaluate(Time.time);
            Application.targetFrameRate = (int)targetFps;
        }
#endif
        
    }
}