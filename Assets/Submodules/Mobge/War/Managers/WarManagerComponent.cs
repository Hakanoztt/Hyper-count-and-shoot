using System;
using System.Collections.Generic;
using Mobge.Core;
using UnityEngine;

namespace Mobge.War {
    public class WarManagerComponent : ComponentDefinition<WarManagerComponent.Data> {
        public const string c_key = "mb_wr_manc";
        public static Data Get(LevelPlayer player) {
            player.TryGetExtra(c_key, out Data man);
            return man;
        }

        [Serializable]
        public class Data : BaseComponent {
            public LevelPlayer Player { get; private set; }

            public TeamMaterials teamMaterials;
            public override void Load(in LevelPlayer.LoadArgs loadArgs) {
                base.Load(loadArgs);
                Player = loadArgs.levelPlayer;
                Player.SetExtra(c_key, this);
            }

            public override void Start(in InitArgs initData) {
            }

            public bool IsEnemy(int team1, int team2) {
                var setup = Player.level.GameSetup;
                return setup.Teams.IsEnemy(team1, team2);
            }
        }

        [Serializable]
        public struct TeamMaterials {
            [SerializeField] private Material[] _materials;
            public Material GetTeamMaterial(int team) {
                return _materials[team % _materials.Length];
            }
        }
    }
}
            