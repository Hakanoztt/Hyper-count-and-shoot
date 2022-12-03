using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Mobge.Animation {
    public abstract class AAnimation : MonoBehaviour
    {
        
#if UNITY_EDITOR
        
        public bool editorLogsEnabled;
        
#endif
        public abstract string[] AnimationList{get;}
        public int defaultTrack = 0;
        public Action<bool> VisibilityChanged { get; set; }
        public abstract Color Color { get; set; }

        public AnimationState PlayAnimation(int track, int animationId, bool loop, float crossFadeTime = 0) {
#if UNITY_EDITOR
            if (editorLogsEnabled) {
                Debug.Log("play anim: (" + track + ", " + AnimationList[animationId] + ")");
            }
#endif
            return SetAnimation(track, animationId, loop, crossFadeTime);
        }
        public AnimationState PlayAnimation(int animationId, bool loop, float crossFadeTime = 0) {
            return PlayAnimation(defaultTrack, animationId, loop, crossFadeTime);
        }
        public AnimationState QueueAnimation(int track, int animationId, bool loop){
#if UNITY_EDITOR
            if(editorLogsEnabled){
                Debug.Log("queue anim: (" + track + ", " +(animationId < 0 ? "null" : AnimationList[animationId]) + ")");
            }
#endif
            return AddAnimation(track, animationId, loop);
        }
        public AnimationState PlayIfNotPlaying(int track, int animationId, bool loop = false) {
            var c = GetCurrent(track);
            if (c == null || c.AnimationId != animationId) {
                return PlayAnimation(track, animationId, loop);
            }
            return c;
        }
        public AnimationState PlayIfNotPlaying(int animationId, bool loop = false) {
            return PlayIfNotPlaying(defaultTrack, animationId, loop);
        }
        private void OnBecameVisible() {
            VisibilityChanged?.Invoke(true);
        }
        private void OnBecameInvisible() {
            VisibilityChanged?.Invoke(false);
        }
        /// <summary>
        /// Play specified animations one by one. Any of the specified animation can be non-existent (lower than zero).
        /// </summary>
        /// <param name="track"></param>
        /// <param name="anim1"></param>
        /// <param name="anim2"></param>
        public AnimationState PlayOrdered(int track, int anim1, int anim2, bool loopLast = true) {
            AnimationState result;
            if (anim1 >= 0) {
                result = PlayAnimation(track, anim1, false);
                if(anim2 >= 0) {
                    QueueAnimation(track, anim2, loopLast);
                }
            }
            else {
                if(anim2 >= 0) {
                    result = PlayAnimation(track, anim2, loopLast);
                }
                else {
                    result = null;
                }
            }
            return result;
        }
        public void StopAnimation() {
            this.StopAnimation(defaultTrack);
        }
        public AnimationState PlayOrdered(int anim1, int anim2, bool loopLast = true) {
            return PlayOrdered(defaultTrack, anim1, anim2, loopLast);
        }
        public AnimationState GetCurrent() => GetCurrent(defaultTrack);
        public abstract Animation GetAnimation(int index);
        protected abstract AnimationState SetAnimation(int track, int animationId, bool loop, float crossFadeTime = 0);
        protected abstract AnimationState AddAnimation(int track, int animationId, bool loop);
        public abstract void QueueStop(int track);
        public void QueueStop() => QueueStop(defaultTrack);
        public abstract void StopAnimation(int track);
        public abstract bool IsPlaying(int track, int animationId);
        public abstract bool IsPlaying(AnimationState state);
        public abstract AnimationState GetCurrent(int track);
        public abstract void ForceUpdate();
        public abstract bool PrepareForEditor();

        public virtual bool EnsureInit() {
            return true;
        }
        public virtual void SetTrackWeight(int track, float weight) { }
        public virtual float GetTrackWeight(int track) { return 0f; }
    }
    public struct AnimationStateSafe {
        public AnimationState state;
        public int id;
        public AnimationStateSafe(AnimationState state) {
            this.state = state;
            this.id = state.id;
        }
        public bool IsPlaying(AAnimation animation) {
            return state != null && id == state.id && animation.IsPlaying(state);

        }
    }
    public abstract class AnimationState {
        public int id;
        public int Track { get; internal set; }
        public int AnimationId { get; internal set; }
        public abstract float Speed { get; set; }
        public abstract float Duration { get; }
        public abstract float Time { get; set; }
        public Animation Animation { get; }
        public abstract object RealState { get; set; }
        public abstract bool Loop { get; set; }
        public float PlayDuration {
            get {
                return Duration / Speed;
            }
            set {
                Speed = Duration / value;
            }
        }
        public AnimationStateSafe ToSafeReference() {
            return new AnimationStateSafe(this);
        }
    }
    public abstract class Animation {
        public abstract float Duration { get; }
    }
    public class AnimationStatePool<T> where T : AnimationState, new() {
        private Stack<T> _list = new Stack<T>();
        private List<T> _activeList = new List<T>();

        public AnimationStatePool(){
        }
        public List<T> ActiveList => _activeList;
        private T Pop(){
            if(_list.Count == 0) return new T(); 
            return _list.Pop();
        }
        public T NewState(object realState, int track, int animationId) {
            var t = Pop();
            t.RealState = realState;
            t.Track = track;
            t.AnimationId = animationId;
            _activeList.Add(t);
            //Debug.Log("new state: " + track + " " + animationId);
            return t;
        }
        public void ReleaseState(object o) {
            if (!TryGetState(o, out var i, out var state)) return;
            ReleaseState(i, state);

        }
        public void ReleaseState(T state) {
            ReleaseState(_activeList.IndexOf(state), state);
        }
        private void ReleaseState(int index, T state) {
            state.id++;
            state.RealState = null;
            _list.Push(state);
            _activeList.RemoveAt(index);
        }
        private bool TryGetState(object o, out int index, out T state){
            for(index = 0; index < _activeList.Count; index++){
                var next = _activeList[index];
                if(next.RealState == o) {
                    state = next;
                    return true;
                }
            }
            //throw new InvalidOperationException("Specified entry is not in the active list. (" + o + ", " + index + ")");
            state = default(T);
            return false;
        }
        public bool TryGetState(object o, out T state) {
            return TryGetState(o, out _, out state);
        }
        public bool TryGetState(int track, out T state) {
            for(int index = 0; index < _activeList.Count; index++){
                var next = _activeList[index];
                if(next.Track == track) {
                    state = next;
                    return true;
                }
            }
            state = default(T);
            return false;
        }
    }
    
    public class AnimationAttribute : PropertyAttribute {
        public AnimationAttribute(bool includeNone = false){
            this.includeNone = includeNone;
        }
        public bool includeNone;
    }
    public interface IAnimationOwner {
        AAnimation Animation{get;}
    }
    [Serializable]
    public class AnimationSpliter {
        [AnimationAttribute] public int animation;
        public Division[] divisions;
        public float TotalTime {
            get{
                float total = 0;
                for(int i = 0; i < divisions.Length; i++){
                    total = divisions[i].duration;
                }
                return total;
            }
        }
        [Serializable]
        public struct Division {
            public float duration;
            public float aimationProgress;
        }
        /** 
         * <summary>
         * Starts the animation that will play with splits. 
         * This function has to be called on FixedUpdate function. </summary>
         */
        public void Start(int track, AAnimation animation, out Updater updater, int startState = 0) {
            updater.state = animation.PlayAnimation(track, this.animation, false);
            updater.currentIndex = 0;
            updater.lastAnimStartTime = startState;
            updater.stateStartTime = Time.fixedTime;
            updater.animDuration = updater.state.Duration;
            ApplyState(ref updater, divisions[0]);
        }
        /** 
         * <summary>
         * Updates the animation progress according to the splits.
         * Call this function in each FixedUpdate until it returns false, after <see cref="Start"/> function is called. </summary>
         */
        public bool Update(AAnimation animation, ref Updater updater, out bool indexChanged) {
            var d = divisions[updater.currentIndex];
            float passedTime = Time.fixedTime - updater.stateStartTime;
            if((indexChanged = passedTime >= d.duration)) {
                updater.currentIndex++;
                if(updater.currentIndex >= divisions.Length){
                    return false;
                }
                updater.stateStartTime = Time.fixedTime;
                ApplyState(ref updater, divisions[updater.currentIndex]);
            }
            return true;
        }
        private void ApplyState(ref Updater updater, Division division) {
            var animTime = division.aimationProgress * updater.animDuration;
            var animDuration = animTime - updater.lastAnimStartTime;
            updater.lastAnimStartTime = animTime;
            updater.state.Speed = animDuration / division.duration;
        }
        public struct Updater {
            public int currentIndex;
            public AnimationState state;
            public float stateStartTime;
            public float lastAnimStartTime;
            public float animDuration;
            public float GetLastStatesDuration(AnimationSpliter splitter) {
                var d = splitter.divisions[currentIndex];
                return d.duration;
            }
            public float LastStatesTime {
                get {
                    return Time.fixedTime - stateStartTime;
                }
            }
            public void FinishCurrentState(){
                stateStartTime = float.NegativeInfinity;
            }

        }
    }
    /**
     * <summary>
     * This attribute is only valid for fields whose type is <see cref="AnimationSpliter"></summary>
     */
    public class AnimationSplitterAttribute : PropertyAttribute {
        public string[] constantIndexNames;
        public AnimationSplitterAttribute(string[] constantIndexNames) {
            this.constantIndexNames = constantIndexNames;
        }
        public AnimationSplitterAttribute() { }
    }

    
    [Serializable]
    public struct AnimationSequence {

        public OperationMode operationMode;
        public enum OperationMode {
            LoopLast = 0,
            PlayStraight = 1,
            WeightedLoop = 2,
            WeightedOnce = 3,
        }
        
        public WeightedAnimation[] animations;
        [Serializable]
        public struct WeightedAnimation {
            [AnimationAttribute] public int animation;
            public float weight;
            [NonSerialized] public float cumulativeWeight;
        }
        
        public void Start(int track, AAnimation animation, out Updater updater) {
            updater.animation = animation;
            updater.track = track;
            updater.isPlaying = true;
            updater.currentIndex = 
                operationMode == OperationMode.WeightedLoop || operationMode == OperationMode.WeightedOnce 
                    ? Random.Range(0, animations.Length): 0;
            if (animations.Length <= 0) {
                return;
            }
            animation.PlayAnimation(track, animations[updater.currentIndex].animation, false);

        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="updater"></param>
        /// <returns>is finished</returns>
        public bool Update(ref Updater updater) {
            if (!updater.isPlaying) return true;
            if (animations.Length <= 0) return true;
            if (updater.animation.IsPlaying(updater.track, animations[updater.currentIndex].animation)) return false;
            if (!ChooseNextIndex(ref updater)) return true;
            updater.animation.PlayAnimation(updater.track, animations[updater.currentIndex].animation, false);
            return false;
        }

        public void Stop(ref Updater updater) {
            updater.isPlaying = false;
            updater.animation.StopAnimation(updater.track);
        }
        
        private bool ChooseNextIndex(ref Updater updater) {
            switch (operationMode) {
                case OperationMode.LoopLast:
                    updater.currentIndex++;
                    updater.currentIndex = Mathf.Clamp(updater.currentIndex, 0, animations.Length-1);
                    return true;
                case OperationMode.PlayStraight:
                    updater.currentIndex++;
                    if (updater.currentIndex < animations.Length) return true;
                    updater.isPlaying = false;
                    return false;
                case OperationMode.WeightedLoop:
                    float totalWeight = 0f;
                    for (int i = 0; i < animations.Length; i++) {
                        totalWeight += animations[i].weight;
                    }
                    float r = Random.Range(0f, totalWeight);
                    float c = 0f;
                    for (int i = 0; i < animations.Length; i++) {
                        c += animations[i].weight;
                        if (r > c) continue;
                        updater.currentIndex = i;
                        return true;
                    }
                    break;
                case OperationMode.WeightedOnce:
                    updater.isPlaying = false;
                    return false;
            }
            return false;
        }
        
        public struct Updater {
            public AAnimation animation;
            public int track;
            public int currentIndex;
            public bool isPlaying;
        }
        public struct AnimatorUpdater {
            public Animator animator;
            public int track;
            public int currentIndex;
            public bool isPlaying;
        }


    }
    
}