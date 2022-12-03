using Mobge.IdleGame;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.UI {

    public interface IPaymentModule {

        public int GetCurrent(AGameContext context);

        public void Change(AGameContext context, string source, int amount);
    }

[Serializable]
    public struct MenuPaymentModule {
        public PaymentType type;
        public ItemSet.ItemPath currency;
        public IPaymentModule module;

        public int GetCurrent(AGameContext context) {

            switch (type) {
                default:
                case PaymentType.TotalScore:
                    return (int)context.GameProgressValue.TotalScore;
                case PaymentType.Currency:
                    return context.GameProgressValue.GetQuantityItemSet(currency.set)[currency.id];
                case PaymentType.Custom:
                    return module.GetCurrent(context);
            }

        }

        public void Change(BaseLevelPlayer player, WalletComponent wallet, string source, int amount) {
            switch (type) {
                default:
                case PaymentType.TotalScore:
                    var context = player.Context;
                    var val = context.GameProgressValue;
                    val.ChangeTotalScore(context, amount, source);
                    context.GameProgress.SaveValue(val);
                    break;
                case PaymentType.Currency:
                    wallet.Change(new ItemCluster.ItemContent(currency.id, amount), source);
                    break;
                case PaymentType.Custom:
                    module.Change(player.Context, source, amount);
                    break;
            }
        }

    }
    public enum PaymentType {
        TotalScore = 0,
        Currency = 1,
        Custom = 10,
    }

}