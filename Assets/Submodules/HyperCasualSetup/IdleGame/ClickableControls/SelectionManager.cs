using Mobge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame {
    public class SelectionManager {

        public const string c_key = "idlSel";

        public static SelectionManager Get(LevelPlayer player) {
            if(!player.TryGetExtra(c_key, out SelectionManager sel)) {
                sel = new SelectionManager();
                sel._player = player;
                player.SetExtra(c_key, sel);
            }
            return sel;
        }

        public static void SetManager(LevelPlayer player, SelectionManager manager) {
            if(player.TryGetExtra(c_key, out SelectionManager old)) {
                old.Clear();
            }
            player.SetExtra(c_key, manager);
            manager._player = player;
        }


        public LevelPlayer Player => _player;
        private LevelPlayer _player;

        private List<BaseSelectable> _selection;

        private SelectionManager() {
            _selection = new List<BaseSelectable>();
        }

        public BaseSelectable Selection {
            get => _selection.Count > 0 ? _selection[0] : null;
            set {
                Clear();
                if (value != null) {
                    _selection.Add(value);
                    value.OnSelected();
                }
            }
        }
        public void Add(BaseSelectable selectable) {
            if (!_selection.Contains(selectable)) {
                _selection.Add(selectable);
                selectable.OnSelected();
            }
        }
        public void Remove(BaseSelectable selectable) {
            if (_selection.Remove(selectable)) {
                selectable.OnDeselected();
            }
        }
        public void Clear() {
            for(int i = 0; i <_selection.Count;i++) {
                _selection[i].OnDeselected();
            }
            _selection.Clear();
        }
    }
}