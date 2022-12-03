using Mobge.HyperCasualSetup;
using Mobge.IdleGame;
using Mobge.IdleGame.UI;
using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Mobge.IdleGame {
    public class OnClickUpgrader : AClickable {

        [InterfaceConstraint(typeof(IUnit))] public Component unitComponent;
        private IUnit _unit;

        public MultipleCostPanel costCollection;
        public Transform maxedVisual;
        public Transform upgradeVisual;

        public Color moneyColor = Color.white;
        public Color moneyNotEnoughColor = Color.red;

        public AReusableItem costStateChangedEffect;

        private WalletComponent _wallet;
        private ItemCluster _currentCost;

        private bool _hasEnoughMoney;

        protected new void Start() {
            base.Start();
            _unit = (IUnit)unitComponent;
            if(UIMoneyExtension.TryGetMainWallet(_unit.Spawner.Player, out _wallet)) {
                _wallet.onChange += OnMoneyChange;
            }
            UpdateVisuals(true);
        }

        private void OnMoneyChange(WalletComponent obj) {
            UpdateCostColors();
        }

        private void UpdateVisuals(bool initial = false) {
            costCollection.UpdateUpgradeCost(_unit, out _currentCost);
            RefreshMaxedUI(!_unit.CanBeUpgraded());
            UpdateCostColors(initial);

        }

        private void UpdateCostColors(bool initial = false) {
            if (_currentCost != null) {
                bool hasEnoughTotal = true;
                for(int i =0; i < _currentCost.Items.Count; i++) {
                    var item = _currentCost.Items[i];
                    int count = _wallet.GetAmount(item.id);
                    bool hasEnough = count >= item.count;
                    Color c = hasEnough ? moneyColor : moneyNotEnoughColor;
                    hasEnoughTotal = hasEnoughTotal && hasEnough;
                    costCollection[i].textsTMPro[0].color = c;
                }
                if(!_hasEnoughMoney && hasEnoughTotal) {
                    if (!initial) {
                        if (costStateChangedEffect != null) {
                            costStateChangedEffect.Play();
                        }
                    }
                }
                _hasEnoughMoney = hasEnoughTotal;
            }

        }

        void RefreshMaxedUI(bool maxed) {
            if (maxedVisual != null) {
                maxedVisual.transform.gameObject.SetActive(maxed);
            }
            if (upgradeVisual != null) {
                upgradeVisual.transform.gameObject.SetActive(!maxed);
            }
            Enabled = !maxed;
        }

        public override void HandleClick() {
            base.HandleClick();
            _currentCost = _unit.Spawner.GetUpgradeCost();
            if (_currentCost != null) {
                if (_wallet.TryRemove(_currentCost, out _)) {
                    _unit.Spawner.Upgrade();
                    UpdateVisuals();
                }
            }

        }


#if UNITY_EDITOR
        protected void OnDrawGizmosSelected() {
            if (unitComponent == null) {
                if (!Application.isPlaying) {
                    var obj = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(this);
                    if (obj != null && obj.TryGetComponent<IUnit>(out var unit)) {
                        unitComponent = (Component)unit;
                    }
                }
            }
        }
#endif
    }
}