using Mobge.Core;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI.CategorizedMarket {

    [Serializable]
    public class RewardedButton {

        public enum ButtonState {
            Normal,
            Loading,
            Downloading /* Internal */
        }

        private ButtonState _state;
        public ButtonState State {
            get => _state;
            set {
                if (_state == value) return;
                if (value != ButtonState.Downloading) {
                    _state = value;
                }
                UpdateState();
            }
        }
        private ButtonState _realState;

        [SerializeField] private Button button;
        public Button Button => button;

        [SerializeField] private RectTransform res;
        [SerializeField] private RectTransform loadingRes;

        [SerializeField] private TMP_Text rwCountText;
        public string Text => rwCountText.text;

        private int _currentRwCount;
        public int CurrentRwCount {
            get => _currentRwCount;
            set {
                _currentRwCount = value;
                UpdateText();
            }
        }
        private int _targetRwCount;
        public int TargetRwCount {
            get => _targetRwCount;
            set {
                _targetRwCount = value;
                UpdateText();
            }
        }

        private MenuManager Manager;
        private LevelPlayer Player => Manager.CurrentPlayer;
        private AGameContext Context => Manager.Context;
        private RoutineManager.Routine _fUpdate;

        public void Configure(MenuManager mgr) {
            Configure(mgr, int.MaxValue, int.MaxValue);
        }

        public void Configure(MenuManager mgr, int currentRwCount, int targetRwCount) {
            Manager = mgr;


            //Disabled for build
            //_fUpdate.Stop();
            //_fUpdate = Player.FixedRoutineManager.DoRoutine(FUpdate);
            _state = ButtonState.Normal;
            //_realState = ButtonState.Downloading;
            _realState = ButtonState.Normal;

            _currentRwCount = currentRwCount;
            _targetRwCount = targetRwCount;

            UpdateState();
            UpdateText();
        }

        private void UpdateState() {
            switch (_state) {
                case ButtonState.Normal:
                    res.gameObject.SetActive(true);
                    loadingRes.gameObject.SetActive(false);
                    break;
                case ButtonState.Loading:
                case ButtonState.Downloading:
                    res.gameObject.SetActive(false);
                    loadingRes.gameObject.SetActive(true);
                    break;
                default:
#if UNITY_EDITOR
                    Debug.LogWarning("Invalid state for RewardedButton!");
#endif
                    break;
            }
        }

        private void UpdateText() {
            if (rwCountText != null) {
                rwCountText.text = _currentRwCount.ToString() + "/" + _targetRwCount.ToString();
            }
        }

        private void FUpdate(float progress, object data) {
//#if !UNITY_EDITOR
//            if (Context.AdManager.IsRewardedVideoAvailable) {
//                _realState = _state;
//            } else {
//                _realState = ButtonState.Downloading;
//            }
//#else
//            if (Input.GetKey(KeyCode.M)) {
//                _realState = _state;
//            } else {
//                _realState = ButtonState.Downloading;
//            }
//#endif
//
//            State = _realState;
        }
    }
}
