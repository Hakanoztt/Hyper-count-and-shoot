using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public interface IChild {
        ElementReference Parent { get; set; }
    }
}