using UnityEngine;

namespace Mobge {
    public class BecomeVisibleCallback : MonoBehaviour {
        public IOnVisibleListener listener;
        private void OnBecameVisible() {
            listener.OnBecomeVisible(this);
        }
        private void OnBecameInvisible() {
            listener.OnBecomeInvisible(this);
        }
    }
    public interface IOnVisibleListener {
        void OnBecomeVisible(BecomeVisibleCallback sender);
        void OnBecomeInvisible(BecomeVisibleCallback sender);
    }
}