using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.IdleGame.UI {
    public class UnitUpgradePopup : BaseMenu, IPopup {

        public MultipleCostPanel costCollection;
        public Transform maxedLabel;
        public Button upgradeButton;
        public float cameraOffset = 1f;
        public AReusableItem notEnoughMoneyEffect;
        private IUnit _unit;
        private TriggerUnitExtension _opener;
        private WalletComponent _enteringWallet;


        public UnitSpawnerComponent.Data Data { get; set; }

        string IPopup.TriggerTag => WalletComponent.c_tag;

        public override void Prepare() {
            base.Prepare();
            upgradeButton.onClick.RemoveListener(UpgradeAction);
            upgradeButton.onClick.AddListener(UpgradeAction);
        }

        private void UpgradeAction() {
            if (_unit.CanBeUpgraded()) {
                var cost = _unit.Spawner.GetUpgradeCost();
                if(_enteringWallet.TryRemove(cost, out _)) {
                    _unit.Spawner.Upgrade();
                }
                else {
                    if (notEnoughMoneyEffect != null) {
                        notEnoughMoneyEffect.Play();
                    }
                }
                UpdateVisuals();
            }
        }

        bool IPopup.ShouldOpen<T>(PopupOpener<T> opener, Collider trigger) {
            if (WalletComponent.TryGet(trigger, out _enteringWallet)) {
                return true;
            }
            return false;
        }

        void IPopup.OnOpen<T>(PopupOpener<T> opener) {
            _unit = opener.GetComponentInParent<IUnit>();
            _opener = opener;

            UpdateVisuals();
        }


        void IPopup.OnClose<T>(PopupOpener<T> opener) {
            _enteringWallet = null;
        }

        void UpdateVisuals() {

            costCollection.UpdateUpgradeCost(_unit, out _);
            RefreshMaxedUI(!_unit.CanBeUpgraded());
            
        }
        void RefreshMaxedUI(bool maxed) {
            if (maxedLabel != null) {
                maxedLabel.transform.gameObject.SetActive(maxed);
            }
            if (upgradeButton != null) {
                upgradeButton.transform.gameObject.SetActive(!maxed);
            }
        }


        protected void LateUpdate() {
            var camera = Camera.main;
            var camTr = camera.transform;
            var worldPosition = _opener.transform.position;
            var camPos = camTr.position;
            var camForward = camTr.forward;
            transform.rotation = Quaternion.LookRotation(camForward);
            Vector3 pos;
            if (camera.orthographic) {
                float verticalDistance = Vector3.Dot(camForward, worldPosition - camPos);
                float extraDistance = verticalDistance - this.cameraOffset;
                pos = worldPosition - camForward * extraDistance;
            }
            else {
                var dir = worldPosition - camPos;
                dir.Normalize();
                pos = camPos + dir * this.cameraOffset;
            }
            this.transform.position = pos;
        }

    }
}