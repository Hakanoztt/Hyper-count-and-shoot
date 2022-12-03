using Mobge.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame.UI {
    public interface IPopup {
        public string TriggerTag { get; }
        public bool ShouldOpen<T>(PopupOpener<T> opener, Collider trigger) where T : BaseMenu;
        public void OnOpen<T>(PopupOpener<T> opener) where T : BaseMenu;
        public void OnClose<T>(PopupOpener<T> opener) where T : BaseMenu;
    }
}