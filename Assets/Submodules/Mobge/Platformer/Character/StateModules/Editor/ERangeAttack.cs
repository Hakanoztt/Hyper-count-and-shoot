using UnityEditor;
using UnityEngine;

namespace Mobge.Platformer.Character {
    [CustomEditor(typeof(RangeAttack))]
    public class ERangeAttack : Editor {
        private RangeAttack _go;
        private Vector3 _target;

        protected void OnEnable() {
            _go = target as RangeAttack;
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if(!_go) return;

            if(GUI.changed) {
                EditorExtensions.SetDirty(_go);
            }
        }
        protected void OnSceneGUI() {
            EProjectileShootData.SceneGUI(_go.shootData, _go.transform, _go.shootOffset, _go.gravity, ref _target);
        }
    }
}