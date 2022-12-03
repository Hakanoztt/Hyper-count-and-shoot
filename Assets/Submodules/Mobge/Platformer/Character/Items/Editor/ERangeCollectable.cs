using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.Platformer.Character {
    [CustomEditor(typeof(RangeModuleData))]
    public class ERangeCollectableData : Editor {
        private RangeModuleData _go;
        private Vector3 _target;
        private static float _virtualWidth = 10;
        protected void OnEnable() {
            _go = target as RangeModuleData;
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
        }
        public override void DrawPreview(Rect r) {
            EProjectileShootData.PreviewGUI(r, _go.shootData, _go.gravity, ref _virtualWidth);
        }
        public override bool HasPreviewGUI() {
            return true;
        }
    }

}