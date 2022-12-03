using System;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.Core.Components;
using Mobge.HyperCasualSetup;
using Mobge.HyperCasualSetup.RoadGenerator;
using Mobge.HyperCasualSetup.UI;
using Mobge.Telemetry;
using Mobge.UI;
using UnityEngine;

namespace Mobge.BasicRunner {

    public class GameComponentBasicRunner : ComponentDefinition<GameComponentBasicRunner.Data> {

        public const string c_key = "mg_gc";
        public static Data GetForPlayer(LevelPlayer player) {
            player.TryGetExtra<Data>(c_key, out var data);
            return data;
        }

        [Serializable]
        public class Data : BaseComponent {
            public override LogicConnections Connections { get => connections; set => connections = value; }
            [SerializeField, HideInInspector] private LogicConnections connections;
            private Dictionary<int, BaseComponent> _components;

            private TelemetryModule _telemetryModule = new TelemetryModule();


            public SwipeNavigationControl controlPrefab;
            public SwipeNavigationControl Controls { get; set; }


            private RunnerController _character;
            private BaseLevelPlayer _player;

            public RunnerController Character=>_character;

            public bool runAsPressed = false;

            private bool _runningMode;

            public override void Start(in InitArgs initData) {
                _player = (BaseLevelPlayer)initData.player;
                _components = initData.components;
                _player.SetExtra(c_key, this);
                _telemetryModule.TrackLevel(_player);

                _player.FixedRoutineManager.DoAction(LateStart);

                //string value = "example argument value";
                //Connections.InvokeSimple(this, 0, value, _components);
                LevelStartMenu.OpenMenu(_player, 0, OnLevelStarted);
            }

            private void OnLevelStarted() {
                 
            }

            private void LateStart(bool complete, object data) {
                if (!complete) {
                    return;
                }
                if (connections.InvokeSimple(this, 0, null, _components) is Transform obj) {
                    _character = obj.GetComponent<RunnerController>();
                    Controls = Instantiate(controlPrefab, _player.transform);
                }

                _player.RoutineManager.DoRoutine(Update);

                LevelStartMenu.OpenMenu(_player, 0, () => {
                    _runningMode = true;
                });
            }

            private void Update(float progress, object data) {
                if (_runningMode) {
                    var moveInput = Controls.Input;
                    if (runAsPressed) {
                        moveInput.y = Controls.Pressed ? 1f : 0f;
                    }
                    else {
                        moveInput.y = 1f;
#if UNITY_EDITOR
                        if (Input.GetKey(KeyCode.RightControl)) {
                            moveInput.y = -moveInput.y;
                        }
#endif
                    }
                    _character.input.MoveInput = moveInput;
                    _character.input.UpdateAction(0, _character);
                }

            }
            //public override object HandleInput(ILogicComponent sender, int index, object input) {
            //    switch (index) {
            //        case 0:
            //            return "example output";
            //    }
            //    return null;
            //}
#if UNITY_EDITOR
            //public override void EditorInputs(List<LogicSlot> slots) {
            //    slots.Add(new LogicSlot("example input", 0));
            //}
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("main character", 0));
            }
#endif
        }
    }
}

