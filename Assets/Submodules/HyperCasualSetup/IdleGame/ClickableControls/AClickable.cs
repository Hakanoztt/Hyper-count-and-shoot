using Mobge.Core;
using Mobge.HyperCasualSetup;
using Mobge.IdleGame;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame {
    public abstract class AClickable : MonoBehaviour {

        public const string c_tag = "ClickableUnit";
        public Action<AClickable> onClick;
        private BaseLevelPlayer _player;
        [SerializeField] private new bool enabled = true;

        public static bool TryGet<T>(Collider trigger, BaseLevelPlayer player, out T clickable) where T : AClickable {
            if(trigger.isTrigger && trigger.TryGetComponent(out clickable)) {
                if (clickable._player == player) {
                    return clickable;
                }
            }
            clickable = default;
            return false;
        }

        public BaseLevelPlayer Player {
            get => _player;
        }

        protected void Awake() {
            gameObject.tag = c_tag;
        }
        protected void Start() {
            _player = (BaseLevelPlayer)this.GetLevelPlayer();
            if (enabled) {
                ClickControls.Get(_player).Register(this);
            }
        }

        public virtual bool Enabled {
            get => enabled;
            set {
                if (value != enabled) {
                    enabled = value;
                    if (_player != null) {
                        if (enabled) {
                            ClickControls.Get(_player).Register(this);
                        }
                        else {
                            ClickControls.Get(_player).UnRegister(this);
                        }
                    }
                }
            }
        }

        public virtual void HandlePointerDown() {

        }
        public virtual void HandleClick() {
            if (onClick != null) {
                onClick(this);
            }
        }

        public virtual void HandlePointerUp() {

        }
    }
}