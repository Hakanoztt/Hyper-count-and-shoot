using ElephantSDK;
using Mobge.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI.CategorizedMarket {
    public class CategorizedMarketMenu : BaseMarketMenu {
        public CategoryManager categoryManager;

        public ScoreExtension totalScore;

        public bool openEquippedPageAfterPrepare = false;

        public float unlockAnimTime;
        public AnimationCurve unlockIndexChangeTimes;

        private MenuManager _menuManager;

        public new MenuManager MenuManager => _menuManager;

        public ItemGroupData itemGroup;

        public override void Prepare() {
            base.Prepare();
            _menuManager = (Mobge.HyperCasualSetup.UI.MenuManager)base.MenuManager;
            categoryManager.Initialize(this);
        }

        public void OpenPage(int idx) {
            if (idx < 0 || idx >= categoryManager.CurrentPage.pageInfos.Length) return;
            categoryManager.CurrentPage.pageCollection.CurrentPage = idx;
        }

        private bool OpenPageWithEquippedItem(MarketPageGroup page) {
            var items = page.GetItemEnumerator();
            while (items.MoveNext()) {
                var item = items.CurrentUIItem;
                if (item.CurrentState % 3 == MarketPage.c_itemStateEquipped) {
                    page.pageCollection.CurrentPage = items.CurrentPage;
                    return true;
                }
            }
            return false;
        }

        public void ClaimReward(string itemName, int adsWatched, string eventName, Action<AGameContext.ClaimResult> onStateChange) {
            GameContext ctx = MenuManager.Context as GameContext;
            if (ctx == null) { return; }
            string adCount = adsWatched.ToString();
            ctx.FireAnalyticsEvent(eventName, new Dictionary<string, string> {
                { "item_name", itemName },
                { "videos_watched", adCount }
            });
            ctx.ClaimReward(null, onStateChange);
        }

        protected override void OnOpen() {
            base.OnOpen();
            var ctx = _menuManager.Context;
            ctx.FireAnalyticsEvent("store_opened");

            if (!openEquippedPageAfterPrepare) {
                OpenPage(0);
            }
        }

        protected override void OnClose() {
            base.OnClose();
            var ctx = _menuManager.Context;
            ctx.FireAnalyticsEvent("store_closed");
        }


        [Serializable]
        public class CategoryManager {
            public int initialPage = 0;
            public Action<int> OnPageChanged;
            public UICollection<UIItem> toggleCollection;

            public ToggleGroup toggleGroup;

            public Color defaultColor;
            public Color selectedColor;

            private int _currentPage;

            [SerializeField] private MarketPageGroup[] pages;
            public int Count => pages.Length;
            public MarketPageGroup CurrentPage => pages[_currentPage];

            public MarketPageGroup this[int index] {
                get => pages[index];
            }
            private Toggle GetToggle(int index) {
                return (Toggle)toggleCollection[index].extraReferences[0];
            }
            public void Initialize(CategorizedMarketMenu group) {
                var ctx = group._menuManager.Context;

                bool firstPageFound = false;

                toggleCollection.Count = 0;
                toggleCollection.Count = pages.Length;
                
                int enabledToggleCount = 0;

                for (int i = 0; i < toggleCollection.Count; i++) {
                    var ui = toggleCollection[i];
                    var tog = GetToggle(i);
                    int index = i;

                    pages[i].Prepare(group, group.itemGroup, out bool hasItem);

                    if (group.openEquippedPageAfterPrepare) {
                        group.OpenPageWithEquippedItem(pages[i]);
                    }

                    pages[i].gameObject.SetActive(hasItem && !firstPageFound);
                    tog.gameObject.SetActive(hasItem);
                    tog.isOn = false;
                    if (hasItem && !firstPageFound) {
                        firstPageFound = true;
                        _currentPage = i;
                        tog.isOn = true;
                    }

                    if (hasItem) {
                        enabledToggleCount++;
                        tog.onValueChanged.RemoveAllListeners();
                        tog.onValueChanged.AddListener((isOn) => {
                            pages[index].gameObject.SetActive(isOn);
                            if (isOn && OnPageChanged != null) {
                                OnPageChanged(index);
                            }
                            ui.images[1].color = isOn ? selectedColor : defaultColor;
                        });
                        ui.images[0].sprite = pages[i].categoryIcon;
                    }

                }


                if (enabledToggleCount < 2) {
                    toggleGroup.gameObject.SetActive(false);
                }
                else {

                    toggleGroup.gameObject.SetActive(true);
                    toggleCollection[_currentPage].images[1].color = selectedColor;
                }

            }

        }

    }

}