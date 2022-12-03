using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mobge.UI;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup
{
    public class LevelElement : ListElement
    {
        public ScoreCollection scoreImages;
        [Serializable]
        public class ScoreCollection : UICollection<Image>
        {

        } 
    }
}