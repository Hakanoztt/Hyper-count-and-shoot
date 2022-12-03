using Mobge.HyperCasualSetup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame {

    public interface IContextOwner {
        AGameContext Context { get; }
    }
}