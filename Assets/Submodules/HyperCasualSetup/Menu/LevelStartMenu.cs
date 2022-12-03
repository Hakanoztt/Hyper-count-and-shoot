using Mobge.Core;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI {

    public class LevelStartMenu : Mobge.UI.BaseMenu {

        public const string c_levelStartActionKey = "lsm_start";
        [OwnComponent] public Button playButton;
        private Mobge.HyperCasualSetup.UI.MenuManager _menuManager;
        private BaseLevelPlayer _player;

        private uint _currentSession;
        private uint _pushedForSession;
        public uint CurrentSession => _currentSession;

        [Header("Sound FX")]
        [SerializeField]
        private ReusableReference gameStartSoundEffect;

        /// <summary>
        /// This method can be called from anywhere, any number of times, anytime (even after level start menu is closed). Start menu will open only one time and <paramref name="onLevelStart"/> will be always fired. This function works in editor too.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="extraMenuIndex"></param>
        /// <param name="onLevelStart"></param>
        public static void OpenMenu(LevelPlayer player, int extraMenuIndex, Action onLevelStart) {
            bool subscribed = false;
            if (player is BaseLevelPlayer bPlayer) {
                var mm = bPlayer.Context.MenuManager;
                if (mm.CurrentPlayer != null) {
                    if (mm != null) {
                        bool available = mm.extraMenus != null && extraMenuIndex < mm.extraMenus.Count;
                        if (available && (mm.extraMenus[extraMenuIndex].InstanceSafe is LevelStartMenu lsMenu)) {
                            lsMenu.OpenMenu(mm, bPlayer, onLevelStart);
                            subscribed = true;
                        } else {
                            Debug.LogWarning($"There is no {typeof(LevelStartMenu)} in extra menus at index {extraMenuIndex} for menu manager given at context.", mm);
                        }
                    }
                }
            }
            if (!subscribed) {
                onLevelStart?.Invoke();
            }
        }

        private void OpenMenu(MenuManager mm, BaseLevelPlayer player, Action onLevelStart) {
            var ses = player.Session;
            if (mm.TopMenu == this || ses != _currentSession) {
                AddOnStartAction(player, onLevelStart);
                if (ses != _currentSession && ses != _pushedForSession) {
                    _pushedForSession = ses;
                    mm.NotifyWhenAvailable(() => {
                        mm.PushMenu(this);
                    });
                }
            } else {
                onLevelStart?.Invoke();
            }
        }

        private void AddOnStartAction(BaseLevelPlayer player, Action action) {
            if (player.TryGetExtra(c_levelStartActionKey, out Action a)) {
                a += action;
                player.SetExtra(c_levelStartActionKey, a);
            } else {
                player.SetExtra(c_levelStartActionKey, action);
            }
        }

        public override void Prepare() {
            base.Prepare();
            _menuManager = (MenuManager)MenuManager;
            _player = _menuManager.CurrentPlayer;
            if (_player.Session != _currentSession) {
                _currentSession = _player.Session;
                if (playButton != null) {
                    playButton.onClick.RemoveListener(PlayGameAction);
                    playButton.onClick.AddListener(PlayGameAction);
                }
            }
        }

        private void PlayGameAction() {
            PlayGame();
        }

        public bool PlayGame() {
            gameStartSoundEffect.SpawnItem(Vector3.zero, transform);
            return _menuManager.PopMenuControlled(this, StartLevel);
        }

        private void StartLevel(Mobge.UI.MenuManager obj) {
            FireOnLevelStart();
        }

        private void FireOnLevelStart() {
            _menuManager.CurrentPlayer.RemoveExtra<Action>(c_levelStartActionKey)?.Invoke();
        }
    }
}