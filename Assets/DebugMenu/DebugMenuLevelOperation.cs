using Mobge.HyperCasualSetup;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.DebugMenu {

    public class DebugMenuLevelOperation : MonoBehaviour, IDebugMenuExtension {
        private DebugMenu _menu;
        public TMPro.TMP_InputField increaseLevelInput;
        public Button levelUpButton;
        public Button finishLevelButton;
        public Button previousLevelButton;
        public Button goToLevelButton;

        public Button increaseButton;
        public Button increase5xButton;
        public Button decreaseButton;
        public Button decrease5xButton;
        public void Init(DebugMenu debugMenu) {
            _menu = debugMenu;

            levelUpButton.onClick.RemoveAllListeners();
            finishLevelButton.onClick.RemoveAllListeners();
            previousLevelButton.onClick.RemoveAllListeners();
            goToLevelButton.onClick.RemoveAllListeners();
            increaseButton.onClick.RemoveAllListeners();
            decreaseButton.onClick.RemoveAllListeners();
            increase5xButton.onClick.RemoveAllListeners();
            decrease5xButton.onClick.RemoveAllListeners();

            levelUpButton.onClick.AddListener(LevelUp);
            finishLevelButton.onClick.AddListener(EndCurrentLevel);
            previousLevelButton.onClick.AddListener(GoToPreviousLevel);
            goToLevelButton.onClick.AddListener(GoToLevel);
            decreaseButton.onClick.AddListener(DecreaseLevelInput);
            increaseButton.onClick.AddListener(IncreaseLevelInput);
            decrease5xButton.onClick.AddListener(DecreaseLevel5xInput);
            increase5xButton.onClick.AddListener(IncreaseLevel5xInput);

        }

        private void DecreaseLevel5xInput() {
            for (int i = 0; i < 5; i++) {
                DecreaseLevelInput();
            }
        }

        private void IncreaseLevel5xInput() {
            for (int i = 0; i < 5; i++) {
                IncreaseLevelInput();
            }
        }

        void IncreaseLevelInput() {
            int increaseLevelCount = 0;
            if (!Int32.TryParse(increaseLevelInput.text, out increaseLevelCount))
                return;
            increaseLevelCount++;
            increaseLevelInput.text = increaseLevelCount.ToString();

        }
        void DecreaseLevelInput() {
            int increaseLevelCount = 1;
            if (!Int32.TryParse(increaseLevelInput.text, out increaseLevelCount))
                return;
            increaseLevelCount--;
            if (increaseLevelCount < 1) increaseLevelCount = 1;
            increaseLevelInput.text = increaseLevelCount.ToString();
        }


        void LevelUp() {

            var context = _menu.Context;

            var progressValue = context.GameProgressValue;

            if (context.MenuManager == null)
                return;
            var currentLevelId = context.MenuManager.LastOpenedId;

            if (!context.LevelData.TryIncreaseLevel(ref currentLevelId))
                return;



            progressValue.NextLevelToPlay = currentLevelId;
            context.GameProgress.SaveValue(progressValue);

            SafePlayLevel(context, currentLevelId);
        }

        void EndCurrentLevel() {
            var currentLevel = _menu.Context.MenuManager.CurrentPlayer;
            if (currentLevel != null) {
                currentLevel.FinishGame(true);
            }
        }

        void GoToPreviousLevel() {
            var context = _menu.Context;

            var progressValue = context.GameProgressValue;
            if (context.MenuManager == null)
                return;
            var currentLevelId = context.MenuManager.LastOpenedId;

            context.LevelData.TryDecreaseLevel(ref currentLevelId);
            progressValue.NextLevelToPlay = currentLevelId;

            SafePlayLevel(context, currentLevelId);
            context.GameProgress.SaveValue(progressValue);

        }

        void GoToLevel() {
            var context = _menu.Context;
            if (!Int32.TryParse(increaseLevelInput.text, out int levelIndex))
                return;
            levelIndex--;
            var currentLevelId = context.MenuManager.LastOpenedId;
            context.LevelData.TryGetLinearIndex(currentLevelId, out var currentIndex);

            if (levelIndex > currentIndex) {
                int increaseCount = levelIndex - currentIndex;
                for (int i = 0; i < increaseCount; i++) {
                    if (!context.LevelData.TryIncreaseLevel(ref currentLevelId))
                        return;
                }
                SafePlayLevel(_menu.Context, currentLevelId);

            }
            else if (currentIndex > levelIndex) {
                int decreaseCount = currentIndex - levelIndex;
                for (int i = 0; i < decreaseCount; i++) {
                    if (!context.LevelData.TryDecreaseLevel(ref currentLevelId))
                        return;
                }
                SafePlayLevel(_menu.Context, currentLevelId);
            }
        }

        public static void SafePlayLevel(GameContext context, ALevelSet.ID id) {
            if (context == null || context.MenuManager == null || context.MenuManager.extraMenus == null ||
                context.MenuManager.extraMenus.Count <= 0 || context.MenuManager.extraMenus[0].Instance == null)
                return;

            context.MenuManager.TryOpenLevel(id);
        }
    }
}