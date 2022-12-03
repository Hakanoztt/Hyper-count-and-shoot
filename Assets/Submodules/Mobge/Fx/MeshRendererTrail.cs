using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Fx {
    public class MeshRendererTrail : MonoBehaviour {
        public MeshRenderer target;
        public float delayTime;
        public Material materialOverride;
        [SerializeField]
        private MeshTrail _renderData;
        protected void Awake() {
            _renderData.Initialize(target.GetComponent<MeshFilter>().sharedMesh, materialOverride ? materialOverride : target.sharedMaterial);
        }
        protected void LateUpdate() {
            _renderData.Update(target.localToWorldMatrix);
        }
    }
}