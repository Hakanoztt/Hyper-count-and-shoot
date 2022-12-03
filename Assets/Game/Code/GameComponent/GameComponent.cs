using System;
using Mobge.Core;
using Mobge.HyperCasualSetup;
using Mobge.Telemetry;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.CountAndShoot {
    public class GameComponent : ComponentDefinition<GameComponent.Data> {
        const string c_key = "gc_key";
        public static Data GetForPlayer(LevelPlayer player) {
            if (player.TryGetExtra(c_key, out Data data)) {
                return data;
            }
            return null;
        }

        [Serializable]
        public class Data : BaseComponent {
            public override LogicConnections Connections { get => connections; set => connections = value; }
            [SerializeField] [HideInInspector] private LogicConnections connections;
            private Dictionary<int, BaseComponent> _components;

            private LevelPlayer _player;
            private TelemetryModule _telemetryModule = new TelemetryModule();
            private Player _character;
            //    private PlayerTarget _playerTarget;
            public Player Player => _character;
            //      public PlayerTarget PlayerTarget => _playerTarget;
            public override void Start(in InitArgs initData) {
                _player = initData.player;
                _player.SetExtra(c_key, this);
                _telemetryModule.TrackLevel((BaseLevelPlayer)_player);
                _components = initData.components;
                _player.RoutineManager.DoAction(LateStart);
            }
            private void LateStart(bool complete, object data) {
                var go = (Transform)Connections.InvokeSimple(this, 0, null, _components);
                //    var pt = (Transform)Connections.InvokeSimple(this, 1, null, _components);
                _character = go.GetComponent<Player>();
                //      _playerTarget = pt.GetComponent<PlayerTarget>();
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
                slots.Add(new LogicSlot("character", 0, typeof(Transform)));
                //   slots.Add(new LogicSlot("Enemy", 1, typeof(Transform)));
            }
#endif
        }
    }
}
