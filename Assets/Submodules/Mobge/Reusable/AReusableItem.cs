using UnityEngine;

namespace Mobge {
    public abstract class AReusableItem : MonoBehaviour {
        private int _runId;
        public int RunId => _runId;
        protected abstract void OnPlay();
        public int Play() {
            _runId++;
            OnPlay();
            return _runId;
        }
        public abstract void Stop();
        
        /// <summary>
        /// Immediately Stops the animation or the effect
        /// </summary>
        public abstract void StopImmediately();
        // to do: Consider making this method not abstract or virtual, this method might be a common case, since it is called a lot.
        // the value that this method returns might be cached and managed by sub classes
        // very similar method was a common case in Oddmar
        public abstract bool IsActive { get; }
#if UNITY_EDITOR
        public virtual void EditorUpdate(Vector3 position, Quaternion rotation, Vector3 scale, float time) { }
#endif
    }
}