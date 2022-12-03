using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.War {

    [Serializable]
    public struct TeamMaterialModule {
        public MaterialPointer[] materialPointers;

        public void ApplyMaterial(Material m) {
            if (materialPointers != null) {
                for (int i = 0; i < materialPointers.Length; i++) {
                    materialPointers[i].ApplyMaterial(m);

                }
            }
        }

        [Serializable]
        public struct MaterialPointer {
            public Renderer renderer;
            public int index;

            private Material[] _sharedMaterials;

            public void ApplyMaterial(Material material) {
                if(_sharedMaterials == null) {
                    _sharedMaterials = renderer.sharedMaterials;
                }
                _sharedMaterials[index] = material;
                renderer.sharedMaterials = _sharedMaterials;
            }
        }
    }
}