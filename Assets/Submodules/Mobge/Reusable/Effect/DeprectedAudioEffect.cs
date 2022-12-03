using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge
{
    public class DeprectedAudioEffect : AReusableItem
    {
        public AudioClip startClip, cycleClip, endClip;
        public AudioSource audioSource;

        protected void Awake() {
            enabled = false;
            
        }

        public override bool IsActive => audioSource.isPlaying;

        public override void Stop() {
            enabled = false;
            Play(endClip);
        }
        protected void Update() {
            if (cycleClip != null) {
                if (!audioSource.isPlaying) {
                    Play(cycleClip, true);
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
            audioSource.volume = 1;
            Play(startClip);
        }
        private void Play(AudioClip clip, bool loop = false) {
            if (clip != null) {
                audioSource.clip = clip;
                audioSource.loop = loop;
                audioSource.Play();
            }
            else {
                if (audioSource.isPlaying) {
                    audioSource.Stop();
                }
            }
        }
    }
}