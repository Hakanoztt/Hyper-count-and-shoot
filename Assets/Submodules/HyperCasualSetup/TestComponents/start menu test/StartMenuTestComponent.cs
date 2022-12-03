using System;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.HyperCasualSetup;
using Mobge.HyperCasualSetup.UI;
using UnityEngine;

namespace Mobge.Test {
    public class StartMenuTestComponent : ComponentDefinition<StartMenuTestComponent.Data> {
        [Serializable]
        public class Data : BaseComponent {
            public override LogicConnections Connections { get => connections; set => connections = value; }
            [SerializeField, HideInInspector] private LogicConnections connections;
            private Dictionary<int, BaseComponent> _components;

            private BaseLevelPlayer _player;
            public float subscribeDelay;
            public override void Start(in InitArgs initData) {
                _player = (BaseLevelPlayer)initData.player;
                _components = initData.components;
                
                if(subscribeDelay == 0) {
                    Subscribe();
                }
                else {
                    _player.FixedRoutineManager.DoAction(Subscribe, subscribeDelay);
                }

                //string value = "example argument value";
                //Connections.InvokeSimple(this, 0, value, _components);
            }

            private void Subscribe(bool complete, object data) {
                if (complete) {
                    Subscribe();
                }
            }

            public void Subscribe() {
                LevelStartMenu.OpenMenu(_player, 0, LeveStarted);
            }

            private void LeveStarted() {
                if (connections != null) {
                    connections.InvokeSimple(this, 0, null, _components);
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
            public override void EditorInputs(List<LogicSlot> slots) {
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("on start level", 0));
            }
#endif
        }
    }
}
            