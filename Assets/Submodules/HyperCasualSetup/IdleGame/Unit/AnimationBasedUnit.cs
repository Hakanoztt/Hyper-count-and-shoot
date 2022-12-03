using Mobge.Animation;
using Mobge.Core;
using Mobge.Core.Components;
using Mobge.HyperCasualSetup;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame
{
    public class AnimationBasedUnit : MonoBehaviour, IUnit
    {
        [OwnComponent] public Animator animator;

        public RankInfo[] ranks;

        public AReusableItem upgradeEffect;

        private AnimationBlock _waitingState;
        public BaseLevelPlayer Player => Spawner.Player;
        public IUnit.UpgradeStyle upgradeStyle = IUnit.UpgradeStyle.Stop;

        int IUnit.RankCount => ranks.Length;
        public AGameContext Context => Player.Context;
        public UnitSpawnerComponent.Data Spawner { get; set; }

        public virtual void SetLevel(int rank, bool initial)
        {
            var b = new AnimationBlock(ranks[rank].animationState, initial);
            if (animator.isInitialized)
            {
                Apply(b);
            }
            else
            {
                _waitingState = b;
                enabled = true;
            }
        }
        ItemCluster IUnit.GetDefaultCost(int index)
        {
            return ranks[index].cost;
        }
        IUnitRank IUnit.GetRank(int index)
        {
            return this.ranks[index].Rank;
        }
        IUnit.UpgradeStyle IUnit.UpgradeStyleWhenMaxed => upgradeStyle;

        protected void Update()
        {
            if (!animator.isInitialized)
            {
                return;
            }
            if (_waitingState.state == 0)
            {
                enabled = false;
                return;
            }
            Apply(_waitingState);
            _waitingState.state = 0;
        }

        private void Apply(AnimationBlock block)
        {
            animator.Play(block.state, 0, block.immediate ? 1f : 0f);
            if (upgradeEffect != null)
            {
                upgradeEffect.Play();
            }
        }


        private struct AnimationBlock
        {
            public int state;
            public bool immediate;

            public AnimationBlock(int state, bool immediate)
            {
                this.state = state;
                this.immediate = immediate;
            }
        }
        [Serializable]
        public struct RankInfo
        {
            [AnimatorState] public int animationState;
            public ItemCluster cost;
            [InterfaceConstraint(typeof(IUnitRank))] public Component rankComponent;
            private IUnitRank _rank;
            public IUnitRank Rank
            {
                get
                {
                    if (_rank == null)
                    {
                        if (rankComponent != null)
                        {
                            _rank = (IUnitRank)rankComponent;
                        }
                    }
                    return _rank;
                }
            }
        }
    }
}