using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mobge.UI;
using UnityEngine.UI;
using TMPro;

namespace Mobge.HyperCasualSetup
{
    public class GenericMenu : BaseMenu
    {
        public Button[] buttons;
        public Text[] texts;
        public Image[] images;
        public TextMeshProUGUI[] tmpTexts;
        public Transform[] transforms;
    }
}