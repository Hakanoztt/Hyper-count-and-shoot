using Mobge.HyperCasualSetup;
using Mobge.IdleGame.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame {
    public interface IUnit {
        UnitSpawnerComponent.Data Spawner { get; set; }
        void SetLevel(int level, bool initial);
        int RankCount { get; }
        ItemCluster GetDefaultCost(int index);
        IUnitRank GetRank(int index);
        UpgradeStyle UpgradeStyleWhenMaxed { get; }
        public enum UpgradeStyle {
            Stop = 0,
            Loop = 1,
            ApplyLastLevel = 2,
        }
    }
    public interface IUnitRank {
        void Start(in Core.BaseComponent.InitArgs initArgs);
        void OnRankEnable(IUnit unit, int rank);
        void OnRankDisable();
    }
    public static class UnitUtils {
        public static bool CanBeUpgraded(this IUnit unit) {
            return unit.Spawner.CanBeUpgraded;
        }
    }
}

