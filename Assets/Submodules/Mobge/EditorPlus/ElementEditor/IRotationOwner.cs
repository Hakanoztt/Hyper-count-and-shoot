using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Mobge {
    public interface IRotationOwner
    {
        Quaternion Rotation { get; set; }
    }
}