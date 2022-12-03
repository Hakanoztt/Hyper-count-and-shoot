using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mobge.UI;
using System;
using Mobge.Core;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Mobge.HyperCasualSetup.UI
{
    public partial class MenuManager : Mobge.UI.MenuManager {


        public MenuReference gameHud;
        public MenuReference pauseMenu;
        public MenuReference loadingMenu;
        public MenuReference gameOverMenu;
        public MenuReference mainMenu;
        public MenuReference levelSelectMenu;
        public MenuReference marketMenu;

        [InterfaceConstraint(typeof(ILevelLoader)), SerializeField] private UnityEngine.Component levelLoaderComponent;


        public Action<MenuManager, LevelPlayer> onPlayerCreated;


        //private Dictionary<BaseMenu, BaseMenu> _extraMenuInstances;

        private AGameContext _gameContext;
        private ILevelLoader _levelLoader;
        private List<MenuReference> _allMenus;

        public GameHud CurrentHud { get; private set; }

        protected CurrentLevelInfo _currentLevelInfo = new CurrentLevelInfo();
        protected ALevelSet.ID _id, _lastId;
        protected Queue<Action> _queue;
        protected bool _isBusy;
        private System.Action _onAvailable;

        public ALevelSet.ID LastOpenedId => _lastId;
        public bool TryGetLastOpenedLinearIndex(out int index) {

            return Context.LevelData.TryGetLinearIndex(LastOpenedId, out index);
        }

        public AGameContext Context => _gameContext;

        public BaseLevelPlayer CurrentPlayer => _levelLoader.CurrentPlayer;
        public LevelEndResults CurrentResults => _currentLevelInfo.results;

        public override LevelPlayer CurrentLevel => this._levelLoader.CurrentPlayer;

        public GameOverMenu CurrentGameOverMenu {
            get {
                var go = this.gameOverMenu.GetTyped<GameOverMenu>();
                if (_currentLevelInfo.gameOverMenu != null) {
                    go = _currentLevelInfo.gameOverMenu;
                }
                return go;
            }
        }
        protected new void Awake() {
            _queue = new Queue<Action>();
            if(levelLoaderComponent is ILevelLoader loader) {
                LevelLoader = loader;
            }
            else {
                LevelLoader = new DefaultLevelLoader();
            }
            base.Awake();
        }

        public void NotifyWhenAvailable(System.Action action) {
            if (!_isBusy) {
                action();
            }
            else {
                _onAvailable += action;
            }
        }
        void UpdateNextLevelToPlay() {
            _id = Context.GameProgressValue.NextLevelToPlay;
        }
        public void StartGame(AGameContext context, out bool testMode) {
            _gameContext = context;
            InitializeMenus();
            var lr = FindObjectOfType<BaseLevelPlayer>();
            if (!lr || !lr.level) {

                if (!_isBusy) {
                    if (this.mainMenu.menuRes != null) {
                        _queue.Enqueue(new Action() {
                            operation = Operation.open,
                            type = ActionType.MainMenu,
                        });

                        this.ConsumeQueue(this);
                    }
                    else {
                        UpdateNextLevelToPlay();
                        _queue.Enqueue(new Action(Operation.open, ActionType.Loading));
                        _queue.Enqueue(new Action(Operation.open, ActionType.Level));
                        _queue.Enqueue(new Action(Operation.close, ActionType.Loading));
                        _queue.Enqueue(new Action(Operation.open, ActionType.Hud, true));
                        ConsumeQueue(this);
                    }
                }
                testMode = false;
            }
            else {
                testMode = true;
                lr.Context = context;
                _levelLoader.CurrentPlayer = lr;
                lr.LoadLevel(lr.level);
            }
        }

        private void InitializeMenus() {
            _allMenus = new List<MenuReference>();
            _allMenus.Add(levelSelectMenu);
            _allMenus.Add(gameHud);
            _allMenus.Add(pauseMenu);
            _allMenus.Add(gameOverMenu);
            if (mainMenu.menuRes != null) {
                _allMenus.Add(mainMenu);
            }
            if (marketMenu.menuRes != null) {
                _allMenus.Add(marketMenu);
            }

            if (loadingMenu.menuRes != null) {
                _allMenus.Add(loadingMenu);
            }
            for (int i = 0; i < _allMenus.Count; i++) {
                _allMenus[i].EnsureInit();
            }
        }


        protected virtual LevelSelectMenu PrepareLevelSelectMenu() {
            var lsm = levelSelectMenu.GetTyped<LevelSelectMenu>();
            lsm.onLevelSelected = TryOpenLevel;
            lsm.onBackPressed = LevelMenuBack;
            lsm.SetParameters(_gameContext);
            return lsm;
        }


        protected virtual GameHud PrepareGameHud(BaseLevelPlayer player) {
            return gameHud.GetTyped<GameHud>();
        }
        protected virtual PauseMenu PreparePauseMenu(BaseLevelPlayer player, PauseMenu pm) {
            pm.Prepare(player);
            pm.onContinueClicked = PauseMenuContinue;
            pm.onMenuClicked = GameToMenu;
            pm.onRestartClicked = LevelRestart;
            return pm;
        }
        private GameOverMenu PrepareGameOver(GameOverMenu gm) {
            gm.Prepare(_gameContext, _currentLevelInfo.results);
            gm.onMenuClicked = GameToMenu;
            gm.openNextLevel = OpenNextLevel;
            gm.onRestartClicked = LevelRestart;
            return gm;
        }
        private BasicMarketMenu PrepareMarket() {
            var gm = this.marketMenu.GetTyped<BasicMarketMenu>();
            gm.backButtonAction = MarketToMain;
            return gm;
        }
        protected virtual MainMenu PrepareMainMenu() {
            if (!mainMenu.menuRes) {
                return null;
            }
            var mm = mainMenu.GetTyped<MainMenu>();
            mm.onPlayGame = MainMenuPlayAction;
            mm.openLevelsMenu = MainMenuToLevelSelect;
            mm.openMarket = MainMenuToMarket;
            return mm;
        }


        private void MainMenuToLevelSelect(MainMenu obj) {
            if (IsTopAndReady(obj)) {
                _queue.Enqueue(new Action(Operation.popUntil, ActionType.Null));
                _queue.Enqueue(new Action(Operation.open, ActionType.LevelSelect));
                ConsumeQueue(this);
            }
        }

        private void LevelMenuBack(LevelSelectMenu menu) {
            if (IsTopAndReady(menu)) {
                _queue.Enqueue(new Action(Operation.popUntil, ActionType.Null));
                _queue.Enqueue(new Action(Operation.open, ActionType.MainMenu));
                ConsumeQueue(this);
            }
        }
        private void MarketToMain(BaseMarketMenu market) {
            if (IsTopAndReady(market)) {
                _queue.Enqueue(new Action(Operation.popUntil, ActionType.Null));
                _queue.Enqueue(new Action(Operation.open, ActionType.MainMenu));
                ConsumeQueue(this);
            }
        }
        private void MainMenuToMarket(MainMenu obj) {
            if (IsTopAndReady(obj)) {
                _queue.Enqueue(new Action(Operation.popUntil, ActionType.Null));
                _queue.Enqueue(new Action(Operation.open, ActionType.Market));
                ConsumeQueue(this);
            }
        }

        private void MainMenuPlayAction(MainMenu obj) {
            if (IsTopAndReady(obj)) {
                UpdateNextLevelToPlay();
                _queue.Enqueue(new Action(Operation.open, ActionType.Loading));
                _queue.Enqueue(new Action(Operation.popUntil, ActionType.Null, true));
                _queue.Enqueue(new Action(Operation.close, ActionType.Level));
                _queue.Enqueue(new Action(Operation.open, ActionType.Level));
                _queue.Enqueue(new Action(Operation.close, ActionType.Loading));
                _queue.Enqueue(new Action(Operation.open, ActionType.Hud, true));
                ConsumeQueue(this);
            }
        }

        private bool HandleMenuAction(BaseMenu menu, Operation op, bool immediate) {
            var t = TopMenu;
            switch (op) {
                default:
                case Operation.open:
                    return this.PushMenu(menu, ConsumeQueue, !immediate);
                case Operation.close:
                    return this.PopMenuControlled(menu, ConsumeQueue, !immediate);
                case Operation.popUntil:
                    return this.PopUntil(menu, ConsumeQueue, !immediate);
            }
        }

        protected void ConsumeQueue(Mobge.UI.MenuManager obj) {
            if (_queue.Count > 0) {
                ExecuteAction(_queue.Dequeue());
            }
            else {
                _isBusy = false;
                if (_onAvailable != null) {
                    _onAvailable();
                    _onAvailable = null;
                }
            }
        }

        public bool LevelRestart(object args) {
            if (!_isBusy) {
                _id = _lastId;
                _queue.Enqueue(new Action(Operation.open, ActionType.Loading));
                _queue.Enqueue(new Action(Operation.open, ActionType.TimeScale));
                _queue.Enqueue(new Action(Operation.popUntil, ActionType.Null, true));
                _queue.Enqueue(new Action(Operation.close, ActionType.Level));
                _queue.Enqueue(new Action(Operation.open, ActionType.Level, false, args));
                _queue.Enqueue(new Action(Operation.open, ActionType.Hud, false));
                _queue.Enqueue(new Action(Operation.close, ActionType.Loading));
                ConsumeQueue(this);
                return true;
            }
            return false;
        }

        private void GameToMenu(BaseMenu obj) {
            if (IsTopAndReady(obj)) {

                _queue.Enqueue(new Action(Operation.open, ActionType.TimeScale));
                _queue.Enqueue(new Action(Operation.open, ActionType.Loading));
                _queue.Enqueue(new Action(Operation.popUntil, ActionType.Null, true));
                _queue.Enqueue(new Action(Operation.close, ActionType.Level));
                _queue.Enqueue(new Action(Operation.open, ActionType.MainMenu, true));
                _queue.Enqueue(new Action(Operation.close, ActionType.Loading));
                ConsumeQueue(this);
            }
        }


        private void PauseMenuContinue(PauseMenu obj) {
            if (IsTopAndReady(obj)) {
                _queue.Enqueue(new Action(Operation.close, ActionType.Pause));
                _queue.Enqueue(new Action(Operation.open, ActionType.TimeScale));
                ConsumeQueue(this);
            }
        }

        public bool TryPopMenu(BaseMenu menu) {
            if (IsTopAndReady(menu)) {
                _queue.Enqueue(new Action(Operation.open, ActionType.CustomMenu, false));
                return true;
            }
            return false;
        }

        public bool PauseGame(BaseMenu gameHud) {
            if (IsTopAndReady(gameHud)) {
                _queue.Enqueue(new Action(Operation.close, ActionType.TimeScale));
                _queue.Enqueue(new Action(Operation.open, ActionType.Pause));
                ConsumeQueue(this);
                return true;
            }
            return false;
        }
        public bool IsTopAndReady(BaseMenu m) {
            return TopMenu == m && (m == null || m.CurrentState == BaseMenu.State.Open) && !_isBusy;
        }


        public bool TryOpenLevel(ALevelSet.ID id, BaseMenu hud, GameOverMenu gameOverMenu = null, PauseMenu pauseMenu = null, object args = null) {
            if (!_isBusy) {
                _id = id;
                CurrentHud = (GameHud)hud;
                _queue.Enqueue(new Action(Operation.open, ActionType.Loading));
                _queue.Enqueue(new Action(Operation.popUntil, ActionType.Null, true));
                _queue.Enqueue(new Action(Operation.close, ActionType.Level));
                _queue.Enqueue(new Action(Operation.open, ActionType.Level, false, args));
                _queue.Enqueue(new Action((o) => {
                    _currentLevelInfo.gameOverMenu = gameOverMenu;
                    _currentLevelInfo.pauseMenu = pauseMenu;
                    ConsumeQueue(this);
                }));
                _queue.Enqueue(new Action(Operation.close, ActionType.Loading));
                if (CurrentHud != null) {
                    _queue.Enqueue(new Action(Operation.open, ActionType.CustomMenu, false, hud));
                }
                ConsumeQueue(this);
                return true;
            }
            return false;
        }
        public void OpenNextLevel(BaseMenu obj) {
            if (IsTopAndReady(obj)) {
                UpdateNextLevelToPlay();
                _queue.Enqueue(new Action(Operation.open, ActionType.TimeScale));
                _queue.Enqueue(new Action(Operation.popUntil, ActionType.Null));
                _queue.Enqueue(new Action(Operation.open, ActionType.Loading));
                _queue.Enqueue(new Action(Operation.close, ActionType.Level));
                _queue.Enqueue(new Action(Operation.open, ActionType.Level));
                _queue.Enqueue(new Action(Operation.close, ActionType.Loading));
                _queue.Enqueue(new Action(Operation.open, ActionType.Hud));
                ConsumeQueue(this);
            }
        }
        public bool TryOpenLevel(LevelData.ID id) {
            return TryOpenLevel(id, gameHud.InstanceSafe);
        }

        private void ExecuteAction(Action a) {
            _isBusy = true;
            bool handled = false;
            switch (a.type) {
                case ActionType.Null:
                    handled = this.HandleMenuAction(null, a.operation, a.immediate);
                    break;
                case ActionType.MainMenu:
                    handled = this.HandleMenuAction(PrepareMainMenu(), a.operation, a.immediate);
                    break;
                case ActionType.LevelSelect:
                    handled = this.HandleMenuAction(PrepareLevelSelectMenu(), a.operation, a.immediate);
                    break;
                case ActionType.Hud:

                    CurrentHud = PrepareGameHud(CurrentPlayer);
                    handled = this.HandleMenuAction(CurrentHud, a.operation, a.immediate);
                    break;
                case ActionType.Pause:
                    var pm = this.pauseMenu.GetTyped<PauseMenu>();
                    if (_currentLevelInfo.pauseMenu != null) {
                        pm = _currentLevelInfo.pauseMenu;
                    }
                    handled = this.HandleMenuAction(PreparePauseMenu(CurrentPlayer, pm), a.operation, a.immediate);
                    break;
                case ActionType.Market:
                    handled = this.HandleMenuAction(PrepareMarket(), a.operation, a.immediate);
                    break;
                case ActionType.GameOver:
                    var go = CurrentGameOverMenu;
                    handled = this.HandleMenuAction(PrepareGameOver(go), a.operation, a.immediate);
                    break;
                case ActionType.CustomMenu:
                    handled = this.HandleMenuAction((BaseMenu)a.data, a.operation, a.immediate);
                    break;
                case ActionType.Level:
                    if (a.operation == Operation.open) {
                        _id = _gameContext.LevelData.ToNearestLevelId(_id);
                        _lastId = _id;
                        _levelLoader.LoadLevel(_id, OnLevelLoaded, a.data);
                        handled = true;
                    }
                    else {
                        CloseLevel();
                        handled = true;
                    }
                    break;
                case ActionType.TimeScale:
                    Time.timeScale = a.operation == Operation.open ? 1 : 0;
                    ConsumeQueue(this);
                    handled = true;
                    break;
                case ActionType.Loading:
                    if (loadingMenu.Instance) {
                        loadingMenu.Instance.SetEnabledWithAnimation(a.operation == Operation.open, LoadingEnded);
                        handled = true;
                    }
                    else {
                        handled = true;
                        ConsumeQueue(this);
                    }
                    break;
                case ActionType.Delay:
                    StartCoroutine(ConsumeQueueDelayed(a.param1));

                    handled = true;
                    break;
                case ActionType.CustomAction:
                    a.customAction(a);
                    handled = true;
                    break;
                default:
                    break;
            }
            if (!handled) {
                Debug.LogError("action is not handled: " + a);
            }
        }
        private void LoadingEnded(bool completed, object data) {
            ConsumeQueue(this);
        }
        private void CloseLevel() {
            if (_levelLoader.CurrentPlayer != null) {
                _currentLevelInfo.OnLevelUnload();
                _levelLoader.UnloadLevel(OnLevelUnload);
            }
            else {
                ConsumeQueue(this);
            }
        }

        private void OnLevelUnload() {
            ConsumeQueue(this);
        }

        private IEnumerator ConsumeQueueDelayed(float delay) {
            yield return new WaitForSecondsRealtime(delay);
            ConsumeQueue(this);
        }
        private void OnLevelLoaded(BaseLevelPlayer player) {
            player.OnLevelFinish += OnLevelFinished;
            _currentLevelInfo.OnLevelLoad();
            ConsumeQueue(this);
        }
        

        private void OnLevelFinished(BaseLevelPlayer player,BaseLevelPlayer.LevelFinishParams @params) {
            var gameProgress = this._gameContext.GameProgressValue;
            gameProgress.TryGetLevelResult(_id, out LevelResult oldLevelResult);
            if (@params.success) {
                var newLevelResult = player.SaveData;
                var mergedResult = new LevelResult();
                mergedResult.Merge(newLevelResult);
                mergedResult.Merge(oldLevelResult);
                gameProgress.SetLevelResult(_gameContext, _id, mergedResult);
                if (_gameContext.LevelData.TryIncreaseLevel(ref _id)) {
                    gameProgress.NextLevelToPlay = _id;
                }
                _gameContext.GameProgress.SaveValue(gameProgress);
                _currentLevelInfo.results.NewResult = newLevelResult;
                _currentLevelInfo.results.MergedResult = mergedResult;
                _currentLevelInfo.results.OldResult = oldLevelResult;
            } else {
                _currentLevelInfo.results.NewResult = player.SaveData;
                _currentLevelInfo.results.OldResult = oldLevelResult;
                _currentLevelInfo.results.MergedResult = null;
            }
            CurrentPlayer.RoutineManager.DoAction(FinishLevel, @params.menuDelay);
        }

        private void FinishLevel(bool completed, object data) {

            if (CurrentPlayer != null && CurrentPlayer.IsFinished && !_currentLevelInfo.Handled) {
                _currentLevelInfo.Handled = true;

                if (_currentLevelInfo.results.NewResult.completed) {

                    var goM = CurrentGameOverMenu;
                    if (goM == null) {
                        OpenNextLevel(TopMenu);
                    }
                    else {

                        _queue.Enqueue(Action.DelayAction(0.5f));
                        _queue.Enqueue(new Action(Operation.popUntil, ActionType.Null));

                        _queue.Enqueue(new Action(Operation.open, ActionType.GameOver));

                        ConsumeQueue(this);
                    }
                }
                else {
                    _queue.Enqueue(Action.DelayAction(0.5f));
                    LevelRestart(null);
                }
            }
        }



        protected struct Action {
            public Operation operation;
            public ActionType type;
            public bool immediate;
            public float param1;
            private BaseMenu customMenu;
            public object data;
            public System.Action<Action> customAction;

            public Action(System.Action<Action> customAction, object data  = null) : this() {
                this.customAction = customAction;
                this.data = data;
                this.type = ActionType.CustomAction;
            }

            public Action(Operation operation, BaseMenu customMenu, bool immediate = false, object data = null) {
                this.customMenu = customMenu;
                this.operation = operation;
                this.immediate = immediate;
                this.type = ActionType.CustomMenu;
                this.param1 = 0;
                this.data = data;
                this.customAction = null;
            }
            public Action(Operation operation, ActionType type, bool immediate = false, object data = null) {
                this.operation = operation;
                this.type = type;
                this.immediate = immediate;
                param1 = 0;
                this.customMenu = null;
                this.data = data;
                this.customAction = null;
            }
            public static Action DelayAction(float duration) {
                Action a = new Action();
                a.param1 = duration;
                a.type = ActionType.Delay;
                return a;
            }
            public override string ToString() {
                return "(" + operation + " , " + type + ")";
            }
        }
        public enum Operation {
            open,
            close,
            popUntil,
        }
        public enum ActionType {
            MainMenu,
            LevelSelect,
            Hud,
            Level,
            Market,
            Pause,
            TimeScale,
            Loading,
            Null,
            GameOver,
            Delay,
            CustomMenu,
            CustomAction
        }
        public struct LevelEndResults {
            public LevelResult OldResult { get; set; }
            public LevelResult NewResult { get; set; }
            public LevelResult MergedResult { get; set; }
            public void Clear() {
                OldResult = null;
                NewResult = null;
                MergedResult = null;
            }
        }
        public class CurrentLevelInfo
        {

            public LevelEndResults results;
            public GameOverMenu gameOverMenu;
            public PauseMenu pauseMenu;

            public bool Handled { get; set; }
            

            public void OnLevelLoad() {
                results.Clear();
                Handled = false;
            }
            public void OnLevelUnload() {
                gameOverMenu = null;
                pauseMenu = null;
                results.Clear();
            }


        }

    }
}