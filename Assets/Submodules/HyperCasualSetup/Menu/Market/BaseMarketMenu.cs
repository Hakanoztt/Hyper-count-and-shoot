using Mobge.HyperCasualSetup.UI;
using Mobge.IdleGame.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup {
    public class BaseMarketMenu : Mobge.UI.BaseMenu {
        [OwnComponent] public Button backButton;
        public MenuPaymentModule paymentModule;
        public Action<BaseMarketMenu> backButtonAction;
        public event Action<ItemSet, int> onItemEquipped;
        public event Action<ItemSet, int> onItemBought;
        public AReusableItem buyEffect;

        public AGameContext GameContext => ((HyperCasualSetup.UI.MenuManager)MenuManager).Context;

        protected new void Awake() {
            base.Awake();
            if (backButton != null) {
                backButton.onClick.AddListener(ExecuteBack);
            }
        }

        private AGameProgress GetSaveData() {
            return GameContext.GameProgressValue;
        }
        private void SaveValues(AGameProgress data) {
            GameContext.GameProgress.SaveValue(data);
        }

        protected void ExecuteBack() {
            backButtonAction?.Invoke(this);
        }

        public bool BuyItem(ItemSet set, int id, int quantity = 1, bool equip = true, float costOverride = -1, int rank = 0) {
            if (quantity <= 0) {
                throw new Exception("Invalid quantity given.");
            }
            //if(!set.stackable && quantity != 1) {
            //    throw new Exception($"Cannot but more than one item from a non-stackable {typeof(ItemSet)}.");
            //}

            var player = GameContext.MenuManager.CurrentPlayer;
            UIMoneyExtension.TryGetMainWallet(player, out var wallet);


            var data = GetSaveData();
            bool _equip = false;
            float totalCost;
            string itemName = set.items[id].name;
            if (costOverride >= 0) {
                totalCost = costOverride;
            }
            else {
                totalCost = set.items[id].GetCost(rank) * quantity;
            }
            if (totalCost > 0) {
                if (paymentModule.GetCurrent(GameContext) < totalCost) {
                    return false;
                }
            }
            AGameProgress.ItemSetData itemData;
            AGameProgress.QuantityItemSetData quantityItemData;

            if (buyEffect != null) {
                buyEffect.Play();
            }

            if (set.stackable) {
                quantityItemData = data.GetQuantityItemSet(set);
                paymentModule.Change(player, wallet, itemName + "_Purchase", (int)-totalCost);
                //data.ChangeTotalScore(GameContext, -totalCost, itemName + "Purchase");
                quantityItemData.AddItem(id, quantity);
            }
            else {
                itemData = data.GetItemSet(set);
                if (itemData.HasItem(id)) {
                    return false;
                }
                paymentModule.Change(player, wallet, itemName + "_Purchase", (int)-totalCost);
                //data.ChangeTotalScore(GameContext, -totalCost, itemName + "Purchase");
                itemData.AddItem(id);

                if (equip) {
                    itemData.EquippedItem = id;
                    _equip = true;
                }
            }
            SaveValues(data);
            onItemBought?.Invoke(set, id);
            if (_equip) {
                onItemEquipped?.Invoke(set, id);
            }
            return true;
        }
        public bool EquipItem(ItemSet set, int id) {
            if (set.stackable) {
                throw new Exception($"Cannot equip an item from a stackable {typeof(ItemSet)}.");
            }
            var data = GetSaveData();
            var setData = data.GetItemSet(set);
            if (setData.EquippedItem == id) {
                return false;
            }
            setData.EquippedItem = id;
            SaveValues(data);

            onItemEquipped?.Invoke(set, id);

            return true;
        }
    }
}