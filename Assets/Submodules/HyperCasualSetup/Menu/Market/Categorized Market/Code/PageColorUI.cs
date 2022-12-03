using Mobge.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.UI.CategorizedMarket {
    public class PageColorUI : UIPageTabs{
        public UnityEngine.UI.Image pageBackground;
        protected new void Update() {
            base.Update();
            float cp = pageCollection.CurrentProgress;
            int floor = Mathf.FloorToInt(cp);
            int ceiling = Mathf.CeilToInt(cp);
            floor = Mathf.Clamp(floor, 0, tabs.Count - 1);
            ceiling = Mathf.Clamp(ceiling, 0, tabs.Count - 1);

            Color c;
            if(floor == ceiling) {
                c = this.tabs[floor].images[0].color;
            }
            else {
                var c1 = tabs[floor].images[0].color;
                var c2 = tabs[ceiling].images[0].color;
                c = Color.LerpUnclamped(c1, c2, cp - floor);
            }
            pageBackground.color = c;
        }
    }
}