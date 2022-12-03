using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mobge.UI;
using System;
using Mobge.Platformer.Character;
using UnityEngine.Events;

namespace Mobge.HyperCasualSetup
{
    public class BasicMarketMenu : BaseMarketMenu {
        public ItemSet itemSet;
        public Elements elements;
        public Text[] totalCoinTexts;
        private UI.MenuManager _menuManager;
        private ListElement _lastEquipped;

        public override void Prepare() {
            base.Prepare();
            _menuManager = MenuManager as Mobge.HyperCasualSetup.UI.MenuManager;
            PopulateItems();
        }



        private void PopulateItems() {
            var data = _menuManager.Context.GameProgressValue;
            var itemData = data.GetItemSet(itemSet);
            elements.Count = itemSet.items.Count;
            for(int i  = 0; i < elements.Count; i++) {
                var item = itemSet.items[i];
                var listElement = elements[i];
                listElement.images[0].sprite = item.sprite;
                listElement.detail.text = item.cost.ToString();
                listElement.MainButtonClicked = BuyButtonClicked;
                listElement.SecondaryButtonClicked = EquipButtonClicked;
                listElement.tag = i;

                RefreshState(listElement, itemData);
            }
        }
        private void RefreshState(ListElement element, AGameProgress.ItemSetData data) {
            bool hasItem = data.HasItem(element.tag);
            if (hasItem) {
                bool equipped = data.EquippedItem == element.tag;
                if (equipped) {
                    if (_lastEquipped != null) {
                        _lastEquipped.CurrentState = ListElement.State.Secondary;
                    }
                    _lastEquipped = element;
                    element.CurrentState = ListElement.State.Disabled;
                }
                else {
                    element.CurrentState = ListElement.State.Secondary;
                }
            }
            else {
                element.CurrentState = ListElement.State.Main;
            }
        }

        private void EquipButtonClicked(ListElement obj) {
            EquipItem(itemSet, obj.tag);
        }

        private void BuyButtonClicked(ListElement obj) {

            if (BuyItem(itemSet, obj.tag)) {
                
            }
            else {
                PlayNotEnoughCoinAnim();
            }


            RefreshState(obj, _menuManager.Context.GameProgressValue.GetItemSet(itemSet));

        }
        private void PlayNotEnoughCoinAnim() {
        }

        [Serializable]
        public class Elements : Mobge.UI.UICollection<ListElement> {

        }
    }
}