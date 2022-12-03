using Mobge.Animation;
using Mobge.HyperCasualSetup;
using Mobge.IdleGame.UI;
using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mobge.IdleGame {

    public class OnStandUnitUpgrader : TriggerUnitExtension {
        

        public float paymentDelay = 2f;
        public Animator animator;

        [Header("Speed of this animation must set to 0.")]
        [AnimatorState] public int loadingAnimation;
        [AnimatorState] public int notEnoughMoneyAnimation;
        public int animationLayer;

        public MultipleCostPanel costPanel;


        public AReusableItem notEnoughMoneyEffect;

        private WalletComponent _pendingWallet;
        private RoutineManager.Routine _waitingRoutine;
        private ItemCluster _nextUpgradeCost;

        public bool WaitingToPay {
            get => !_waitingRoutine.IsFinished;
        }

        public override string TriggerTag => WalletComponent.c_tag;

        protected override void Initialize(IUnit unit) {
            UpdateForNextLevel();
        }





        private void UpdateForNextLevel() {
            if (Unit == null) {
                return;
            }
            if (Unit.Spawner.CanBeUpgraded) {
                gameObject.SetActive(true);
                _nextUpgradeCost = Unit.Spawner.GetUpgradeCost();
                costPanel.UpdateCost(_nextUpgradeCost);
            }
            else {
                gameObject.SetActive(false);
            }
        }

        protected override void TriggerEntered(Collider trigger, int newCount) {
            if(!_unit.CanBeUpgraded()) {
                return;
            }
            if(!WaitingToPay) {
                if(WalletComponent.TryGet(trigger, out var wallet)) {
                    if (wallet.HasEnough(_nextUpgradeCost, out _)) {
                        _pendingWallet = wallet;
                        _waitingRoutine = _unit.Spawner.Player.RoutineManager.DoAction(DelayTimerEnd, this.paymentDelay, UpdateWaitingAnim);
                    }
                    else {
                        DoNotEnoughMoneyAction();
                    }
                }
            }
        }

        private void DoNotEnoughMoneyAction() {
            animator.Play(this.notEnoughMoneyAnimation, animationLayer, 0f);
            if (notEnoughMoneyEffect != null) {
                notEnoughMoneyEffect.Play();
            }
        }

        private void DelayTimerEnd(bool complete, object data) {
            if (complete) {
                SetAnimationProgress(loadingAnimation, 0);
                if (!_unit.CanBeUpgraded()) {
                    return;
                }
                if (_pendingWallet.TryRemove(Unit.Spawner.GetUpgradeCost(), out _)) {
                    Unit.Spawner.Upgrade();
                    UpdateForNextLevel();
                }
                else {
                    DoNotEnoughMoneyAction();
                }
            }
            else {
                SetAnimationProgress(loadingAnimation, 0f);
            }
        }

        private void UpdateWaitingAnim(float progress, object data) {
            SetAnimationProgress(loadingAnimation, progress);
        }

        protected override void TriggerExited(Collider trigger, int newCount) {
            if(_pendingWallet != null) {
                if (_pendingWallet.HasCollider(trigger)) {
                    CancelTransaction();
                    _pendingWallet = null;
                }
            }
        }
        private void CancelTransaction() {
            if (WaitingToPay) {
                _waitingRoutine.Stop();
            }
        }
        private void SetAnimationProgress(int state, float progress) {
            animator.Play(state, animationLayer, progress);
        }
    }
}