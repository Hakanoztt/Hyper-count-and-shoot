using System;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.Core.Components;
using Mobge.HyperCasualSetup;
using UnityEngine;

namespace Mobge.IdleGame {

    public class UnitSpawnerComponent : ComponentDefinition<UnitSpawnerComponent.Data> {

        private const int c_rankInputOffset = 256;
        private const int c_rankOutputOffset = 256;

        [Serializable]
        public class Data : PrefabSpawnerComponent.Data {
            public int defaultRank = -1;

            protected BaseLevelPlayer _player;

            public RankData[] ranks;

            public override LogicConnections Connections { get => connections; set => connections = value; }
            [SerializeField, HideInInspector] private LogicConnections connections;
            private Dictionary<int, BaseComponent> _components;

            private int _rank = -1;

            private IUnitRank _lastRank;

            public IUnit UnitInstance { get; private set; }

            public BaseLevelPlayer Player => _player;
            public string UniqueId { get; private set; }
            public int RankCount => UnitInstance.RankCount;


            protected int Rank {
                get => _rank;
            }

            public bool IsVisible {
                get => _instance.gameObject.activeSelf;
                private set {
                    if (_instance.gameObject.activeSelf != value) {
                        _instance.gameObject.SetActive(value);
                    }
                }
            }
            public bool CanBeUpgraded {
                get {
                    switch (UnitInstance.UpgradeStyleWhenMaxed) {
                        case IUnit.UpgradeStyle.Stop:
                            return Rank + 1 < RankCount;
                        case IUnit.UpgradeStyle.Loop:
                        case IUnit.UpgradeStyle.ApplyLastLevel:
                        default:
                            return true;
                    }
                }
            }
            public override void Start(in InitArgs initData) {
                base.Start(initData);

                _player = (BaseLevelPlayer)initData.player;

                var levelId = _player.Context.MenuManager.LastOpenedId;
                int compId = initData.id;
                UniqueId = "unit_" + levelId.Value + "-" + compId;


                IsVisible = false;
                _rank = -1;

                _components = initData.components;

                if (_instance.TryGetComponent<IUnit>(out var ins)) {
                    UnitInstance = ins;
                    UnitInstance.Spawner = this;
                }

                for(int i = 0; i < UnitInstance.RankCount; i++) {
                    var rank = UnitInstance.GetRank(i);
                    if (rank != null) {
                        rank.Start(initData);
                    }
                }

                _player.FixedRoutineManager.DoAction(DelayedStart);
            }
            private int CalculateNextRank(int rank) {
                int value = rank + 1;
                switch (UnitInstance.UpgradeStyleWhenMaxed) {
                    default:
                    case IUnit.UpgradeStyle.Stop:
                        break;
                    case IUnit.UpgradeStyle.Loop:
                        value = value % RankCount;
                        break;
                    case IUnit.UpgradeStyle.ApplyLastLevel:
                        value = Mathf.Min(value, RankCount - 1);
                        break;
                }
                return value;
            }
            public ItemCluster GetUpgradeCost() {
                int nextRank = CalculateNextRank(_rank);
                if (this.ranks.Length > nextRank) {

                    var cOverride = this.ranks[nextRank].costOverride;
                    if (cOverride != null && cOverride.set != null) {
                        return cOverride;
                    }
                }

                return UnitInstance.GetDefaultCost(nextRank);
            }
            public void Upgrade() {
                int value = CalculateNextRank(_rank);
                if (_rank != value) {

                    var val = _player.Context.GameProgressValue;
                    val.SetInt(UniqueId, value);
                    _player.Context.GameProgress.SaveValue(val);
                }
                ApplyRank(value, true);
            }

            public override void End() {
                base.End();
                if (_lastRank != null) {
                    _lastRank.OnRankDisable();
                }
            }


            private void DelayedStart(bool complete, object data) {
                bool visible = true;
                if (connections.InvokeSimple(this, 0, null, _components) is RankState c) {
                    if (!c.unlocked) {
                        visible = false;
                        c.callback += BecomeVisible;
                    }
                }
                if (visible) {
                    BecomeVisible(false);
                }
            }

            private void BecomeVisible(bool animation) {
                IsVisible = true;
                int targetRank = _player.Context.GameProgressValue.GetInt(UniqueId, defaultRank);
                if (targetRank == -1) {
                    targetRank = 0;
                }
                ApplyRank(targetRank, animation);
            }


            void ApplyRank(int value, bool animation) {
                do {
                    _rank = CalculateNextRank(_rank);
                    if (ranks.Length > _rank) {
                        ref var rank = ref ranks[_rank];
                        if (rank.State != null) {
                            rank.EnsureInit();
                            if (rank.TryUnlock(animation)) {
                                if (rank.hasOutput) {
                                    connections.InvokeSimple(this, c_rankOutputOffset + _rank, null, _components);
                                }
                            }
                        }
                    }
                }
                while (_rank != value);

                UnitInstance.SetLevel(value, !animation);
                if (_lastRank != null) {
                    _lastRank.OnRankDisable();
                }
                _lastRank = UnitInstance.GetRank(value);
                if (_lastRank != null) {
                    _lastRank.OnRankEnable(UnitInstance, value);
                }
            }

            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case 0:
                        return "example output";
                    default:
                        if (index >= c_rankInputOffset) {
                            ref var rank = ref ranks[index - c_rankInputOffset];
                            rank.EnsureInit();
                            rank.State.unlocked = Rank >= index - c_rankInputOffset;
                            return rank.State;
                        }
                        break;
                }
                return null;
            }

            [Serializable]
            public struct RankData {

                public bool hasInput;
                public bool hasOutput;

                public ItemCluster costOverride;

                private RankState _state;
                public RankState State => _state;

                public void EnsureInit() {
                    if (_state == null) {
                        _state = new RankState();
                    }
                }

                public bool TryUnlock(bool animation) {
                    if (!_state.unlocked) {
                        _state.unlocked = true;
                        if (_state.callback != null) {
                            _state.callback(animation);
                        }
                        _state.callback = null;
                        return true;
                    }
                    return false;
                }
            }

            public class RankState {
                public Action<bool> callback;
                public bool unlocked;
            }

#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                for (int i = 0; i < ranks.Length; i++) {
                    if (ranks[i].hasInput) {
                        slots.Add(new LogicSlot("Is level >= " + i, c_rankInputOffset + i, null, typeof(RankState)));
                    }
                }
            }

            public override void EditorOutputs(List<LogicSlot> slots) {
                for (int i = 0; i < ranks.Length; i++) {
                    if (ranks[i].hasOutput) {
                        slots.Add(new LogicSlot("Is level >= " + i, c_rankOutputOffset + i));
                    }
                }
                slots.Add(new LogicSlot("fetch dependence", 0, null, typeof(RankState)));
            }
#endif
        }
    }
}