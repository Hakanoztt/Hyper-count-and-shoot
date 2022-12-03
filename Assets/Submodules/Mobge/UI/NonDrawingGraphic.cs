using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.UI {
    [RequireComponent(typeof(CanvasRenderer))]
    public class NonDrawingGraphic : UnityEngine.UI.Graphic
    {
#if UNITY_EDITOR
        protected override void Reset() {
            this.color = new Color(1, 1, 1, 0.2f);
        }
#endif
        public override Color color {
            get {
                return base.color;
            }

            set {
                base.color = value;
            }
        }


        protected override void OnPopulateMesh(VertexHelper toFill) {
#if UNITY_EDITOR
            if(!Application.isPlaying) {
                base.OnPopulateMesh(toFill);
            }
#endif
        }


        protected override void UpdateGeometry() {
#if UNITY_EDITOR
            if(!Application.isPlaying) {
                base.UpdateGeometry();
            }
#endif
        }


#if UNITY_EDITOR
        public override void OnRebuildRequested() {
            if(!Application.isPlaying) {
                base.OnRebuildRequested();
            }
        }
#endif


        public override void Rebuild(CanvasUpdate update) {
#if UNITY_EDITOR
            if(!Application.isPlaying) {
                base.Rebuild(update);
            }
#endif
        }

    }
    
}