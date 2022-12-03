using Mobge.IdleGame;
using Mobge.IdleGame.UI;
using Mobge.Telemetry;
using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI {
    public class IncrementalMenu : BaseMenu {



        [OwnComponent] public Button closeButton;

        [Header("tmp 2: title text")]
        [Header("tmp 1: cost text")]
        [Header("tmp 0: level text")]
        [Header("state 3: add")]
        [Header("state 2: maxed")]
        [Header("state 1: not enough money")]
        [Header("state 0: ready to buy")]
        public UICollection<UIItem> buttons;
        public int upgradeCount = 3;
        public int[] maxLevels;
        public int itemCount = 3;

        public MenuPaymentModule paymentModule;

        public Action<int> onIncrementalUpdate;
        public Action<IncrementalMenu> handleClose;


        private List<ItemSet.ItemPath> _paths;
        private MenuManager _manager;

        private ItemGroupData _itemData;

        public IAddHandler AddHandler { get; set; }
        private BaseLevelPlayer LevelPlayer { get; set; }
        private AGameContext Context => LevelPlayer.Context;

        public void Prepare(ItemGroupData itemSet) {
            _manager = MenuManager as Mobge.HyperCasualSetup.UI.MenuManager;
            LevelPlayer = (BaseLevelPlayer)_manager.CurrentLevel;
            _itemData = itemSet;
            if (_paths == null) {
                _paths = new List<ItemSet.ItemPath>();
            }
            if (closeButton != null) {
                closeButton.onClick.RemoveListener(FireHandleClose);
                closeButton.onClick.AddListener(FireHandleClose);
            }
            PrepareUpgradeButtons();
        }

        private void FireHandleClose() {
            if (handleClose != null) {
                handleClose(this);
            }
        }

        private void PrepareUpgradeButtons() {

            _paths.Clear();

            var sel = _itemData.NewRandomSelector(LevelPlayer.Context);
            int count = Mathf.Min(sel.AvailableCount, itemCount);

            buttons.Count = count;

            for (int i = 0; i < count; i++) {
                var path = sel.Select();
                _paths.Add(path);
                var ui = buttons[i];
                ui.tag = i;
                var item = path.Item;
                ui.textsTMPro[2].text = item.name;
                ui.buttons[0].OnClick -= IncrementButtonClick;
                ui.buttons[0].OnClick += IncrementButtonClick;
                ui.images[0].sprite = item.sprite;

                RefreshButtonVisual(i);
            }

        }

        public void RefreshButtonVisual(int index) {
            var button = buttons[index];
            var item = _paths[button.tag];
            int level = GetIncrementalLevel(Context, item);
            button.textsTMPro[0].text = "Level " + (level + 1);
            int cost = item.Item.GetCost(level);
            button.textsTMPro[1].text = cost.ToString();
            if (level >= this.maxLevels[index]) {
                button.SetState(2);
            }
            else if (cost <= paymentModule.GetCurrent(_manager.Context)) {
                button.SetState(0);
            }
            else {
                if (AddHandler != null && AddHandler.ShouldOfferAdd(this,item,level)) {
                    button.SetState(3);
                }
                else {
                    button.SetState(1);
                }
            }


        }

        private void IncrementButtonClick(UIItem arg1, int buttonIndex) {

            int itemIndex = arg1.tag;

            var item = _paths[itemIndex];
            var level = GetIncrementalLevel(Context, item);

            if (arg1.CurrentState == 3 && AddHandler!=null) {
                this.Interactable = false;
                AddHandler.ShowAdd(this, item, level, (result)=> {
                    this.Interactable = true;
                    if (result) {
                        IncrementItemDirect(arg1);
                    }
                });
            }
            else if (arg1.CurrentState == 0) {


                UIMoneyExtension.TryGetMainWallet(LevelPlayer, out var wallet);
                paymentModule.Change(LevelPlayer, wallet, "incremental-" + item.Item.name, -item.Item.GetCost(level));


                LevelPlayer.Score += 1;
                LevelPlayer.Score -= 1;

                IncrementItemDirect(arg1);
            }

        }


        private void IncrementItemDirect(UIItem arg1) {

            int itemIndex = arg1.tag;

            var item = _paths[itemIndex];
            IncreaseItemLevel(Context, item);

            arg1.PlayExtraAnim(0);
            if(arg1.TryGetEffect(0,out var e)) {
                e.Play();
            }

            for (int i = 0; i < buttons.Count; i++) {
                RefreshButtonVisual(i);
            }
            if (onIncrementalUpdate != null) {
                onIncrementalUpdate(itemIndex);
            }
            IncrementalListeners.FireIncrementalActions(LevelPlayer, itemIndex);
        }

        public int GetIncrementalLevel(AGameContext context, ItemSet.ItemPath item) {
            var setProgress = context.GameProgressValue.GetQuantityItemSet(item.set);
            return setProgress[item.id];
        }
        public void IncreaseItemLevel(AGameContext context, ItemSet.ItemPath item) {
            var val = context.GameProgressValue;
            var setProgress = val.GetQuantityItemSet(item.set);
            int level = setProgress[item.id];
            setProgress[item.id] = level + 1;
            context.GameProgress.SaveValue(val);
        }

        public interface IAddHandler {
            bool ShouldOfferAdd(IncrementalMenu menu, ItemSet.ItemPath item, int level);
            void ShowAdd(IncrementalMenu menu, ItemSet.ItemPath item, int level, Action<bool> result);
        }

    }
}