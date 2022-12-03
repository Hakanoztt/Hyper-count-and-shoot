using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup
{
    public class WorldSelectPanel : MonoBehaviour
    {
        private RectTransform _tr;
        public Text title;

        public int WorldIndex { get; private set; } = -1;

        public Elements elements;
        public Action<WorldSelectPanel, int> onLevelButtonClicked;
        protected void Awake() {
            _tr = (RectTransform)transform;
        }
        internal void SetLevels(AGameContext gc, int worldIndex, Vector3 localPosition) {
            _tr.anchoredPosition = localPosition;
            var pr = gc.GameProgressValue;
            var data = gc.LevelData;
            if (WorldIndex != worldIndex) {
                title.text = "World " + (worldIndex+1);
                WorldIndex = worldIndex;
                elements.Count = data.GetLevelCount(worldIndex);

            }
            for (int i = 0; i < elements.Count; i++) {
                var ei = elements[i];
                ei.title.text = (i + 1).ToString();
                ei.tag = i;
                ei.MainButtonClicked = ElementClicked;
                var id = LevelData.ID.FromWorldLevel(worldIndex, i);
                var unlocked = pr.IsUnlocked(data, id, gc);
                ei.SetState(unlocked ? ei.mainState : ei.disabledState);
                int stars = 0;
                if (pr.TryGetLevelResult(id, out LevelResult lr)) {
                    stars = lr.levelChallenge;
                }
                ei.scoreImages.Count = stars;
            }
        }

        private void ElementClicked(ListElement obj) {
            onLevelButtonClicked(this, obj.tag);
        }


        [Serializable]
        public class Elements : Mobge.UI.UICollection<LevelElement>
        {

        }
    }
}