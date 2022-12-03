using Mobge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.Components
{
    public class LevelAchievementComponent : ComponentDefinition<LevelAchievementComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent
        {
            BaseLevelPlayer _player;
            public int maxPotentialValue = 1;
            public override void Start(in InitArgs initData) {
                _player = (BaseLevelPlayer)initData.player;
                _player.TotalChallenge += maxPotentialValue;
            }

            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    default:
                    case 0:
                        _player.LevelChallenge++;
                        break;
                    case 1:
                        _player.LevelChallenge += (int)(float)input;
                        break;
                    case 2:
                        _player.LevelChallenge = (int)(float)input;
                        break;
                }
                return null;
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("increase", 0));
                slots.Add(new LogicSlot("increase by", 1, typeof(float)));
                slots.Add(new LogicSlot("set to", 2, typeof(float)));
            }
#endif
        }
    }
}