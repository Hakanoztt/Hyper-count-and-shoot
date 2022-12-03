using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Animation
{
    public class AnimatorAdapter : AAnimation
    {
        private static AnimationStatePool<Entry> _entryPool = new AnimationStatePool<Entry>();
        public Animator animator;
        [NonSerialized] private bool _initialized;

        public AnimationDefinition[] animations;
        public override string[] AnimationList {
            get {
                if (animations == null) {
                    return new string[0];
                }

                var l = animations.Length;
                string[] results = new string[l];
                for (int i = 0; i < l; i++) {
                    var c = animations[i].clip;
                    results[i] = c != null ? c.ToString() : null;
                }
                return results;
            }
        }
        private ExposedList<Entry> _tracks;
        protected void Awake() {
            EnsureInit();
        }
        public override bool EnsureInit() {
            if (_initialized) {
                return true;
            }
            _tracks = new ExposedList<Entry>();
            if (animations == null) {
                animations = new AnimationDefinition[0];
            }
            else {
                for (int i = 0; i < animations.Length; i++) {
                    animations[i].Initialize();
                }
            }
            _initialized = true;
            return true;
        }

        public override Color Color { get => Color.white; set { } }

        public override void ForceUpdate() {

        }
        protected void Update() {
            for (int i = 0; i < _tracks.Count; i++) {
                var t = _tracks.array[i];
                if (t != null) {
                    var cs = animator.GetCurrentAnimatorStateInfo(t.Track);
                    if (cs.shortNameHash == t.definition.stateHash) {
                        t.time = cs.length * cs.normalizedTime;
                    }
                    else {
                        var next = t.next;
                        Release(t);
                        if (next != null) {
                            PlayDirect(next);
                        }
                        else {
                            _tracks.array[i] = null;
                        }
                    }
                }
            }
        }
        private void Release(Entry t) {
            t.isPlaying = false;
            t.adapter = null;
            t.next = null;
            _entryPool.ReleaseState(t);
        }
        private Entry NewEntry(int track, int animationId) {
            var e = _entryPool.NewState(null, track, animationId);
            e.definition = animations[animationId];
            e.speed = 1;
            e.time = 0;
            e.adapter = this;
            return e;
        }
        private void EnsureTrackCount(int track) {
            if (track >= _tracks.Count) {
                _tracks.SetCountFast(track + 1);
            }
        }
        private void PlayDirect(Entry entry, float crossFadeTime = 0) {
            int track = entry.Track;
            EnsureTrackCount(track);
            _tracks.array[track] = entry;
            entry.Speed = entry.speed;
            animator.SetLayerWeight(track, 1);
            animator.CrossFade(entry.definition.stateHash, crossFadeTime, track);
            animator.Update(0);
            entry.isPlaying = true;
        }

        public override Animation GetAnimation(int index) {
            return animations[index];
        }

        public override AnimationState GetCurrent(int track) {
            return GetCurrentEntry(track);


        }
        private Entry GetCurrentEntry(int track) {
            if (_tracks == null) {
                return null;
            }
            if (_tracks.Count <= track) {
                return null;
            }
            return _tracks.array[track];
        }

        public override bool IsPlaying(int track, int animationId) {
            var e = GetCurrentEntry(track);
            if (e != null) {
                return e.definition == animations[animationId];
            }
            return false;
        }

        public override bool IsPlaying(AnimationState state) {
            return GetCurrentEntry(state.Track) == state;
        }

        public override bool PrepareForEditor() {
            EnsureInit();
            return true;
        }

        public override void QueueStop(int track) {

        }

        public override void StopAnimation(int track) {
            var e = GetCurrentEntry(track);
            if (e != null) {
                animator.SetLayerWeight(track, 0);
                _tracks.array[track] = null;
                Release(e);
            }
        }

        protected sealed override AnimationState AddAnimation(int track, int animationId, bool loop) {
            var c = GetCurrentEntry(track);
            EnsureTrackCount(track);
            var e = NewEntry(track, animationId);
            if (c == null) {
                _tracks.array[track] = e;
                PlayDirect(e);
            }
            else {
                c.next = e;
            }
            return e;
        }

        protected sealed override AnimationState SetAnimation(int track, int animationId, bool loop, float crossFadeTime = 0) {
            var e = NewEntry(track, animationId);
            StopAnimation(track);
            EnsureTrackCount(track);
            _tracks.array[track] = e;
            PlayDirect(e, crossFadeTime);
            return e;
        }


        [Serializable]
        public class AnimationDefinition : Animation
        {
            public AnimationClip clip;
            [NonSerialized]
            public int stateHash;
            [AnimatorFloatParameter] public int speedKey;
            public void Initialize() {
                stateHash = Animator.StringToHash(clip.name);
            }

            public sealed override float Duration {
                get {
                    return clip.length;
                }
            }
        }
        public class Entry : AnimationState
        {
            public Entry next;
            public AnimatorAdapter adapter;
            public AnimationDefinition definition;
            public float speed;
            public bool isPlaying;
            public float time;

            public override float Speed {
                get => speed;
                set {
                    speed = value;
                    if (isPlaying) {
                        if (definition.speedKey != 0) {
                            adapter.animator.SetFloat(definition.speedKey, value);
                        }
                        else {
                            adapter.animator.speed = value;
                        }
                    }
                }
            }

            public override float Duration => definition.Duration;

            public override float Time {
                get {
                    return time;
                }
                set {
                    // var stt = adapter.animator.GetCurrentAnimatorStateInfo(this.Track);
                    adapter.animator.Play(definition.stateHash, this.Track, value / definition.Duration);
                    this.time = value;
                    adapter.animator.Update(0);
                }
            }
            public override bool Loop { get { return false; } set { } }
            public override object RealState { get => this; set { } }
        }
    }
}