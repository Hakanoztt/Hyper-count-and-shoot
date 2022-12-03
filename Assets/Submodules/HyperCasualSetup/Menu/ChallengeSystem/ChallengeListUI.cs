using Mobge.Core;
using Mobge.Serialization;
using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI.ChallengeSystem {
    public class ChallengeListUI : BaseMenu {


        [OwnComponent] public Button backButton;
        public ChallengeData data;
        public Collection collection;
        public int loadingMenuIndex;



        public MenuManager.MenuReference challengeLevelHud;

        public string bestScoreFormat = "{0}";
        public int challengeTMPIndex = 2;
        public int challengeGameOverMenuExtraIndex = -1;
        public int challengePauseMenuExtraIndex = -1;

        public string claimKey;

        public string playWithoutClaimEvent;
        public string playWithClaimEvent;

        public string challengeScoreFormat = "{0}";

        public string progressDataKey = "challenge_levels";

        public bool noClaimOnlyFirstTime;
        public int noClaimPlayCount = 1;

        private ProgressData _progressData;

        private long _weekId;
        private int _timeOffset;

        public override void Prepare() {
            base.Prepare();
            UpdateList();
        }
        private  new MenuManager MenuManager {
            get => ((Mobge.HyperCasualSetup.UI.MenuManager)base.MenuManager);
        }
        private AGameProgress GameProgressValue {
            get {
                return MenuManager.Context.GameProgressValue;
            }
        }
        private void ReadData() {
            var bytes = GameProgressValue.GetBytes(progressDataKey);
            if (bytes == null) {
                _progressData = new ProgressData();
            }
            else {
                try {
                    BinaryObjectData bod;
                    bod.data = bytes;
                    bod.targets = null;
                    _progressData = BinaryDeserializer.Instance.Deserialize<ProgressData>(bod);
                }
                catch {
                    _progressData = new ProgressData();
                }
            }
        }
        public bool IsFirstLevelFree {
            get {
                if(collection.Count >= 0) {
                    var ui = collection[0];
                    return ui.buttons[0].button.gameObject.activeSelf;
                }
                return false;
            }
        }
        private void SaveData() {
            var bod = BinarySerializer.Instance.Serialize(typeof(ProgressData), _progressData);
            var ctx = (MenuManager).Context;
            var gpv = ctx.GameProgressValue;
            gpv.SetBytes(progressDataKey, bod.data);
            ctx.GameProgress.SaveValue(gpv);
        }

        void UpdateList() {
            _timeOffset = GetTimeOffset(DateTime.Now, out _weekId);
            var al = data.GetActiveLevels(_timeOffset);
            collection.Count = 0;
            int uiIndex = 0;
            var value = GameProgressValue;
            ReadData();

            while (al.MoveNext()) {
                int id = al.Current;
                ChallengeData.Level lvl = data.levels[id];
                collection.Count++;
                ChallengeListItem i = collection[uiIndex++];
                i.tag = id;
                i.textsTMPro[0].text = lvl.name;
                var leftTime = lvl.GetLeftTime(_timeOffset, out int intervalIndex);
                TimeSpan leftTs = TimeSpan.FromSeconds(leftTime);
                i.textsTMPro[1].text = ToString(leftTs);
                i.rewards.Count = lvl.scores.Length;
                i.images[0].sprite = lvl.banner;

                int challenge = 0;
                if (value.TryGetLevelResult(lvl.level, out var levelResult)) {
                    challenge = levelResult.levelChallenge;
                }

                i.textsTMPro[challengeTMPIndex].text = string.Format(bestScoreFormat, challenge);


                for (int j = 0; j < lvl.scores.Length; j++) {
                    var rui = i.rewards[j];
                    var scr = lvl.scores[j];
                    rui.textsTMPro[0].text = string.Format(challengeScoreFormat, scr.value);
                    rui.images[0].sprite = scr.icon;
                    rui.SetState(challenge >= scr.value ? 1 : 0);
                }

                i.buttons[0].OnClick -= OpenLevel;
                i.buttons[0].OnClick += OpenLevel;
                if (i.buttons.Length >= 2) {
                    i.buttons[1].OnClick -= OpenLevelWithAdd;
                    i.buttons[1].OnClick += OpenLevelWithAdd;
                }

                long sessionId = GetCurrentSessionId(lvl, intervalIndex, _weekId);
                int numOfPlay = _progressData.GetNumberOfPlays(id, sessionId);

                if(i.buttons.Length >= 2) {
                    bool showAdd = !string.IsNullOrEmpty(claimKey) && numOfPlay >= noClaimPlayCount;
                    i.buttons[0].button.gameObject.SetActive(!showAdd);
                    i.buttons[1].button.gameObject.SetActive(showAdd);
                }

            }
        }

        private long GetCurrentSessionId(ChallengeData.Level level, int intervalIndex, long weekId) {
            if (noClaimOnlyFirstTime) {
                return 0;
            }
            var startSecond = level.intervals[intervalIndex].start;
            return weekId + TimeSpan.FromSeconds(startSecond).Ticks;
        }

        public static int GetTimeOffset(DateTime date, out long weekId) {
            var day = (int)date.DayOfWeek;
            var hour = date.Hour;
            var minute = date.Minute;
            var second = date.Second;

            weekId = new DateTime(date.Year, date.Month, date.Day).Ticks - new TimeSpan(day).Ticks;

            return (((24 * day) + hour) * 60 + minute) * 60 + second;
        }

        private void OpenLevelWithAdd(UIItem arg1, int arg2) {
            Interactable = false;
            MenuManager.Context.ClaimReward(claimKey, (r) => {
                Interactable = true;
                if (r == AGameContext.ClaimResult.Claimed) {
                    OpenLevel(arg1, arg2, playWithClaimEvent);
                }
            });
        }
        private void OpenLevel(UIItem arg1, int arg2) {
            OpenLevel(arg1, arg2, playWithoutClaimEvent);
        }
        private void OpenLevel(UIItem arg1, int arg2, string eventName) {
            var mm = MenuManager;
            var level = data.levels[arg1.tag];
            var id = level.level;
            var hud = challengeLevelHud.InstanceSafe;
            if (hud == null) {
                hud = mm.gameHud.InstanceSafe;
            }
            var go = challengeGameOverMenuExtraIndex >= 0 ? mm.extraMenus[challengeGameOverMenuExtraIndex].InstanceSafe : mm.gameOverMenu.InstanceSafe;
            var pm = challengePauseMenuExtraIndex >= 0 ? mm.extraMenus[challengePauseMenuExtraIndex].InstanceSafe : mm.pauseMenu.InstanceSafe;

            if (mm.TryOpenLevel(id, hud, (GameOverMenu)go, (PauseMenu)pm)) {
                level.GetLeftTime(_timeOffset, out int intervalId);
                long sessionId = GetCurrentSessionId(level, intervalId, _weekId);
                _progressData.IncreaseNumberOfPlay(arg1.tag, sessionId);
                SaveData();
            }
        }

        string ToString(TimeSpan timeSpan) {
            if (timeSpan.Days > 0) {
                return string.Format("{0}d:{1:D2}h:{2:D2}m:{3:D2}s", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            }
            return string.Format("{0:D2}h:{1:D2}m:{2:D2}s", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
        }

        [Serializable] public class Collection : UICollection<ChallengeListItem> { }

        [Serializable]
        public class ProgressData {
            [SerializeField] private Dictionary<int, LevelData> levelDatas;
            public int GetNumberOfPlays(int levelId, long sessionId) {
                if (levelDatas == null) {
                    return 0;
                }
                if(!levelDatas.TryGetValue(levelId, out var ld)) {
                    return 0;
                }
                if(ld.sessionId != sessionId) {
                    return 0;
                }
                return ld.numberOfPlay;
            }
            public void IncreaseNumberOfPlay(int levelId, long sessionId) {
                if(levelDatas == null) {
                    levelDatas = new Dictionary<int, LevelData>();
                }
                LevelData ld;
                if(!levelDatas.TryGetValue(levelId, out ld) || ld.sessionId != sessionId) {
                    ld.numberOfPlay = 0;
                    ld.sessionId = sessionId;
                }
                ld.numberOfPlay++;
                levelDatas[levelId] = ld;
            }
        }

        [Serializable]
        public struct LevelData {
            public int numberOfPlay;
            public long sessionId;
        }

    }
}