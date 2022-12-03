using Mobge.Animation;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.UI {

    public class UIItem : MonoBehaviour {

        [OwnComponent(false)] public Text[] texts;
        [OwnComponent(false)] public TextMeshProUGUI[] textsTMPro;
        [OwnComponent(true)] public Animator animator;
        [OwnComponent(false)] public Image[] images;
        [OwnComponent(false)] public AReusableItem[] effects;
        [InterfaceConstraint(typeof(Component))] public Component[] extraReferences;
        [AnimatorState] public int[] states;
        public float stateTransitionTime = 0;
        public int extraAnimLayer = 1;
        [AnimatorState] public int[] extraAnims;
        public Button[] buttons;
        [NonSerialized] public new int tag;
        private int _currentState;

        public int CurrentState => _currentState;
        public bool ActiveSelf {
            get => gameObject.activeSelf;
            set {
                if (value != gameObject.activeSelf) {
                    gameObject.SetActive(value);
                }
            }
        }
        private static bool TryGet<T>(T[] arr, int index, out T t) {
            if (arr == null || arr.Length <= index) {
                t = default(T);
                return false;
            }
            t = arr[index];
            return t != null;
        }

        public bool TryGetText(int index, out Text text) {
            return TryGet(texts, index, out text);
        }

        public bool TryGetTMhPro(int index, out TextMeshProUGUI text) {
            return TryGet(textsTMPro, index, out text);
        }

        public bool TryGetEffect(int index, out AReusableItem effect) {
            return TryGet(effects, index, out effect);
        }

        [Serializable]
        public class Button {

            [OwnComponent(false)] public UnityEngine.UI.Button button;
            public event Action<UIItem, int> OnClick;
            private int _index;
            private UIItem _item;

            public void Initialize(int index, UIItem item) {
                _item = item;
                _index = index;
                button.onClick.AddListener(ButtonAction);
            }

            private void ButtonAction() {
                OnClick?.Invoke(_item, _index);
            }
        }

        private int _nextState = 0;

        protected void Awake() {
            enabled = true;
            for(int i = 0; i < buttons.Length; i++) {
                buttons[i].Initialize(i, this);
            }
        }

        protected void Update() {
            if (_nextState >= 0 && animator && _nextState < states.Length) {
                if (animator.isInitialized) {
                    animator.Play(states[_nextState],0,0f);
                    _nextState = ~_nextState;
                    enabled = false;
                }
            } else {
                enabled = false;
            }
        }

        public void PlayExtraAnim(int index, float normalizedTime = 0) {
            animator.Play(extraAnims[index], extraAnimLayer, normalizedTime);
        }

        public void SetState(int stateIndex) {
            SetState(stateIndex, stateTransitionTime);
        }

        public void SetState(int stateIndex, float overrideTransitionTime) {
            _currentState = stateIndex;
            if (animator) {
                if (!animator.isInitialized) {
                    _nextState = stateIndex;
                    enabled = true;
                } else {
                    if (overrideTransitionTime <= 0) {
                        animator.Play(states[stateIndex], 0, 0);
                    } else {
                        animator.CrossFade(states[stateIndex], overrideTransitionTime);
                    }
                    _nextState = ~stateIndex;
                }
            }
        }
    }
}