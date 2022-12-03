
using System;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.HyperCasualSetup;
using UnityEngine;

namespace Mobge.HyperCasualSetup.UI.ChallengeSystem {
    public class ChallengeGameComponent : ComponentDefinition<ChallengeGameComponent.Data> {
        public const string c_game = "cgc";
        public static Data GetFromPlayer(LevelPlayer player) {
            player.TryGetExtra(c_game, out Data game);
            return game;
        }
        [Serializable]
        public class Data : BaseComponent {
            public float timeLimit;
            public bool startTimerOnStart = false;
            public bool TimerEnabled => timeLimit > 0;

            private float _leftTime;
            private BaseLevelPlayer _player;
            private ActionManager.Action _timerAction;

            public Action<Data, float> OnTimerUpdated;
            public Action<Data> OnTimerEnded;
            public Action<Data> onAllChallengesCompleted;
            public float LeftTime => _leftTime;

            public override LogicConnections Connections { get => _connections; set => _connections = value; }
            [SerializeField, HideInInspector] private LogicConnections _connections;
            private Dictionary<int, BaseComponent> _components;


            public override void Start(in InitArgs initData) {
                initData.player.SetExtra(c_game, this);
                _leftTime = timeLimit;
                _player = (BaseLevelPlayer)initData.player;
                _player.OnLevelChallengeChange += PlayerChallengeUpdated;

                if (startTimerOnStart && TimerEnabled) {
                    StartTimer();
                }
                _components = initData.components;

            }

            private void PlayerChallengeUpdated(BaseLevelPlayer arg1, int arg2) {
                if(arg1.LevelChallenge == arg1.TotalChallenge) {
                    StopTimer();
                    if (onAllChallengesCompleted != null) {
                        onAllChallengesCompleted(this);
                    }
                    if (_connections != null) {
                        _connections.InvokeSimple(this, 1, null, _components);
                    }
                }
            }

            public void LevelStarted() {
                if (TimerEnabled && _timerAction.IsFinished()) {
                    StartTimer();
                }
            }

            private void StartTimer() {
                _player.FixedActionManager.DoTimedAction(timeLimit, UpdateTimer, TimerEnd);
            }

            private void TimerEnd(object data, bool completed) {
                if (completed) {
                    if (OnTimerEnded != null) {
                        OnTimerEnded(this);
                    }

                    if (_connections != null) {
                        _connections.InvokeSimple(this, 0, null, _components);
                    }
                }
            }

            private void UpdateTimer(in ActionManager.UpdateParams @params) {
                if (OnTimerUpdated != null) {
                    OnTimerUpdated(this, timeLimit * @params.progress);
                }
            }
            private void StopTimer() {
                _timerAction.Stop();
            }
            //public override object HandleInput(ILogicComponent sender, int index, object input) {
            //    switch (index) {
            //        case 0:
            //            //on example input fired
            //            return "example output";
            //    }
            //    return null;
            //}
#if UNITY_EDITOR
            //public override void EditorInputs(List<LogicSlot> slots) {
            //    slots.Add(new LogicSlot("example input", 0));
            //}
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("On Timer End", 0));
                slots.Add(new LogicSlot("On All Challenges Collected", 1));
            }
#endif
        }
    }
}
            