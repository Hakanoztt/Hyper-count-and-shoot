using System;
using UnityEngine;

namespace Mobge.HyperCasualSetup {

    [Serializable]
    public class PrizeData  {

        public Type giftType;
        public ItemSet set;
        public int itemId;
        public float value;
        public float valueRankMultiplayer;
        public string label;
        public bool labelIsFormat;

        public ItemSet.Item Item { get => set.items[itemId]; }
        public Sprite Icon {
            get {
                if (giftType == Type.Score || set == null) {
                    return null;
                }
                set.items.TryGetElement(itemId, out ItemSet.Item item);
                if (item == null) {
                    return null;
                }
                return item.sprite;
            }
        }

        public string GetDisplayString(int rank) {
            var val = CalculateValue(rank);
            if (labelIsFormat) {
                return string.Format(label, val);
            }
            else {
                return label + val;
            }
        }

        public float CalculateValue(int rank) {
            return value + rank * valueRankMultiplayer;
        }

        public void ApplyTo(TextMesh text, int rank) {
            if (text != null) {
                text.text = GetDisplayString(rank);
            }
        }

        public void ApplyToData(AGameContext context, AGameProgress progress, float currentScore, int rank, bool equipIfPossible = true, string moneyEvent  =null) {
            float scoreIncrease;
            switch (giftType) {
                case Type.Score:
                    scoreIncrease = CalculateValue(rank);
                    progress.ChangeTotalScore(context, scoreIncrease, moneyEvent);
                    break;
                case Type.ScoreMultiplayer:
                    scoreIncrease = (CalculateValue(rank) - 1) * currentScore;
                    progress.ChangeTotalScore(context, scoreIncrease, moneyEvent);
                    break;
                case Type.ItemWithQuantity:
                    progress.GetQuantityItemSet(set).AddItem(itemId, (int)CalculateValue(rank));
                    break;
                default:
                    var data = progress.GetItemSet(set);
                    data.AddItem(itemId);
                    if (equipIfPossible) {
                        data.EquippedItem = itemId;
                    }
                    break;
            }

        }


        public enum Type {
            Score = 0,
            ItemWithQuantity = 1,
            SingleItem = 2,
            ScoreMultiplayer = 3,
        }
    }
}