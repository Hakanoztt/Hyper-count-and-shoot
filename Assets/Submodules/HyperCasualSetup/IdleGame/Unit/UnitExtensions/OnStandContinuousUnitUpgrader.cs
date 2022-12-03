using Mobge.Animation;
using Mobge.HyperCasualSetup;
using Mobge.IdleGame.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame {

    public sealed class OnStandContinuousUnitUpgrader : TriggerUnitExtension {

        public override string TriggerTag => WalletComponent.c_tag;

        [SerializeField] private float totalTransactionTime = 0.5f;
        [SerializeField] private float transactionTick = 0.08f;

        private float AmountPerTickRate => transactionTick/totalTransactionTime;

        public float delay = 0.5f;

        public MultipleCostPanel panel;


        [Header("Effects")]
        public ReusableReference transactionEffect;
        public AReusableItem highlightEffect;

        public UnityEngine.UI.Image fillImage;

        private RoutineManager.Routine _transactionRoutine;

        private TransactionProgressData _progressData;


        private ExposedList<TransactionInfo> _transactions;

        public bool IsUnderTransaction => !_transactionRoutine.IsFinished;

        public float Time => UnityEngine.Time.time;

        public Action<OnStandContinuousUnitUpgrader, WalletComponent> onUpgrade;



        protected override void Initialize(IUnit unit) {
            bool canBeUpgraded = unit.CanBeUpgraded();
            if (canBeUpgraded) {

                _transactions = new ExposedList<TransactionInfo>();
                _progressData = new TransactionProgressData(Unit.Spawner.Player.Context, unit.Spawner.GetUpgradeCost(), Unit.Spawner.UniqueId + "_oscuu");
            }
            UpdateForNextLevel(canBeUpgraded);
            UpdateFillImage();
        }
        private void UpdateForNextLevel(bool canBeUpgraded) {
            if(Unit == null) {
                return;
            }
            if (canBeUpgraded) {
                gameObject.SetActive(true);
                panel.UpdateCost(_progressData.LeftAmount);
            }
            else {
                gameObject.SetActive(false);
            }
        }
        private bool IsIncluded(WalletComponent wallet) {
            for(int i = 0; i < _transactions.Count; i++) {
                if(_transactions.array[i].Wallet == wallet) {
                    return true;
                }
            }
            return false;
        }
        protected override void TriggerEntered(Collider trigger, int newCount) {
            if (!Unit.CanBeUpgraded()) { return; }
            if (WalletComponent.TryGet(trigger, out var wallet) && !IsIncluded(wallet)) {
                int index = _transactions.AddFast();
                ref var transaction = ref _transactions.array[index];
                transaction.Initialize(wallet, this);
                transaction.effect = transactionEffect.SpawnItem(Vector3.zero, wallet.transform);


                if (_transactionRoutine.IsFinished) {
                    SetTransactionEnabled(true);
                }
            }
        }

        private void UpdateTransactions(float progress, object data) {
            bool anyTransactionDone = false;
            int lastPayingIndex = -1;
            for(int i = 0; i < _transactions.Count; i++) {
                ref var trans = ref _transactions.array[i];
                if (trans.Update(this)) {
                    if (!anyTransactionDone) {
                        anyTransactionDone = true;
                        lastPayingIndex = i;
                    }
                }

            }
            if (anyTransactionDone) {
                var nextUpgradeCost = this.Unit.Spawner.GetUpgradeCost();
                if (_progressData.CurrentAmount.Contains(nextUpgradeCost, out _)) {
                    Unit.Spawner.Upgrade();
                    SetTransactionEnabled(false);
                    _progressData.CurrentAmount.Clear();
                    _progressData.Save(this.Unit.Spawner.Player.Context);


                    bool canBeUpgraded = Unit.Spawner.CanBeUpgraded;
                    if (canBeUpgraded) {
                        _progressData.RefreshLeftAmount(Unit.Spawner.GetUpgradeCost());
                    }

                    UpdateForNextLevel(canBeUpgraded);

                    if (onUpgrade != null) {
                        onUpgrade(this, _transactions.array[lastPayingIndex].Wallet);
                    }
                    _transactions.ClearFast();


                } else {
                    
                    _progressData.Save(Unit.Spawner.Player.Context);

                    panel.UpdateCost(_progressData.LeftAmount);
                }

                UpdateFillImage();
            }
        }
        private void SetTransactionEnabled(bool enabled) {
            if (enabled) {
                _transactionRoutine = Unit.Spawner.Player.RoutineManager.DoRoutine(UpdateTransactions);
                if (this.highlightEffect != null) {
                    this.highlightEffect.Play();

                }
            }
            else {
                _transactionRoutine.Stop();
                if (this.highlightEffect != null) {
                    this.highlightEffect.Stop();
                }
            }
        }

        protected override void TriggerExited(Collider trigger, int newCount) {

            for(int i = 0; i < _transactions.Count; i++) {
                ref var trans = ref _transactions.array[i];
                if (trans.TryFinish(trigger)) {
                    _transactions.Swap(i, _transactions.Count - 1);
                    _transactions.RemoveLastFast();
                    break;
                }
            }
            if(_transactions.Count == 0) {
                SetTransactionEnabled(false);
            }
        }

        private void UpdateFillImage() {
            if (fillImage != null) {
                if (_progressData.CurrentAmount == null) {
                    fillImage.fillAmount = 0;
                    return;
                }

                ItemCluster paid = _progressData.CurrentAmount;
                ItemCluster cost = Unit.Spawner.GetUpgradeCost();

                if (cost != null && cost.set != null) {
                    float numerator = 0, denominator = 0;
                    for (int i = 0; i < paid.Items.Count; i++) {
                        numerator += paid.Items[i].count;
                    }
                    for (int i = 0; i < cost.Items.Count; i++) {
                        denominator += cost.Items[i].count;
                    }
                    fillImage.fillAmount = numerator / denominator;
                }
                else {
                    fillImage.fillAmount = 0;
                }
            }
        }

        private struct TransactionInfo {
            public AReusableItem effect;
            private ItemCluster _amountPerTick;
            private float _nextWithdrawTime;
            private WalletComponent _wallet;
            
            public WalletComponent Wallet => _wallet;

            public void Initialize(WalletComponent wallet, OnStandContinuousUnitUpgrader upgrader) {
                if (_amountPerTick == null) {
                    _amountPerTick = new ItemCluster(wallet.CurrencySet);
                }
                else {
                    _amountPerTick.Clear();
                }
                _wallet = wallet;
                var cost = upgrader.Unit.Spawner.GetUpgradeCost();
                var data = upgrader._progressData.CurrentAmount;
                for(int i = 0; i < cost.Items.Count; i++) {
                    var costContent = cost.Items[i];
                    int upgradeCount = data.GetCount(costContent.id, out _);
                    _amountPerTick.Add(costContent.id, costContent.count - upgradeCount);
                }
                float amountPerTickRate = upgrader.AmountPerTickRate;
                var aIts = _amountPerTick.Items;
                for (int i = 0; i <aIts.Count; i++) {
                    var content = aIts[i];
                    content.count = Mathf.CeilToInt(content.count * amountPerTickRate);
                    aIts[i] = content;
                }
                _nextWithdrawTime = upgrader.Time + upgrader.delay;
            }
            public bool Update(OnStandContinuousUnitUpgrader upgrader) {

                float time = upgrader.Time;
                if(_nextWithdrawTime > time) {
                    return false;
                }
                _nextWithdrawTime = time + upgrader.transactionTick;
                bool transferred = false;
                ref var prg = ref upgrader._progressData;
                for (int i = 0; i < _amountPerTick.Items.Count; i++) {
                    var content = _amountPerTick.Items[i];
                    content.count = Mathf.Min(content.count, _wallet.GetAmount(content.id));
                    content.count = Mathf.Min(content.count, prg.LeftAmount.GetCount(content.id, out _));
                    if (content.count > 0) {
                        prg.LeftAmount.TryRemove(content);
                        _wallet.TryRemove(content);

                        prg.CurrentAmount.Add(content.id, content.count);


                        transferred = true;

                    }
                }
                return transferred;
            }

            public bool TryFinish(Collider collider) {
                if (_wallet != null) {
                    if (_wallet.HasCollider(collider)) {
                        _wallet = null;
                        return true;
                    }
                }
                return false;
            }

        }

        [Serializable]
        public struct TransactionProgressData {
            private ItemCluster _currentAmount;
            private string _saveKey;
            private ItemCluster _leftAmount;

            public ItemCluster CurrentAmount => _currentAmount;
            public ItemCluster LeftAmount => _leftAmount;

            public void Save(AGameContext context) {
                var val = context.GameProgressValue;
                var data = val.GetCustomQuantityData(_saveKey);
                data.Clear();
                var its = _currentAmount.Items;
                for(int i = 0; i < its.Count; i++) {
                    data.AddItem(its[i].id, its[i].count);
                }

                context.GameProgress.SaveValue(val);
            }

            public TransactionProgressData(AGameContext context, ItemCluster nextUpgradeCost, string saveKey) {
                _saveKey = saveKey;
                var data = context.GameProgressValue.GetCustomQuantityData(_saveKey);
                _currentAmount = new ItemCluster(nextUpgradeCost.set);
                _leftAmount = new ItemCluster(nextUpgradeCost.set);
                var en = data.GetQuantityEnumerator();
                while (en.MoveNext()) {
                    var p = en.Current;
                    _currentAmount.Add(p.key, p.quantity);
                }
                RefreshLeftAmount(nextUpgradeCost);
            }

            public void RefreshLeftAmount(ItemCluster nextUpgradeCount) {
                if (nextUpgradeCount == null) {
                    return;
                }
                _leftAmount.Clear();

                for (int i = 0; i < nextUpgradeCount.Items.Count; i++) {
                    var item = nextUpgradeCount.Items[i];
                    int ownedAmount = _currentAmount.GetCount(item.id, out _);
                    int count = nextUpgradeCount.GetCount(item.id, out _);
                    count -= ownedAmount;
                    _leftAmount.Add(item.id, count);
                }
            }
        }

    }
}