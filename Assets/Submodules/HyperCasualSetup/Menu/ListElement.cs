using Mobge.Animation;
using Mobge.UI;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup {
    [Obsolete("This component is obsolate. Use " + nameof(UIItem) + " instead.", false)]
    public class ListElement : MonoBehaviour
    {
        [OwnComponent] public Text title;
        [OwnComponent] public Text subTitle;
        [OwnComponent] public Text detail;
        [OwnComponent(true)] public Animator animator;
        [OwnComponent] public Image[] images;
        [AnimatorState] public int mainState;
        [AnimatorState] public int secondaryState;
        [AnimatorState] public int disabledState;
        public int extraAnimLayer = 1;
        [AnimatorState] public int[] extraAnims;
        [OwnComponent] public Button mainButton;
        [OwnComponent] public Button secondaryButton;
        [NonSerialized] public new int tag;


        public Action<ListElement> MainButtonClicked;
        public Action<ListElement> SecondaryButtonClicked;
        
        
        private int _nextState;

        private State _state;

        protected void Awake() {
            if (mainButton) {
                mainButton.onClick.AddListener(MainButtonAction);
            }
            if (secondaryButton) {
                secondaryButton.onClick.AddListener(SecondaryButtonAction);
            }
            enabled = true;
        }
        protected void Update() {
            if (_nextState != 0) {
                if (animator) {
                    if (animator.isInitialized) {
                        animator.Play(_nextState, -1, 0f);
                        _nextState = 0;
                        enabled = false;
                    }
                }
                else {
                    enabled = false;
                }
            }
            else {
                enabled = false;
            }
        }
        public void PlayExtraAnim(int index) {
            animator.Play(extraAnims[index], extraAnimLayer);
        }

        private void MainButtonAction() {
            MainButtonClicked?.Invoke(this);
        }
        private void SecondaryButtonAction() {
            SecondaryButtonClicked?.Invoke(this);
        }


        public void SetState(int state) {
            if (animator) {
                if (!animator.isInitialized) {
                    _nextState = state;
                    enabled = true;
                }
                else {
                    animator.Play(state, -1, 0f);
                }
            }
        }
        public State CurrentState {
            get => _state;
            set {
                _state = value;
                int stt;
                switch (_state) {
                    default:
                    case State.Main:
                        stt = mainState;
                        break;
                    case State.Secondary:
                        stt = secondaryState;
                        break;
                    case State.Disabled:
                        stt = disabledState;
                        break;
                }
                SetState(stt);
            }
        }

        public enum State
        {
            Main = 0,
            Secondary = 1,
            Disabled = 2,
        }
    }
}