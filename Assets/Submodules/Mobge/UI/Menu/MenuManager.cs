using Mobge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.UI
{
    public class MenuManager : MonoBehaviour
    {
        public List<MenuReference> extraMenus;


        private Stack<BaseMenu> _menuStack;
        private Action<MenuManager> _transitionEnd;
        private NextOp _nextMenu;
        private HashSet<BaseMenu> _registeredMenus;

        public BaseMenu TopMenu => _menuStack.Count == 0 ? null : _menuStack.Peek();

        protected void Awake() {
            _menuStack = new Stack<BaseMenu>();
            _registeredMenus = new HashSet<BaseMenu>();
        }
        public void EnsureRegistration(BaseMenu menu) {
            var oldMM = menu.MenuManager;
            if(oldMM != null && oldMM != this) {
                throw new Exception("One instance of " + typeof(BaseMenu) + " cannot be registered two different instances of" + typeof(MenuManager) + ".");
            }
            menu.MenuManager = this;
            _registeredMenus.Add(menu);
        }
        public bool Unregister(BaseMenu menu) {
            return _registeredMenus.Remove(menu);
        }
        private void OnDestroy() {
            if (extraMenus != null) {
                for (int i = 0; i < extraMenus.Count; i++) {
                    var ins = extraMenus[i].Instance;
                    if (ins != null) {
                        EnsureRegistration(ins);
                    }
                }
            }
            if (_registeredMenus != null) {
                var e = _registeredMenus.GetEnumerator();
                while (e.MoveNext()) {
                    if (e.Current != null) {
                        e.Current.gameObject.DestroySelf();
                    }
                }
                e.Dispose();
                _registeredMenus.Clear();
            }
        }
        public bool PushMenu(BaseMenu menu, Action<MenuManager> onTransitionEnd = null, bool animation = true) {
            var topMenu = TopMenu;
            if(topMenu != null && topMenu.CurrentState != BaseMenu.State.Open) {
                return false;
            }
            menu.MenuManager = this;
            menu.Prepare();
            if (topMenu) {
                topMenu.Interactable = false;
                topMenu.OnFocusChange(false);
            }
            _menuStack.Push(menu);
            _transitionEnd = onTransitionEnd;
            if (animation) {
                menu.SetEnabledWithAnimation(true, PushEnd);
            }
            else {
                menu.SetEnabled(true);
                PushEnd(true, null);
            }
            return true;
        }

        private void PushEnd(bool completed, object data) {
            if (completed) {
                FireTransitionEnd();
            }
        }

        public virtual LevelPlayer CurrentLevel => null;

        public bool PopMenuControlled(BaseMenu topMenu, Action<MenuManager> onTransitionEnd = null, bool animation = true) {
            var t = TopMenu;
            if (t != topMenu) {
                return false;
            }
            if(t.CurrentState != BaseMenu.State.Open) {
                return false;
            }
            _transitionEnd = onTransitionEnd;
            _nextMenu .Reset();
            if (animation) {
                topMenu.SetEnabledWithAnimation(false, EndPop);
            }
            else {
                topMenu.SetEnabled(false);
                EndPop(true, null);
            }
            return true;
        }
        public bool PopUntil(BaseMenu target, Action<MenuManager> onTransitionEnd = null, bool animation = true) {
            var t = TopMenu;
            if (t == null || t.CurrentState != BaseMenu.State.Open) {
                return false;
            }
            _transitionEnd = onTransitionEnd;
            _nextMenu.mode = NextMode.PopUntil;
            _nextMenu.target = target;
            if (animation) {
                t.SetEnabledWithAnimation(false, EndPop);
            }
            else {
                t.SetEnabled(false);
                EndPop(true, null);
            }
            return true;
        }


        public bool ReplaceTopMenu(BaseMenu menu, Action<MenuManager> onTransitionEnd = null) {
            var t = TopMenu;
            if(t == null || t.CurrentState != BaseMenu.State.Open) {
                return false;
            }
            _transitionEnd = onTransitionEnd;
            _nextMenu.mode = NextMode.Replace;
            _nextMenu.target = menu;
            t.SetEnabledWithAnimation(false, EndPop);
            return true;
        }

        private void EndPop(bool completed, object data) {
            if (completed) {
                _menuStack.Pop();
                switch (_nextMenu.mode) {
                    default:
                    case NextMode.None:
                        var topMenu = TopMenu;
                        if (topMenu) {
                            topMenu.Interactable = true;
                            topMenu.OnFocusChange(true);
                        }
                        FireTransitionEnd();
                        break;
                    case NextMode.Replace:
                        var nm = _nextMenu.target;
                        _nextMenu.Reset();
                        PushMenu(nm, _transitionEnd);
                        break;
                    case NextMode.PopUntil:
                        var tm = TopMenu;
                        if (tm) {
                            tm.Interactable = true;
                        }
                        if (tm == _nextMenu.target) {
                            FireTransitionEnd();
                            if (tm != null) {
                                tm.OnFocusChange(true);
                            }
                        }
                        else {
                            PopUntil(_nextMenu.target, _transitionEnd);
                        }
                        break;
                }
            }
        }
        private void FireTransitionEnd() {
            if (_transitionEnd != null) {
                var a = _transitionEnd;
                _transitionEnd = null;
                a(this);
            }
        }
        private struct NextOp
        {
            public NextMode mode;
            public BaseMenu target;
            public void Reset() {
                mode = NextMode.None;
                target = null;
            }
        }
        public enum NextMode
        {
            None,
            Replace,
            PopUntil
        }

        [Serializable]
        public class MenuReference {
            public BaseMenu menuRes;
            private BaseMenu _instance;
            // public List<MenuSequenceConnection> slotToSequence; 
            // [Serializable]
            // public struct MenuSequenceConnection {
            //     public int slotIndex;
            //     public int sequenceIndex;
            // }
            public void EnsureInit() {
                if (_instance == null && menuRes!=null) {
                    _instance = Instantiate(menuRes);
                }
            }
            public BaseMenu Instance => _instance;
            public BaseMenu InstanceSafe {
                get {
                    EnsureInit();
                    return _instance;
                }
            }
            public T GetTyped<T>() where T : BaseMenu {
                return (T)_instance;
            }
            public T GetTypedSafe<T>() where T : BaseMenu {
                return (T)InstanceSafe;
            }
        }
    }
}