using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class CollectableDefinition : ComponentDefinition<CollectableDefinition.Data>
    {

        [Serializable]
        public class Data : BaseComponent {
            public Mobge.Platformer.Character.CollectableData collectable;
            public override void Start(in InitArgs args)
            {
                
            }
        }
    }

}