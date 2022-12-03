using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public class TrailEffect : AReusableItem {
        [OwnComponent] public TrailRenderer trail;

        protected void Awake() {
            Stop();
        }

        public override bool IsActive => trail.isVisible;

        public override void Stop() {
            trail.emitting = false;
        }

        public override void StopImmediately() {
            trail.emitting = false;
            trail.Clear();
        }

        protected override void OnPlay() {
            trail.emitting = true;
        }
    }
}