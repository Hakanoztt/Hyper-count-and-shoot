
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI.CategorizedMarket {

    public class RewardedButtonAdapter : MonoBehaviour {

        [SerializeField] private RewardedButton _button;
        public RewardedButton RewardedButton => _button;
        public Button Button => _button.Button;

        public void Configure(MenuManager mgr) {
            _button.Configure(mgr);
        }

        public void Configure(MenuManager mgr, int currentRwCount, int targetRwCount) {
            _button.Configure(mgr, currentRwCount, targetRwCount);
        }

        public void SetState(RewardedButton.ButtonState state) {
            _button.State = state;
        }
    }
}