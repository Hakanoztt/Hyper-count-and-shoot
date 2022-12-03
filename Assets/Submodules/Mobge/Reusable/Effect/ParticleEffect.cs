using System;
using UnityEngine;

namespace Mobge {
    public class ParticleEffect : AReusableItem {
        public ParticleSystem[] effects;
        protected override void OnPlay() {
            for (int i = 0; i < effects.Length; i++) {
                var e = effects[i];
                e.Play(false);
                var em = e.emission;
                em.enabled = true;
            }
        }
        public override void Stop() {
            for (int i = 0; i < effects.Length; i++) {
                var e = effects[i];
                e.Stop(false);
                var em = e.emission;
                em.enabled = false;
            }
        }
        public override void StopImmediately() {
            for (int i = 0; i < effects.Length; i++) {
                var e = effects[i];
                e.Stop(false);
                e.Clear(false);
                var em = e.emission;
                em.enabled = false;
            }
        }
        public override bool IsActive {
            get {
                for (int i = 0; i < effects.Length; i++) {
                    if (effects[i].isPlaying) {
                        return true;
                    }
                }
                return false;
            }
        }
#if UNITY_EDITOR
        public override void EditorUpdate(Vector3 position, Quaternion rotation, Vector3 scale, float time) {
            transform.localScale = scale;
            transform.SetPositionAndRotation(position, rotation);
            float maxDuration = 0f;
            for (int i = 0; i < effects.Length; i++) {
                maxDuration = Mathf.Max(maxDuration, effects[i].main.duration);
            }
            time %= maxDuration;
            for (int i = 0; i < effects.Length; i++) {
                effects[i].Simulate(time);
            }
        }
#endif
    }
}