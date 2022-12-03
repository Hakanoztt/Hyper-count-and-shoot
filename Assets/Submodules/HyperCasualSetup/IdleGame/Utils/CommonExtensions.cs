using Mobge.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame {
    public static class CommonExtensions {
        /// <summary>
        /// Returns <see cref="LevelPlayer"> if available for the specified <see cref="Component">. This function is a little bit costly so result of this function should be cached.
        /// </summary>
        /// <param name="component">Specified <see cref="Component"></see>/></param>
        /// <returns></returns>
        public static LevelPlayer GetLevelPlayer(this Component component) {
            return component.transform.root.GetComponent<LevelPlayer>();
        }
    }
}