using Mobge.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mobge.HyperCasualSetup {

    public class LevelFailMenu : BaseMenu, IPointerClickHandler {

        [Tooltip("Don't wait for click/tap")]
        [SerializeField] private bool endAutomatically;

        private bool _clickConsumed;

        public override void Prepare() {
            base.Prepare();

            if (endAutomatically) {
                Restart();
                _clickConsumed = true;
            } else {
                _clickConsumed = false;
            }
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
            if (_clickConsumed) return;

            Restart();
            _clickConsumed = true;
        }

        public void Restart() {
            (MenuManager as UI.MenuManager).CurrentPlayer.FinishGame(false, 0.1f);
        }

    }
}
