using Mobge.Core;
using System;
using System.Collections.Generic;

namespace Mobge.HyperCasualSetup.Components
{
    public class LevelScoreComponent : ComponentDefinition<LevelScoreComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent
        {
            BaseLevelPlayer _player;
            public float potentialScoreCount = 1;
            public override void Start(in InitArgs initData) {
                _player = (BaseLevelPlayer)initData.player;
                _player.TotalScore += potentialScoreCount;
            }

            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    default:
                    case 0:
                        _player.Score++;
                        break;
                    case 1:
                        _player.Score += (float)input;
                        break;
                    case 2:
                        _player.Score = (float)input;
                        break;
                    case 3:
                        return _player.Score;
                }
                return null;
            }
            #if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("increase", 0));
                slots.Add(new LogicSlot("increase by", 1, typeof(float)));
                slots.Add(new LogicSlot("set to", 2, typeof(float)));
                slots.Add(new LogicSlot("get value", 3, null, typeof(float)));
            }
            #endif
        }
    }
}