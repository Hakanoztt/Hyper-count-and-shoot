
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Fx
{
    public class LiquidSimulationDebugger : MonoBehaviour
    {
        public ComputeShader shader;
        public LiquidSimulationComponent.Data data;
        public float currentCount;

        private void Update() {
            data.SetConstants(shader);
            currentCount = data.ParticleCount;
        }
    }
}


#endif