using Mobge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Mobge.HyperCasualSetup.UI {
    public partial class MenuManager {


        public ILevelLoader LevelLoader {
            get => _levelLoader;
            private set {
                if (_levelLoader != value) {
                    if(_levelLoader != null) {
                        _levelLoader.Deactivate();
                    }
                    _levelLoader = value;
                    if(_levelLoader != null) {
                        _levelLoader.Activate(this);
                    }
                }
            }
        }

        public interface ILevelLoader {
            BaseLevelPlayer CurrentPlayer { get; set; }
            void Activate(MenuManager manager);
            void Deactivate();
            void LoadLevel(ALevelSet.ID id, Action<BaseLevelPlayer> onLoad, object loadParameters);
            void UnloadLevel(System.Action onUnLoad);
        }

        public class DefaultLevelLoader : ILevelLoader {
            private MenuManager _manager;
            private AGameContext Context => _manager.Context;
            private ALevelSet.AddressableLevel _levelResource;
            private object _loadParam;
            private Action<BaseLevelPlayer> _onLoad;
            private BaseLevelPlayer _player;
            private System.Action _onUnload;

            public BaseLevelPlayer CurrentPlayer {
                get => _player;
                set { _player = value; }
            }

            public void Activate(MenuManager manager) {
                _manager = manager;
            }
            public void Deactivate() {
                _manager = null;
            }
            public void LoadLevel(ALevelSet.ID id, Action<BaseLevelPlayer> onLoad, object loadParameters) {
                _onLoad = onLoad;
                var levelRes = Context.LevelData[id];
                _levelResource = levelRes;
                var loading = levelRes.LoadAssetAsync<BaseLevel>();
                _loadParam = loadParameters;
                loading.Completed += LoadLevelCompleted;


            }
            public void UnloadLevel(System.Action onUnload) {
                _manager.StartCoroutine(CloseLevelRoutine());
                _onUnload = onUnload;
            }
            private IEnumerator CloseLevelRoutine() {
                _player.DestroyLevel();
                _player = null;
                yield return new WaitForEndOfFrame();
                _levelResource.ReleaseAsset();
                _levelResource = null;
                yield return new WaitForEndOfFrame();
                Resources.UnloadUnusedAssets();
                yield return new WaitForEndOfFrame();
                GC.Collect();

                if (_onUnload != null) {
                    var a = _onUnload;
                    _onUnload = null;
                    a();
                }
            }
            
            private void LoadLevelCompleted(AsyncOperationHandle<BaseLevel> loading) {
                loading.Completed -= LoadLevelCompleted;
                if (loading.Status == AsyncOperationStatus.Succeeded) {
                    var level = loading.Result;
                    _player = (BaseLevelPlayer)new GameObject("player: " + level.name).AddComponent(level.PlayerType);
                    _player.Context = Context;
                    _player.OnGameStart += GameStarted;

                    _player.LoadLevel(level, _loadParam as Dictionary<object, object>);


                    _loadParam = null;
                }
                else {
                    throw new Exception("Cannot load level!");
                }
            }

            private void GameStarted(LevelPlayer obj) {
                if (_onLoad != null) {
                    var a = _onLoad;
                    _onLoad = null;
                    a(_player);
                }
            }

        }
    }
}