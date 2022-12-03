using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.UI.ChallengeSystem {
    public class ChallengeListItem : UIItem {

        public RewardCollection rewards;


        [Serializable]
        public class RewardCollection : UICollection<UIItem> { }
    }
}