using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Serialization {
    public interface ISaveable
    {
        object State { get; set; }
    }
}