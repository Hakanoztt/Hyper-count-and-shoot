using Mobge.Animation;
using Mobge.Build;
using Mobge.HyperCasualSetup;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.DebugMenu {
    public class DebugMenu : MonoBehaviour, IGameContextExtension {

        [OwnComponent(true)] public Canvas canvas;
        [InterfaceConstraint(typeof(IDebugMenuExtension))] public Component[] extensions;
        public DebugMenuOpenRitualChecker ritualChecker;
        public Animator animator;
        [AnimatorState] public int openAnim;
        public Button closeButton;
        public Action OnMenuOpened, OnMenuClosed;
        public TMPro.TMP_Text version;
        public TMPro.TMP_Text commit;
        private GameContext _context;

        public GameContext Context => _context;
        bool IsMenuOpened {
            get { return canvas.enabled; }
            set { canvas.enabled = value; if (value) animator.Play(openAnim); }
        }


        public void Init(AGameContext context) {
            _context = (GameContext)context;
            for (int i = 0; i < extensions.Length; i++) {
                if (extensions[i] is IDebugMenuExtension ext) {
                    ext.Init(this);
                }
            }

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);

            commit.text = GitCommitVersion.CommitVersion;
            version.text = Application.version;

            IsMenuOpened = false;
        }

        private void Update() {
            if (ritualChecker.Check()) {
                Show();
            }
        }

        public void Show() {
            if (IsMenuOpened) return;
            if (OnMenuOpened != null)
                OnMenuOpened();
            IsMenuOpened = true;
        }

        public void Hide() {
            if (OnMenuClosed != null)
                OnMenuClosed();
            IsMenuOpened = false;
        }

        [System.Serializable]
        public struct DebugMenuOpenRitualChecker {
            public float screenCornerPercent;
            public float patternInputTime;
            int _patternIndex;
            float _patternInputStart;
            public bool Check() {
                if (Input.GetMouseButtonDown(0)) {

                    if (_patternIndex < 4 && Input.mousePosition.x < Screen.width * screenCornerPercent &&
                        Input.mousePosition.y > Screen.height - (Screen.width * screenCornerPercent)) {
                        _patternIndex++;
#if UNITY_EDITOR
                        return true;
#endif
                    }
                    else if (_patternIndex >= 4 && Input.mousePosition.x > Screen.width * (1 - screenCornerPercent) &&
                             Input.mousePosition.y > Screen.height - (Screen.width * screenCornerPercent)) {
                        if (Time.unscaledTime - _patternInputStart < patternInputTime) {
                            return true;
                        }
                        _patternIndex = 0;
                        _patternInputStart = Time.unscaledTime;
                    }
                    else {
                        _patternIndex = 0;
                        _patternInputStart = Time.unscaledTime;
                    }
                }
                return false;
            }
        }
    }
    public interface IDebugMenuExtension {
        void Init(DebugMenu debugMenu);
    }
}


