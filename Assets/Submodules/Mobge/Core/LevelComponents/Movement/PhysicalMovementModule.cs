using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class PhysicalMovementModule : BaseMovementModule<MovementModule.Data>
    {
        [Serializable]
        public class Data : BaseData
        {
            public override void Start(in InitArgs initData)
            {
                
            }
        }
    }
}