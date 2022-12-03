using Mobge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup
{
    [CreateAssetMenu(menuName = "Hyper Casual/Level")]
    public class BaseLevel : Level
    {
        public override Type PlayerType => typeof(BaseLevelPlayer);
        public int tag;
    }
}