using System;
using UnityEngine;

namespace Mobge.Core
{
    [CreateAssetMenu(menuName = "Mobge/Level")]
    [Serializable]
    public class Level : Piece
    {
        [SerializeField] protected GameSetup _gameSetup;
        public AssetReferenceDecorSet decorationSet;
        public virtual GameSetup GameSetup {
            get {
                if(_gameSetup == null) {
                    _gameSetup = Mobge.GameSetup.DefaultSetup;
                }
                return _gameSetup;
            }
            set => _gameSetup = value;
        } 
        public virtual Type PieceType => typeof(Piece);
        public virtual Type PlayerType => typeof(LevelPlayer);
        public virtual Type GameSetupType => typeof(GameSetup);

        public virtual void LoadReferences(AsyncOperationGroup operations) {
            if (decorationSet != null && decorationSet.RuntimeKeyIsValid()) {
                operations.Add<DecorationSet>(decorationSet.LoadAssetAsync<DecorationSet>());
            }
        }
        public LevelPlayer PlayLevel() {
            var lp = (LevelPlayer)new GameObject(name).AddComponent(PlayerType);
            lp.LoadLevel(this);
            return lp;
        }
    }
}
