using Mobge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.UI {
    public class IncrementalListeners {
        private const string incremental_actions_key = "incremental_actions";
        public static void Register(LevelPlayer levelPlayer, Action<int> OnIncrementalUpdate) {
            levelPlayer.TryGetExtra(incremental_actions_key, out Action<int> actions);
            actions += OnIncrementalUpdate;
            levelPlayer.SetExtra(incremental_actions_key, actions);
        }

        public static void FireIncrementalActions(LevelPlayer levelPlayer, int incrementalIndex) {
            levelPlayer.TryGetExtra(incremental_actions_key, out Action<int> actions);
            if (actions != null) {
                actions(incrementalIndex);
            }
        }
    }
}