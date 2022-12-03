using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.UI.ChallengeSystem {
    public class ChallengeScoreExtension : MonoBehaviour, BaseMenu.IExtension{
        public ChallengeData data;
        public Collection uiCollection;
        public string scoreFormat = "Your Score: {0}";
        public TextReference scoreTexts;

        void BaseMenu.IExtension.Prepare(BaseMenu menu) {
            var menuManager = (Mobge.HyperCasualSetup.UI.MenuManager)menu.MenuManager;
            if (data.TryGetLevel(menuManager.LastOpenedId, out ChallengeData.Level entry)) {
                gameObject.SetActive(true);

                var progress = menuManager.Context.GameProgressValue;
                int score = 0;
                if(progress.TryGetLevelResult(menuManager.LastOpenedId, out var levelResult)) {
                    score = levelResult.levelChallenge;
                }

                uiCollection.Count = entry.scores.Length;
                for(int i = 0; i < entry.scores.Length; i++) {
                    var ui = uiCollection[i];
                    ui.SetState(score >= entry.scores[i].value ? 1 : 0);
                }
                scoreTexts.Text = string.Format(scoreFormat, score);
            }
            else {
                gameObject.SetActive(false);
            }
        }
        [Serializable]
        public class Collection : UICollection<UIItem> {

        }
    }
}