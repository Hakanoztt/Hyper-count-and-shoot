using Mobge.HyperCasualSetup;
using Mobge.IdleGame;
using Mobge.IdleGame.UI;
using Mobge.Platformer.Character;
using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame {
    public class PopupOpener : PopupOpener<BaseMenu> {


#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            if (contextOwner == null) {
                var newC = GetComponentInParent<IContextOwner>() as Component;
                if (newC != contextOwner) {
                    contextOwner = newC;

                }
            }
        }
#endif

    }
    public class PopupOpener<T> : TriggerUnitExtension where T : BaseMenu {

        public const string c_tag = "PopupTrigger";


        public override string TriggerTag => _popup != null ? _popup.TriggerTag : c_tag;

        public static PopupOpener<T> AssignPopup(Collider col, IUnit unit, PopupReference<T> popup) {
            if (popup.menuRes == null) {
                return null;
            }
            var opener = col.gameObject.GetOrAddComponent<PopupOpener<T>>();
            opener._unit = unit;
            //opener.reference.
            return opener;
        }


        public PopupReference<T> reference;

        private IPopup _popup;
        private AGameContext _context;

        private bool _open;

        public MenuManager MenuManager => _context.MenuManager;



        protected override void Initialize(IUnit unit) {
            _context = unit.Spawner.Player.Context;
            _popup = reference.GetInstance(_context.MenuManager) as IPopup;
        }


        protected override void TriggerEntered(Collider trigger, int newCount) {
            if (_open) {
                return;
            }
            if (_popup != null && !_popup.ShouldOpen(this, trigger)) {
                return;
            }
            reference.SetVisibility(MenuManager, true);
            if (_popup != null) {
                _popup.OnOpen(this);
            }
            _open = true;
        }

        protected override void TriggerExited(Collider trigger, int newCount) {
            if (_open) {
                reference.SetVisibility(MenuManager, false);
                if (_popup != null) {
                    _popup.OnClose(this);
                }
                _open = false;
            }
        }
    }
}


