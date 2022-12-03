using UnityEngine;

namespace Mobge {
    public class AnimationReusable : AReusableItem {

        [OwnComponent(true)]
        public new UnityEngine.Animation animation;

        public AnimationClip startClip;
        public AnimationClip loopClip;
        public AnimationClip endClip;
        public float fadeTime;

        public ActionManager ActionManager { get; set; }
        private ActionManager.Action _current;


        protected override void OnPlay() {
            StopAnimation();
            if (startClip) {
                _current = PlayAnimation(startClip);
            }
            if (loopClip) {
                if (ActionManager == null) {
                    animation.PlayQueued(loopClip.name, QueueMode.CompleteOthers);
                }
                else {
                    if (_current.IsFinished()) {
                        PlayAnimation(loopClip);
                    }
                    else {
                        _current.OnFinish = AddLoopToQueue;
                    }
                }
            }
            //else if (endClip) {
            //    animation.PlayQueued(endClip.name, QueueMode.CompleteOthers);
            //}
        }
        public ActionManager.Action PlayAnimation(AnimationClip clip) {
            if(ActionManager == null) {
                animation.CrossFade(clip.name, fadeTime);
                //animation.Play(clip.name);
                return new ActionManager.Action();
            }
            else {
                //Debug.Log("crossing: " + clip);
                return animation.CrossFadeState(clip.name, ActionManager, fadeTime);
            }
        }
        private void AddLoopToQueue(object data, bool success) {
            PlayAnimation(loopClip);
        }
        public void StopAnimation() {
            animation.Stop();
            _current.Stop();
        }
        public override void Stop() {
            if (endClip) {
                PlayAnimation(endClip);
            } else {
                StopAnimation();
            }
        }
        public override void StopImmediately() {
            StopAnimation();
        }
        public override bool IsActive {
            get {
                var val = animation.enabled && animation.isPlaying && (ActionManager == null || !_current.IsFinished());
                //Debug.Log("anim: " + val);
                return val;
            }
        }
        #if UNITY_EDITOR
        public override void EditorUpdate(Vector3 position, Quaternion rotation, Vector3 scale, float time) {
            var tr = transform;
            tr.localPosition = position;
            tr.localRotation = rotation;
            tr.localScale = scale;
        }
        #endif
    }
}