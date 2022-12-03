using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.UI {
    public class DelayedFadeInExtension : MonoBehaviour, BaseMenu.IExtension {
        public float delay = 2f;
        public float fadeDuration = 0.25f;
        [OwnComponent(true)] public CanvasGroup group;
        private BaseMenu _menu;

        void BaseMenu.IExtension.Prepare(BaseMenu menu) {
            _menu = menu;
            group.alpha = 0;
            group.interactable = false;
            menu.ActionManager.DoAction(FadeIn, delay, null);
        }

        private void FadeIn(bool completed, object data) {
            if (completed) {
                _menu.ActionManager.DoAction(FadeInEnd, fadeDuration, FadeUpdate);
            }
        }

        private void FadeUpdate(float progress, object data) {
            group.alpha = progress;
        }


        private void FadeInEnd(bool completed, object data) {
            if (completed) {
                group.interactable = true;
            }
        }
    }
}
