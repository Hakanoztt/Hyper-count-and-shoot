using Mobge.Core;
using Mobge.HyperCasualSetup;
using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mobge.IdleGame.UI {
    public class UIMoneyExtension : MonoBehaviour, BaseMenu.IExtension {

        private const string c_walletKey = "mainWllt";
        private const string c_extensionListKey = "mnyExtLst";

        public static void SetMainWallet(LevelPlayer player, WalletComponent mainWallet) {
            player.SetExtra(c_walletKey, mainWallet);
            var ext = GetExtensions(player);
            for(int i = 0; i < ext.Count; i++) {
                ext[i].UpdateUI(mainWallet);
                mainWallet.onChange += ext[i].UpdateUI;
            }
            ext.Clear();
        }

        public static bool TryGetMainWallet(LevelPlayer player, out WalletComponent wallet) {
            return player.TryGetExtra(c_walletKey, out wallet);
        }

        private static void RegisterUI(LevelPlayer player, UIMoneyExtension ext) {
            if(TryGetMainWallet(player, out var wallet)) {
                ext.UpdateUI(wallet);
                wallet.onChange += ext.UpdateUI;
            }
            else {
                GetExtensions(player).Add(ext);
            }
        }


        private static List<UIMoneyExtension> GetExtensions(LevelPlayer player) {
              if(!player.TryGetExtra<List<UIMoneyExtension>>(c_extensionListKey,out var l)) {
                l = new List<UIMoneyExtension>();
                player.SetExtra(c_extensionListKey, l);
            }
            return l;
        }

        public MultipleCostPanel costCollection;
        public ItemSet currencies;

        private HyperCasualSetup.UI.MenuManager _manager;

        public AGameContext Context => _manager.Context;

        public int showCurrencyIfEmpty;

        private ItemCluster _items;

        void BaseMenu.IExtension.Prepare(BaseMenu menu) {
            _manager = (HyperCasualSetup.UI.MenuManager)menu.MenuManager;


            UpdateCluster();
            costCollection.UpdateCost(currencies, _items.Items, showCurrencyIfEmpty);
            RegisterUI(_manager.CurrentPlayer, this);
        }




        private void UpdateCluster() {
            if (_items == null) {
                _items = new ItemCluster(currencies);
            }
            _items.UpdateFromItemSet(Context);
            
        }


        private void UpdateUI(WalletComponent obj) {
            costCollection.UpdateCost(currencies, obj.Items, showCurrencyIfEmpty);
        }




    }
}