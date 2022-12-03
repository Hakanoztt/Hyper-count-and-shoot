using Mobge.Core;
using Mobge.Core.Components;
using Mobge.Platformer.Character;
using Mobge.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.UI {
    public class HealthBarCanvas : MonoBehaviour, IComponentExtension {
        public const string c_key = "hcsHBCnv";

        [Header("Images 1: health amount")]
        public UICollection<UIItem> healthBars;

        private List<BarRef> _tempModules;

        private Manager _manager;
        private Canvas _canvas;
        private RectTransform _tr;

        public static Manager GetManager(LevelPlayer player) {
            return Manager.Get(player);
        }


        void IComponentExtension.Start(in BaseComponent.InitArgs initData) {
            _manager = GetManager(initData.player);
            _canvas = GetComponent<Canvas>();
            _tr = (RectTransform)_canvas.transform;
            _canvas.worldCamera = Camera.main;
            _tempModules = new List<BarRef>();
        }

        protected void LateUpdate() {
            _tempModules.Clear();
            for (int i = 0; i < _manager.healths.Count; ) {
                var n = _manager.healths.array[i];
                if (!n.notifier.Visible) {
                    _manager.healths.RemoveFast(i);
                }
                else {
                    if (!n.notifier.health.Full) {
                        _tempModules.Add(n);
                    }
                    i++;
                }
            }

            healthBars.Count = _tempModules.Count;
            var camera = _canvas.worldCamera;
            for (int i = 0; i < _tempModules.Count; i++) {
                var man = _tempModules[i];
                float percentage = man.notifier.health.Percentage;
                var ui = healthBars[i];
                ui.images[0].fillAmount = percentage;
                var screen = camera.WorldToScreenPoint(man.notifier.WorldPoint);
                var p = Joystick.ToLocal(_tr, healthBars.CurrentParent, screen);
                ui.transform.localPosition = p;
            }
        }

        public struct BarRef {
            public HealthBarModule.Notifier notifier;
        }

        public class Manager {
            public static Manager Get(LevelPlayer player) {
                if(!player.TryGetExtra(c_key, out Manager manager)) {
                    manager = new Manager();
                    player.SetExtra(c_key, manager);
                }
                return manager;
            }

            public ExposedList<BarRef> healths;

            private Manager() {
                healths = new ExposedList<BarRef>();
            }

            public void Add(HealthBarModule.Notifier notifier) {
                BarRef br;
                br.notifier = notifier;
                healths.Add(br);
            }
        }

    }
}