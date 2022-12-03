using Mobge.Core;
using System;

namespace Mobge.HyperCasualSetup {
    public abstract class BaseLevelPlayer<SaveData> : BaseLevelPlayer where SaveData : LevelResult, new() {
        public override Type DataType => typeof(SaveData);
    }
    public class BaseLevelPlayer : LevelPlayer {
        protected LevelResult _saveData;
        public AGameContext Context { get; set; }
        
        private new void Awake() {
            var l = level;
            level = null;
            base.Awake();
            level = l;
        }
        public override void StartGame() {
            _saveData = (LevelResult)Activator.CreateInstance(DataType);
            base.StartGame();
        }
        public virtual Type DataType { get => typeof(LevelResult); }
        public event Action<BaseLevelPlayer, LevelFinishParams> OnLevelFinish;
        public event Action<BaseLevelPlayer, int> OnLevelChallengeChange;
        public event Action<BaseLevelPlayer, float> OnScoreChange;
        private bool _isFinished;
        public bool IsFinished => _isFinished;
        public int LevelChallenge {
            get => _saveData.levelChallenge;
            set {
                if(value != _saveData.levelChallenge) {
                    _saveData.levelChallenge = value;
                    OnLevelChallengeChange?.Invoke(this, _saveData.levelChallenge);
                }
            }
        }
        public float Score {
            get => _saveData.score;
            set {
                if(value != _saveData.score) {
                    _saveData.score = value;
                    OnScoreChange?.Invoke(this, _saveData.score);
                }
            }
        }
        public float TotalScore {
            get; set;
        }
        public int TotalChallenge {
            get; set;
        }
        public LevelResult SaveData => _saveData;
        public bool FinishGame(bool success, float delay = 0.5f) {
            if (!_isFinished) {
                _saveData.completed = success;
                _isFinished = true;
                if (OnLevelFinish != null) {
                    OnLevelFinish(this, new LevelFinishParams() {
                        menuDelay = delay,
                        success = success
                    });
                }
                return true;
            }
            return false;
        }
        public override void DestroyLevel() {
            base.DestroyLevel();
        }
        public struct LevelFinishParams {
            public float menuDelay;
            public bool success;
        }
    }
}