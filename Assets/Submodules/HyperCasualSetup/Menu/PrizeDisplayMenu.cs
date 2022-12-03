using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI {
    public class PrizeDisplayMenu : BaseMenu {
        [OwnComponent] public Button backButton;
        [OwnComponent] public Button claimButton;
        [OwnComponent] public Text title;

        public Action<PrizeDisplayMenu, bool> onBackButtonClicked;
        [Header("Secondary State is single items.")]
        [Header("Main State is for quantitiable items.")]
        public Collection prizeCollection;
        public Sprite coinIcon;
        public int singleItemIconImageIndex = 0;
        public int quantitiableItemIconImageIndex = 0;

        private PrizeData[] _prizes;


        protected new void Awake() {
            base.Awake();
            if (backButton) {
                backButton.onClick.AddListener(BackButtonClick);
            }
            if (claimButton) {
                claimButton.onClick.AddListener(ClaimClick);
            }
        }

        private void ClaimClick() {
            Interactable = false;
            TryClaim(ClaimResult);
        }

        private void ClaimResult(bool obj) {
            Interactable = true;
            if (obj) {
                if (onBackButtonClicked != null) {
                    onBackButtonClicked(this, true);
                }
            }
        }

        private void BackButtonClick() {
            if (onBackButtonClicked != null) {
                onBackButtonClicked(this, false);
            }
        }

        public virtual void TryClaim(Action<bool> onResult) {
            onResult(true);
        }

        public override void Prepare() {
            base.Prepare();
        }

        public void SetPrizes(PrizeData[] prizes, int rank) {
            _prizes = prizes;
            prizeCollection.Count = _prizes.Length;
            for(int i = 0; i < _prizes.Length; i++) {
                ApplyVisual(prizeCollection[i], _prizes[i], rank);
            }
        }
        private void ApplyVisual(ListElement ui, PrizeData data, int rank) {
            Sprite icon;
            string text = data.GetDisplayString(rank);
            ListElement.State state;
            int iconIndex;
            switch (data.giftType) {
                default:
                case PrizeData.Type.Score:
                    icon = coinIcon;
                    state = ListElement.State.Main;
                    iconIndex = quantitiableItemIconImageIndex;
                    break;
                case PrizeData.Type.ItemWithQuantity: {
                        var item = data.set.items[data.itemId];
                        icon = item.sprite;
                        state = ListElement.State.Main;
                        iconIndex = quantitiableItemIconImageIndex;
                    }
                    break;
                case PrizeData.Type.SingleItem: {
                        var item = data.set.items[data.itemId];
                        icon = item.sprite;
                        state = ListElement.State.Secondary;
                        iconIndex = singleItemIconImageIndex;
                    }
                    break;
            }


            if (ui.title) {
                ui.title.text = text;
            }
            if (iconIndex >= 0 && ui.images != null && ui.images.Length > iconIndex) {
                var imUI = ui.images[iconIndex];
                if (imUI != null) {
                    imUI.sprite = icon;
                }
            }
            ui.CurrentState = state;

        }

        [Serializable]
        public class Collection : UICollection<ListElement> {

        }
    }
}