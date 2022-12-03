using UnityEngine;

namespace Mobge {

    public class AudioEffect : AReusableItem {

        public AudioClip[] startClips, cycleClips, endClips;
        [OwnComponent] public AudioSource audioSource;

        private float _defaultVolume = 1f;

        protected void Awake() {
            enabled = false;
            _defaultVolume = audioSource.volume;
        }

        public override bool IsActive => audioSource.isPlaying;
        public override void Stop() {
            enabled = false;
            Play(endClips);
        }

        protected void Update() {
            if (cycleClips != null) {
                if (!audioSource.isPlaying) {
                    Play(cycleClips, true);
                }
            }
        }

        public override void StopImmediately() {
            enabled = false;
            Play(null);
        }

        public float Volume {
            get => audioSource.volume;
            set {
                audioSource.volume = value;
            }
        }

        protected override void OnPlay() {
            enabled = true;
            audioSource.volume = _defaultVolume;
            Play(startClips);
        }

        private void Play(AudioClip[] clips, bool loop = false) {
            if (clips != null && clips.Length > 0) {
                audioSource.clip = clips[Random.Range(0, clips.Length)];
                audioSource.loop = loop;
                audioSource.Play();
            } else {
                if (audioSource.isPlaying) {
                    audioSource.Stop();
                }
            }
        }
    }
}