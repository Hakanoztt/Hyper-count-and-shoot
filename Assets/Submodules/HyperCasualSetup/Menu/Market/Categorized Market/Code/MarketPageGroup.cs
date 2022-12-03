using ElephantSDK;
using Mobge;
using Mobge.HyperCasualSetup;
using Mobge.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace Mobge.HyperCasualSetup.UI.CategorizedMarket {

    public class MarketPageGroup : MonoBehaviour {

        public UIPageCollection pageCollection;
        public PageInfo[] pageInfos;
        public string remote_unlockCosts = "market_unlock_random_costs";
        public string event_buy = "skin_unlocked_market";
        public string event_rewarded = "rw_skin_unlocked_market";
        public string event_itemUnlocked = "item_unlocked";
        public string event_itemEquipped = "item_equipped";

        public string event_itemTapFormat = "store_tap_{0}";

        public Sprite categoryIcon;


        private Mobge.HyperCasualSetup.ItemGroupData itemGroup;

        public ReusableReference unlockEffect;

        private CategorizedMarketMenu _menu;
        private float[] _unlockCosts;

        private bool _firstTime = true;
        private List<Mobge.HyperCasualSetup.ItemSet.ItemPath> _tempItems;

        private Mobge.HyperCasualSetup.UI.MenuManager MenuManager => _menu.MenuManager;
        public MenuPaymentModule PaymentModule => _menu.paymentModule;
        public CategorizedMarketMenu Menu => _menu;
        internal UIItem LastEquippedItem { get; set; }
        internal MarketPage LastEquippedItemPage { get; set; }

        public void Prepare(CategorizedMarketMenu menu, ItemGroupData itemGroup, out bool hasItem) {
            hasItem = false;
            _menu = menu;
            if (_firstTime) {
                _firstTime = false;
                _tempItems = new List<Mobge.HyperCasualSetup.ItemSet.ItemPath>();
                ReadRemote();
                for (int i = 0; i < pageInfos.Length; i++) {
                    ref var info = ref pageInfos[i];
                    info.ReadRemote();
                }
            }
            this.itemGroup = itemGroup;

            pageCollection.PageCount = 0;

            for (int i = 0; i < pageInfos.Length; i++) {
                var pi = pageInfos[i];

                if (pi.itemGroupSetIndex < itemGroup.Groups.Length) {
                    var g = itemGroup.Groups[pi.itemGroupSetIndex];
                    _tempItems.Clear();
                    g.GetAllItems(_tempItems);

                    if (_tempItems.Count > 0) {
                        pageCollection.AddPage(i % pageCollection.pageResources.Length);
                        hasItem = true;
                        var page = pageCollection.GetPage<MarketPage>(i);
                        page.Initialize(this, i, _tempItems.ToArray());
                    }
                }
            }

            if (hasItem) {
                pageCollection.CurrentPage = 0;
            }

            pageCollection.currentPageChanged -= PageChanged;
            pageCollection.currentPageChanged += PageChanged;

            Menu.onItemBought -= ItemBought;
            Menu.onItemBought += ItemBought;
        }

        private void ReadRemote() {
            var rc = RemoteConfig.GetInstance();
            var unlockCosts = rc.Get(remote_unlockCosts, null);
            _unlockCosts = null;
            if (unlockCosts != null) {
                var pcs = unlockCosts.Split(',');
                if (pcs != null) {
                    _unlockCosts = new float[pcs.Length];
                    for (int i = 0; i < pcs.Length; i++) {
                        _unlockCosts[i] = float.Parse(pcs[i]);
                    }

                    ApplyRemoteValues();
                }
            }


        }
        void ApplyRemoteValues() {
            var group = this;
            if (group.pageInfos != null && _unlockCosts != null) {
                int count = Mathf.Min(_unlockCosts.Length, group.pageInfos.Length);
                for (int i = 0; i < count; i++) {
                    var cost = _unlockCosts[i];
                    var pi = group.pageInfos[i];
                    pi.unlockCost.cost = cost;

                    group.pageInfos[i] = pi;
                }
            }
        }

        private void PageChanged(UIPageCollection pages, int oldPage, int newPage) {
            MarketPage page = pages.GetPage<MarketPage>(newPage);

            var eventName = string.Format(event_itemTapFormat, page.Info.LowerCaseTitle);


            MenuManager.Context.FireAnalyticsEvent(eventName);

        }

        private void ItemBought(ItemSet itemSet, int index) {
            Menu.GameContext.FireAnalyticsEvent(event_itemEquipped, new Dictionary<string, string> {
                { "item_name", itemSet.items[index].name },
                { "item_type", itemSet.tag }
            });
        }
        public ItemEnumerator GetItemEnumerator() {
            return new ItemEnumerator(this);
        }
        [Serializable]
        public struct PageInfo {
            public string title;
            public int itemGroupSetIndex;
            public bool unlockMultipleRewardedMode;
            //public float unlockCost;
            public UnlockCost unlockCost;
            public int buyQuantity;
            [Tooltip("Read iff Unlock Multiple Rewarded Mode is enabled. This amount of ads need to be watched to claim the items in this category.")]
            public int partialUnlockAdCost;
            public bool disableUnlockButton;
            public string event_itemUnlocked;
            public string remote_unlockMultipleRewardedMode;
            public string remote_unlockCost;
            public string remote_geometricCost;
            public string remote_costIncreaseForLevel;
            public string remote_buyQuantity;
            public string remote_partialUnlockAdCost;

            public int GetCost(int level) {
                float cost = unlockCost.cost;
                var result = cost + unlockCost.costIncreaseForLevel * level;
                if (unlockCost.geometricCostIncrease != 0) {
                    result *= Mathf.Pow(unlockCost.geometricCostIncrease, level);
                }
                return Mathf.RoundToInt(Mathf.Min(result, unlockCost.maxCost));
            }

            public bool UnlockWithAd => unlockCost.cost == 0;


            private string _lowerCaseTitle;
            public string LowerCaseTitle {
                get {
                    EnsureLowerCase();
                    return _lowerCaseTitle;
                }
            }

            private void EnsureLowerCase() {
                if (_lowerCaseTitle == null) {
                    _lowerCaseTitle = title.ToLower();
                }
            }

            public void ReadRemote() {
                EnsureLowerCase();
                var remote = RemoteConfig.GetInstance();
                if (!string.IsNullOrWhiteSpace(remote_unlockMultipleRewardedMode)) {
                    unlockMultipleRewardedMode = remote.GetBool(remote_unlockMultipleRewardedMode, unlockMultipleRewardedMode);
                }
                if (!string.IsNullOrWhiteSpace(remote_unlockCost)) {
                    unlockCost.cost = remote.GetFloat(remote_unlockCost, unlockCost.cost);
                }
                if (!string.IsNullOrWhiteSpace(remote_geometricCost)) {
                    unlockCost.cost = remote.GetFloat(remote_geometricCost, unlockCost.geometricCostIncrease);
                }
                if (!string.IsNullOrWhiteSpace(remote_costIncreaseForLevel)) {
                    unlockCost.cost = remote.GetFloat(remote_costIncreaseForLevel, unlockCost.costIncreaseForLevel);
                }
                if (!string.IsNullOrWhiteSpace(remote_buyQuantity)) {
                    buyQuantity = remote.GetInt(remote_buyQuantity, buyQuantity);
                }
                if (!string.IsNullOrWhiteSpace(remote_partialUnlockAdCost)) {
                    partialUnlockAdCost = remote.GetInt(remote_partialUnlockAdCost, partialUnlockAdCost);
                }

            }

            [Serializable]
            public struct UnlockCost {
                public float cost;
                public float geometricCostIncrease;
                public float costIncreaseForLevel;
                public float maxCost;
            }
        }
        public struct ItemEnumerator {
            private MarketPageGroup _group;
            private int _pageIndex;
            private int _itemIndex;
            private int _currentPageItemCount;
            internal ItemEnumerator(MarketPageGroup group) {
                _group = group;
                _pageIndex = -1;
                _itemIndex = -1;
                _currentPageItemCount = 0;
            }

            public bool MoveNext() {
                _itemIndex++;
                if (_itemIndex >= _currentPageItemCount) {
                    _pageIndex++;
                    while (_pageIndex < _group.pageCollection.PageCount) {
                        var currenpPage = _group.pageCollection.GetPage<MarketPage>(_pageIndex);
                        if (currenpPage.items.Count > 0) {
                            _itemIndex = 0;
                            _currentPageItemCount = currenpPage.items.Count;
                            return true;
                        }
                        _pageIndex++;
                    }
                    return false;


                }
                return true;
            }
            public int CurrentPage { get => _pageIndex; }
            public UIItem CurrentUIItem {
                get {
                    var page = _group.pageCollection.GetPage<MarketPage>(_pageIndex);
                    var item = page.items[_itemIndex];
                    return item;
                }
            }
            public ItemSet.ItemPath Current {
                get {
                    var page = _group.pageCollection.GetPage<MarketPage>(_pageIndex);
                    var item = page.items[_itemIndex];
                    return page.GetItemPath(item.tag);
                }
            }
        }
    }
}