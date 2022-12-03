using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mobge.UI;
using System;

namespace Mobge.HyperCasualSetup.UI
{
    public class LevelProgressBar : MonoBehaviour, BaseMenu.IExtension
    {
        public TextReference leftProgress;
        public TextReference rightProgress;
        public ProgressBar bar;
        void BaseMenu.IExtension.Prepare(BaseMenu menu) {
            var manager = menu.MenuManager as Mobge.HyperCasualSetup.UI.MenuManager;
            var ctx = manager.Context;
            var currentId = manager.LastOpenedId;
            currentId[2] = -1;
            var nextId = currentId;
            int count = 0;
            int completedCount = 0;
            var gameProgress = (AGameProgress)ctx.GameProgress.ValueUnsafe;
            do {
                if (!ctx.LevelData.TryIncreaseLevel(ref nextId)) {
                    break;
                }
                if(nextId[0] != currentId[0] || nextId[1] != currentId[1]) {
                    break;
                }
                if (gameProgress.TryGetLevelResult(nextId, out LevelResult res)) {
                    if (res.completed) {
                        completedCount++;
                    }
                }
                count++;


            } while (true);

            bar.Count = count;
            int i = 0;
            for (; i < completedCount; i++) {
                bar[i].SetState(bar[i].secondaryState);
            }
            for(; i <count; i++) {
                bar[i].SetState(bar[i].mainState);
            }
            leftProgress.Text = (1 + currentId[1]).ToString();
            rightProgress.Text = (2 + currentId[1]).ToString();
        }
        [Serializable]
        public class ProgressBar : UICollection<ListElement>
        {

        }
    }
}