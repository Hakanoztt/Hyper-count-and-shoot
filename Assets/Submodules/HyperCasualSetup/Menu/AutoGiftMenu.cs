using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mobge.UI;
using System;
using UnityEngine.UI;
using Mobge.Animation;
using System.Globalization;
using UnityEngine.Serialization;

namespace Mobge.HyperCasualSetup.UI {
    public class AutoGiftMenu : MonoBehaviour, BaseMenu.IExtension, IAnimationOwner {
        public int progressStepPercentage = 10;

        public Transform menuRoot;
        public AReusableItem increaseEndEffect;
        public AReusableItem increaseProgressEffect;
        public AReusableItem completedEffect;

        public Image[] icons;

        public Text progressText;
        public string progressTextFormat = "{0}";


        public AAnimation progressAnimation;
        [Animation] public int progressState;


        public float animationDelay = 0.5f;
        public float animationTime = 0.5f;
        public Image progressUI;

        public PrizeDisplayMenu giftCollectMenuRes;

        public ReclaimModule reclaimModule;

        public ItemGroupData itemSelector;

        public Action<AutoGiftMenu> _onAnimationsFinished;

        public string eventClaimMenuOpen;

        private const string ProgressKey = "auto gift progress";
        private const string PrizeDataItemSetNameKey = "auto_gift_prize_data:set_name";
        private const string PrizeDataItemIdKey = "auto_gift_prize_data:item_id";
        //private const string PrizeDataStackableKey = "auto_gift_prize_data:stackable";

        private int _oldValue;
        private int _newValue;
        protected PrizeDisplayMenu _giftCollectMenu;
        private BaseMenu _menu;
        private uint _lastSession;
        private bool _activate;
        private bool _animationsChecked;
        private readonly PrizeData _prize = new PrizeData();

        AAnimation IAnimationOwner.Animation => this.progressAnimation;

        public virtual void Prepare(BaseMenu menu) {
            _menu = menu;
            var session = CurrentPlayer.Session;
            if (_lastSession != session) {
                _lastSession = session;
                _activate = UpdateData(menu);
                _animationsChecked = false;
                menuRoot.gameObject.SetActive(_activate);
                menu.Interactable = !_activate;
                if (_activate) {
                    if (progressAnimation != null) {
                        progressAnimation.EnsureInit();
                    }
                    _menu.ActionManager.DoAction(DelayedPrepare, Time.deltaTime * 1.5f);
                    reclaimModule.Prepare(this);
                    StartAnimations();
                }

                menu.onFocusChange -= onFocusChange;
                menu.onFocusChange += onFocusChange;
            }
        }
        public BaseLevelPlayer CurrentPlayer {
            get {
                if (_menu == null) {
                    return null;
                }
                return ((HyperCasualSetup.UI.MenuManager)_menu.MenuManager).CurrentPlayer;
            }
        }
        protected virtual void StartAnimations() {
            if (!_animationsChecked) {
                _animationsChecked = true;
                _menu.ActionManager.DoAction(ExecuteAnimation, animationDelay);
            }
        }
        private void DelayedPrepare(bool completed, object data) {
            SetProgressVisually(_oldValue / 100f, !_animationsChecked);
        }
        private void SetProgressVisually(float progress, bool playEffect = true) {
            if (progressUI) {
                progressUI.fillAmount = progress;
                if (increaseProgressEffect != null && playEffect) {
                    if (!increaseProgressEffect.IsActive) {
                        increaseProgressEffect.Play();
                    }
                }
            }
            if (progressAnimation != null) {
                var stt = progressAnimation.PlayIfNotPlaying(progressState, false);
                stt.Speed = 0;
                stt.Time = progress * stt.Duration;
            }
            if (progressText) {
                progressText.text = string.Format(CultureInfo.InvariantCulture, progressTextFormat, progress);
            }
        }
        bool UpdateData(BaseMenu menu) {
            var man = menu.MenuManager as MenuManager;
            var progVal = man.Context.GameProgressValue;
            var filled = IncreaseValue(progVal);
            if (!UpdateSelectedItem(progVal, filled)) {
                return false;
            }
            var s = _prize.Icon;
            for (int i = 0; i < icons.Length; i++) {
                icons[i].sprite = s;
            }

            man.Context.GameProgress.SaveValue(progVal);
            return true;
        }
        private bool UpdateSelectedItem(AGameProgress progVal, bool filled) {
            var menuManager = (MenuManager) _menu.MenuManager;
            var selector = itemSelector.NewRandomSelector(menuManager.Context);
            ItemSet.ItemPath item;
            if (!progVal.HasBytes(PrizeDataItemSetNameKey)) {
                if (selector.AvailableCount <= 0) return false;
                item = selector.Select();
                progVal.SetString(PrizeDataItemSetNameKey, item.set.name);
                progVal.SetInt(PrizeDataItemIdKey, item.id);
                //progVal.SetBool(PrizeDataStackableKey, item.Stackable);
            }else {
                if (!itemSelector.TryGetItemSet(progVal.GetString(PrizeDataItemSetNameKey, ""), out var itemSet)) {
                    progVal.RemoveBytes(PrizeDataItemSetNameKey);
                    progVal.RemoveValue(PrizeDataItemIdKey);
                    //progVal.RemoveValue(PrizeDataStackableKey);
                    return false;
                }
                item = new ItemSet.ItemPath() {
                    set = itemSet,
                    id = progVal.GetInt(PrizeDataItemIdKey, 0),
                    //stackable = progVal.GetBool(PrizeDataStackableKey, false),
                };
            }
            _prize.set = item.set;
            _prize.itemId = item.id;
            _prize.value = 1f;
            _prize.giftType = item.Stackable ? PrizeData.Type.ItemWithQuantity : PrizeData.Type.SingleItem;

            if (filled) {
                progVal.RemoveBytes(PrizeDataItemSetNameKey);
                progVal.RemoveValue(PrizeDataItemIdKey);
                //progVal.RemoveValue(PrizeDataStackableKey);
            }
            return true;
        }
        private bool IncreaseValue(AGameProgress progVal) {
            _oldValue = (int)progVal.GetFloat(ProgressKey, 0);
            _newValue = _oldValue + GetStepPercentage();
            bool filled = _newValue >= 100;
            if (filled) {
                _newValue = 100;
                progVal.SetFloat(ProgressKey, 0);
            }
            else {
                progVal.SetFloat(ProgressKey, _newValue);
            }
            return filled;
        }
        protected virtual int GetStepPercentage() {
            return progressStepPercentage;
        }
        protected void GiftCollected(PrizeDisplayMenu obj, bool collected) {
            if (collected) {
                var man = (MenuManager)_menu.MenuManager;
                var val = man.Context.GameProgressValue;
                _prize.ApplyToData(man.Context, val, CurrentPlayer.SaveData.score, 0);
                man.Context.GameProgress.SaveValue(val);
                reclaimModule.SetMode(ReclaimMode.Claimed);
            }
            else {
                reclaimModule.SetMode(ReclaimMode.Unclaimed);
            }
            _giftCollectMenu.MenuManager.PopMenuControlled(_giftCollectMenu);
            _menu.Interactable = true;

            _onAnimationsFinished?.Invoke(this);
            _onAnimationsFinished = null;
        }
        private void ExecuteAnimation(bool completed, object data) {
            if (completed) {
                AReusableItem effectToPlay;
                if (_newValue >= 100) {
                    effectToPlay = completedEffect;
                }
                else {
                    effectToPlay = increaseEndEffect;
                }
                if (effectToPlay != null) {
                    effectToPlay.Play();
                }
                _menu.ActionManager.DoAction(AnimationEnded, animationTime, UpdateProgress);
            }
        }
        private void UpdateProgress(float progress, object data) {
            SetProgressVisually(Mathf.Lerp(_oldValue / 100f, _newValue / 100f, progress));
        }
        private void AnimationEnded(bool completed, object data) {
            if (completed) {
                if (_newValue >= 100) {
                    OpenGiftCollectMenu();
                }
                else {
                    _menu.Interactable = true;
                    _onAnimationsFinished?.Invoke(this);
                    _onAnimationsFinished = null;
                }

                if (increaseProgressEffect != null)
                {
                    increaseProgressEffect.Stop();
                }
            }
        }
        protected void OpenGiftCollectMenu() {
            EnsureGiftCollectMenu();
            _giftCollectMenu.SetPrizes(new PrizeData[]{_prize}, 0);
            _menu.MenuManager.PushMenu(_giftCollectMenu);
            ((MenuManager)_menu.MenuManager).Context.FireAnalyticsEvent(eventClaimMenuOpen);
        }
        private void OnDestroy() {
            if (_giftCollectMenu) {
                _giftCollectMenu.gameObject.DestroySelf();
            }
        }

        protected void EnsureGiftCollectMenu() {
            if (_giftCollectMenu == null) {
                _giftCollectMenu = Instantiate(giftCollectMenuRes);
                _giftCollectMenu.onBackButtonClicked = GiftCollected;
            }
        }

        private void onFocusChange(bool isFocused) {
            if (!isFocused && increaseProgressEffect != null) {
                increaseProgressEffect.Stop();
            }
        }

        [Serializable]
        public struct ReclaimModule {
            [OwnComponent] public RectTransform[] reclaimSpecifics;
            [OwnComponent] public RectTransform[] defaultSpecifics;
            [OwnComponent] public Button reclaimButton;
            private void SetEnabled(RectTransform[] trs, bool enabled) {
                for(int i = 0; i < trs.Length; i++) {
                    trs[i].gameObject.SetActive(enabled);
                }
            }
            public void SetMode(ReclaimMode mode) {
                if (reclaimButton) {
                    SetEnabled(defaultSpecifics, mode == ReclaimMode.NotReady);
                    SetEnabled(reclaimSpecifics, mode != ReclaimMode.NotReady);
                    reclaimButton.gameObject.SetActive(mode == ReclaimMode.Unclaimed);
                }
            }
            internal void Prepare(AutoGiftMenu autoGiftMenu) {
                if (reclaimButton) {
                    reclaimButton.onClick.RemoveListener(autoGiftMenu.OpenGiftCollectMenu);
                    reclaimButton.onClick.AddListener(autoGiftMenu.OpenGiftCollectMenu);
                    SetMode(ReclaimMode.NotReady);
                }
            }
        }
        public enum ReclaimMode {
            NotReady,
            Claimed,
            Unclaimed
        }
    }
}