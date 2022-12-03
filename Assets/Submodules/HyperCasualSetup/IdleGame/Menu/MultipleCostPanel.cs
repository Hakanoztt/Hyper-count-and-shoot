using Mobge.Core;
using Mobge.HyperCasualSetup;
using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Mobge.IdleGame.UI {
    [Serializable]
    public class MultipleCostPanel : UICollection<UIItem> {
        public FormatModule formatModule;
        public string currencyFormat;

        public void UpdateCost(ItemCluster cost, int defaultItem = 0) {
            UpdateCost(cost.set, cost.Items, defaultItem);
        }
        public void UpdateCost(ItemSet currencies, ListIndexer<ItemCluster.ItemContent> cost, int defaultItem = 0) {
            int count = cost.Count;
            if (count == 0) {
                if (defaultItem >= 0) {
                    this.Count = 1;
                    var item = currencies.items[defaultItem];
                    var ui = this[0];
                    ui.textsTMPro[0].text = "0";
                    ui.images[0].sprite = item.sprite;
                }
                else {
                    this.Count = 0;
                }
            }
            else {
                this.Count = count;
                for (int i = 0; i < cost.Count; i++) {
                    var ui = this[i];
                    var itemContent = cost[i];
                    ui.textsTMPro[0].text = FormatCurrency(itemContent.count);
                    ui.images[0].sprite = currencies.items[itemContent.id].sprite;
                }
            }
        }

        private string FormatCurrency(int cost) {
            if (formatModule.enable) {
                return formatModule.Format(cost);
            }

            if (string.IsNullOrEmpty(currencyFormat)) {
                return cost.ToString();
            }
            return string.Format(currencyFormat, cost);
        }

        public void UpdateUpgradeCost(IUnit unit, out ItemCluster cost, int defaultItem = 0) {
            if (!unit.CanBeUpgraded()) {
                CurrentParent.gameObject.SetActive(false);
                cost = null;
            }
            else {
                CurrentParent.gameObject.SetActive(true);
                cost = unit.Spawner.GetUpgradeCost();

                UpdateCost(cost, defaultItem);
            }
        }

        [Serializable]
        public class FormatModule {
            public bool enable;
            public string[] TousandShorts = new string[] { "K", "M", "B", "T", "Q" };
            public int decimalCount;


            public string Format(float cost) {
                var numOfdigit = Math.Floor(Math.Log10(cost) + 1);

                string format = "0.";
                for (int j = 0; j < decimalCount; j++) {
                    format += "#";
                }

                for (int i = TousandShorts.Length - 1; i >= 0; i--) {
                    if (numOfdigit> (i + 1) * 3) {
                        return (cost / Mathf.Pow(10, (i + 1) * 3)).ToString(format) + TousandShorts[i];
                    }
                }
                return cost.ToString("#,0");
            }

        }
    }
}