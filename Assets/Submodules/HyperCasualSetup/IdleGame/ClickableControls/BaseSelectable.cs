using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mobge.IdleGame {
    public class BaseSelectable : AClickable {
        private bool _selected;
        public OnClickBehaviour onClickBehaviour;
        [Tooltip("Use " + nameof(BasicReusableItem) + " for just activating and deactivating a game object.")]
        public AReusableItem selectedEffect;
        public Action<BaseSelectable> onSelectionStateChanged;

        private SelectionManager _selectionManager;

        protected new void Start() {
            base.Start();
            _selectionManager = SelectionManager.Get(this.GetLevelPlayer());
            if (_selected) {
                _selectionManager.Selection = this;
            }
        }

        public override bool Enabled {
            get => base.Enabled;
            set {
                base.Enabled = value;
                if (!base.Enabled) {
                    Selected = false;
                }
            }
        }

        public bool Selected {
            get => _selected;
            set {
                if (_selected != value) {
                    if (_selectionManager != null) {
                        if (value) {
                            _selectionManager.Selection = this;
                        }
                        else {
                            _selectionManager.Remove(this);
                        }
                    }
                    else {
                        _selected = value;
                    }
                }
            }
        }

        public virtual void OnSelected() {
            _selected = true;
            if (selectedEffect != null) {
                selectedEffect.Play();
            }
            if (onSelectionStateChanged != null) {
                onSelectionStateChanged(this);
            }
        }
        public virtual void OnDeselected() {
            _selected = false;
            if (selectedEffect != null) {
                selectedEffect.Stop();
            }
            if (onSelectionStateChanged != null) {
                onSelectionStateChanged(this);
            }
        }

        public override void HandleClick() {
            switch (onClickBehaviour) {
                case OnClickBehaviour.Select:
                    Selected = true;
                    break;
                case OnClickBehaviour.Toggle:
                    Selected = !Selected;
                    break;
            }
        }


        public enum OnClickBehaviour {
            Select = 0,
            Toggle = 1,
            None = 10,
        }
    }
}