using Mobge;
using Mobge.HyperCasualSetup;
using Mobge.Telemetry;
using Mobge.UI;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI.CategorizedMarket {

    public class MarketPage : MonoBehaviour {

        public const int c_itemStateLocked = 0, c_itemStateOwned = 1, c_itemStateEquipped = 2;

        [OwnComponent] public Button unlockButton;
        public TextReference unlockButtonText;

        public RewardedButton unlockRewardedButton;
        public TextReference title;
        public Sprite lockedSprite;
        public string event_unlockItemPrefix;
        public string event_unlockItemSuffix;

        private Button _activeButton;

        [Header("image 1: higlight image")]
        [Header("image 0: item icon")]
        [Header("state 2: equipped")]
        [Header("state 1: owned")]
        [Header("state 0: locked")]
        public UICollection<UIItem> items;
        public UICollection<UIItem> dummyItems;
        public int fillWithDummiesUntil;

        public int itemValue_uiType = -1;
        [Header("FX")]
        [SerializeField]
        private ReusableReference unlockEffect;
        [SerializeField]
        private ReusableReference unlockSoundEffect;

        private HashSet<int> _lockedIndexes;
        private List<int> _tempIndexes;
        private int _index;
        private MarketPageGroup _group;
        private ItemSet.ItemPath[] _items;

        public MarketPageGroup.PageInfo Info => _group.pageInfos[_index];
        public AGameContext Context => _group.Menu.MenuManager.Context;
        private const string c_unlockRandomCountPrefix = "unlock_random_count";

        private bool CanUnlock => Info.UnlockWithAd || Info.GetCost(UnlockRandomCount) <= _group.PaymentModule.GetCurrent(_group.Menu.GameContext);

        private RandomSelectorByCost _randomSelector;

        string rwPrefix = "rw_market_";

        private int UnlockRandomCount {
            get {
                int unlockCount = 0;
                for (int i = 0; i < _items.Length; i++) {
                    var itemPath = _items[i];
                    if (Context.GameProgressValue.GetItemSet(itemPath.set).HasItem(itemPath.id)) {
                        unlockCount++;
                    }
                }
                return unlockCount;
            }
        }

        internal void Initialize(MarketPageGroup group, int index, ItemSet.ItemPath[] items) {
            _index = index;
            _group = group;
            _items = items;

            var info = Info;

            title.Text = info.title;

            bool ad = info.UnlockWithAd;
            unlockButton.gameObject.SetActive(!ad);
            unlockButton.onClick.RemoveListener(UnlockAction);
            unlockButton.onClick.AddListener(UnlockAction);

            unlockRewardedButton.Configure(_group.Menu.MenuManager);
            unlockRewardedButton.Button.gameObject.SetActive(ad && !info.unlockMultipleRewardedMode);
            if (!info.unlockMultipleRewardedMode) {
                unlockRewardedButton.Button.onClick.RemoveListener(UnlockAction);
                unlockRewardedButton.Button.onClick.AddListener(UnlockAction);
            }

            if (info.disableUnlockButton) {
                unlockButton.gameObject.SetActive(false);
                unlockRewardedButton.Button.gameObject.SetActive(false);
            }


            _activeButton = ad ? unlockRewardedButton.Button : unlockButton;
            _activeButton.interactable = CanUnlock;

            if (!ad) {
                unlockButtonText.Text = info.GetCost(UnlockRandomCount).ToString();
            }

            if (_lockedIndexes == null) {
                _lockedIndexes = new HashSet<int>();
            }
            else {
                _lockedIndexes.Clear();
            }
            if (itemValue_uiType < 0) {
                this.items.Count = _items.Length;
            }
            else {
                this.items.Count = 0;
            }
            for (int i = 0; i < _items.Length; i++) {
                UIItem element;

                if (itemValue_uiType < 0) {
                    element = this.items[i];
                }
                else {
                    element = this.items.AddElement((int)_items[i].Item.GetValueSafe(itemValue_uiType, 0));
                }

                PrepareElement(i, element, _group.Menu.GameContext);

            }
            if (dummyItems.itemRes != null) {
                int dummyCount = Mathf.Max(0, fillWithDummiesUntil - _items.Length);
                dummyItems.Count = dummyCount;
            }
            else {
                dummyItems.Count = 0;
            }
            UpdateButtonState();
        }
        public ItemSet.ItemPath GetItemPath(int tag) => _items[tag];
        private void UpdateButtonState() {
            _activeButton.interactable = _lockedIndexes.Count > 0 && CanUnlock;
            unlockButtonText.Text = Info.GetCost(UnlockRandomCount).ToString();
        }

        private void PrepareElement(int index, UIItem element, AGameContext context) {
            var item = _items[index];
            element.images[0].sprite = item.Item.sprite;
            element.images[1].enabled = false;
            element.tag = index;
            element.buttons[0].OnClick -= EquipButtonClicked;
            element.buttons[0].OnClick += EquipButtonClicked;

            if (Info.UnlockWithAd && !Info.disableUnlockButton) {
                if (Info.unlockMultipleRewardedMode) {
                    RewardedButtonAdapter rewardedButtonAdapter = element.buttons[1].button.GetComponent<RewardedButtonAdapter>();
                    var adCountTracker = ItemAdCountTracker.GetTracker(context);
                    rewardedButtonAdapter.Configure(_group.Menu.MenuManager, adCountTracker.GetAdCount(item), Info.partialUnlockAdCost);
                    element.buttons[1].button.gameObject.SetActive(true);
                    element.buttons[1].OnClick -= UnlockWithAdsButtonClicked;
                    element.buttons[1].OnClick += UnlockWithAdsButtonClicked;
                }
            }

            RefreshState(element, context);
        }

        private void RefreshState(UIItem element, AGameContext context) {
            var item = _items[element.tag];



            if (item.set.stackable) {
                element.SetState(1);
                _lockedIndexes.Add(element.tag);
            }
            else {
                bool unlocked = false;

                int count = ItemAdCountTracker.GetTracker(context).GetAdCount(item);
                if (Info.UnlockWithAd && count >= Info.partialUnlockAdCost) {
                    unlocked = true;
                }

                if (context.GameProgressValue.GetItemSet(item.set).HasItem(item.id)) {
                    unlocked = true;
                }

                if (unlocked) {
                    _lockedIndexes.Remove(element.tag);
                    bool equipped = context.GameProgressValue.GetItemSet(item.set).EquippedItem == item.id;
                    if (equipped) {
                        if (_group.LastEquippedItem != element) {
                            if (_group.LastEquippedItem != null) {
                                _group.LastEquippedItem.SetState(1);
                                if (_group.LastEquippedItemPage.Info.UnlockWithAd && _group.LastEquippedItemPage.Info.unlockMultipleRewardedMode) { _group.LastEquippedItem.SetState(_group.LastEquippedItem.CurrentState + 3); }
                            }
                            _group.LastEquippedItem = element;
                            _group.LastEquippedItemPage = this; // TODO DOA remove this once a/b is complete
                        }
                        element.SetState(2);
                    }
                    else {
                        element.SetState(1);
                    }
                }
                else {
                    _lockedIndexes.Add(element.tag);
                    element.SetState(0);
                }



            }

            if (Info.UnlockWithAd && Info.unlockMultipleRewardedMode) { element.SetState(element.CurrentState + 3); }



        }
        private void EquipButtonClicked(UIItem arg1, int arg2) {
            if (arg1.CurrentState == 1 || arg1.CurrentState == 2 || arg1.CurrentState == 1 + 3) {
                var path = _items[arg1.tag];
                if (!path.set.stackable) {
                    _group.Menu.EquipItem(path.set, path.id);
                    RefreshState(arg1, _group.Menu.GameContext);

                    _group.Menu.GameContext.FireAnalyticsEvent(_group.event_itemEquipped, new Dictionary<string, string> {
                        { "item_name", path.Item.name },
                        { "item_type", path.set.tag }
                    });
                }
            }
        }

        private void UnlockWithAdsButtonClicked(UIItem arg1, int arg2) {
            ItemSet.ItemPath path = _items[arg1.tag];
            var adCT = ItemAdCountTracker.GetTracker(_group.Menu.GameContext);
            int count = adCT.GetAdCount(path);
            if (count >= Info.partialUnlockAdCost) return;

            string eventName = _group.event_rewarded + "_" + Info.title;

            _group.Menu.Interactable = false;
            if (arg1.buttons[1].button.TryGetComponent(out RewardedButtonAdapter rw)) {
                rw.RewardedButton.Button.interactable = false;
                rw.SetState(RewardedButton.ButtonState.Loading);
            }

            _group.Menu.GameContext.FireAnalyticsEvent(eventName, new Dictionary<string, string> {
                { "item_name",  path.Item.name},
                { "videos_watched", count.ToString() }
            });

            var context = _group.Menu.GameContext;

            context.ClaimReward(null,
                (r) => {
                    switch (r) {
                        default:
                        case AGameContext.ClaimResult.Failed:
                            break;
                        case AGameContext.ClaimResult.Canceled:
                            _group.Menu.GameContext.FireAnalyticsEvent(rwPrefix + "skipped");

                            break;
                        case AGameContext.ClaimResult.Claimed:
                            _group.Menu.GameContext.FireAnalyticsEvent(rwPrefix + "completed");

                            adCT.SetAdCount(path);
                            count++;
                            if (count >= Info.partialUnlockAdCost) {
                                UnlockDirect(arg1);
                            }
                            else {
                                if (rw != null) {
                                    rw.RewardedButton.CurrentRwCount = count;
                                }
                            }
                            break;
                    }
                    if (rw != null) {
                        rw.RewardedButton.Button.interactable = true;
                        rw.SetState(RewardedButton.ButtonState.Normal);
                    }
                    _group.Menu.Interactable = true;
                });

        }


        private void UnlockAction() {
            bool ad = Info.UnlockWithAd;

            if (ad) {
                string eventName = _group.event_rewarded + "_" + Info.title;

                _group.Menu.Interactable = false;
                unlockRewardedButton.State = RewardedButton.ButtonState.Loading;

                var context = (GameContextWithAnalytics)_group.Menu.GameContext;
                context.ClaimReward(rwPrefix + "started",
                    (r) => {
                        switch (r) {
                            case AGameContext.ClaimResult.Claimed:
                                UnlockRandomDirect();
                                _group.Menu.GameContext.FireAnalyticsEvent(rwPrefix + "completed");
                                _group.Menu.GameContext.FireAnalyticsEvent(eventName, new Dictionary<string, string> {
                                    { "item_name", "random" },
                                    { "videos_watched", "0" }
                                });
                                break;
                            case AGameContext.ClaimResult.Failed:
                                break;
                            case AGameContext.ClaimResult.Canceled:
                                if (Application.isEditor) {
                                    UnlockRandomDirect();
                                }
                                else {
                                    _group.Menu.GameContext.FireAnalyticsEvent(rwPrefix + "skipped");
                                }
                                break;
                            default:
                                break;
                        }

                        _group.Menu.Interactable = true;
                        unlockRewardedButton.State = RewardedButton.ButtonState.Normal;
                    });
            }
            else {
                if (CanUnlock) {
                    UnlockRandomDirect();
                }
            }
        }

        private void UnlockRandomDirect() {
            const int hImage = 1;
            _group.Menu.Interactable = false;
            if (_tempIndexes == null) {
                _tempIndexes = new List<int>();
            }
            else {
                _tempIndexes.Clear();
            }
            var e = _lockedIndexes.GetEnumerator();
            while (e.MoveNext()) {
                _tempIndexes.Add(e.Current);
            }
            int lastIndex = _tempIndexes[Random.Range(0, _tempIndexes.Count)];
            int lastNumber = 0;
            int maxValue = -1;
            int targetIndex = _randomSelector.Select(this, _tempIndexes);
            items[lastIndex].images[hImage].enabled = true;
            _group.Menu.ActionManager.DoAction((completed, data) => {
                if (completed) {
                    _group.Menu.Interactable = true;
                    var element = items[lastIndex];
                    element.images[hImage].enabled = false;
                    UnlockDirect(element);
                }
            },
            _group.Menu.unlockAnimTime,
            (progress, data) => {
                var number = (int)_group.Menu.unlockIndexChangeTimes.Evaluate(progress);
                if (number != lastNumber) {
                    items[lastIndex].images[hImage].enabled = false;
                    if (number > lastNumber) {
                        if (maxValue < number) {
                            lastIndex = _tempIndexes[Random.Range(0, _tempIndexes.Count)];
                            maxValue = number;
                        }
                        items[lastIndex].images[hImage].enabled = true;
                    }
                    else { // number < lastNumber
                        lastIndex = targetIndex;
                    }
                    lastNumber = number;
                }
            });
        }

        private void UnlockDirect(UIItem element) {
            var item = _items[element.tag];

            _group.unlockEffect.SpawnItem(Vector3.zero, element.transform);

            _group.Menu.BuyItem(item.set, item.id, Info.buyQuantity, true, Info.GetCost(UnlockRandomCount));

            RefreshState(element, _group.Menu.GameContext);

            UpdateButtonState();
            if (_group.Menu.totalScore != null) {
                _group.Menu.totalScore.UpdateScores();
            }
            unlockEffect.SpawnItem(Vector3.zero, element.transform);
            unlockSoundEffect.SpawnItem(Vector3.zero, element.transform);

            _group.Menu.MenuManager.Context.FireAnalyticsEvent(_group.event_itemUnlocked, new Dictionary<string, string> {
                { "item_name",  item.Item.name },
                { "item_type", item.set.tag }
            });
        }

        public struct RandomSelectorByCost {

            private List<int> _selecteds;

            public int Select(MarketPage page, List<int> indexes) {
                if (_selecteds == null) {
                    _selecteds = new List<int>();
                }
                else {
                    _selecteds.Clear();
                }
                int lastCost = int.MaxValue;
                for(int i = 0; i < indexes.Count; i++) {
                    int index = indexes[i];
                    int cost = page._items[index].Item.cost;
                    if (cost < lastCost) {
                        lastCost = cost;
                        _selecteds.Clear();
                        _selecteds.Add(index);
                    }
                    else if (cost == lastCost) {
                        _selecteds.Add(index);
                    }
                }
                return _selecteds[UnityEngine.Random.Range(0, _selecteds.Count)];
            }
        }
    }
}