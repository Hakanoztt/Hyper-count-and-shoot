using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Mobge {
	public class EditorTools {
        private List<Tool> tools = new List<Tool>();
        private Tool _activeTool;
        private Event e => Event.current;
        public Tool ActiveTool => _activeTool;
        public Ray MouseRay => HandleUtility.GUIPointToWorldRay(e.mousePosition);
        public void AddTool(Tool t) {
            tools.Add(t);
        }
        public void OnSceneGUI() {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if (e.type == EventType.Repaint) {
                HandleEventUpdate();
                return;
            }
            HandleEventPress();
            HandleEventDrag();
            HandleEventRelease();
        }
        private void HandleEventPress() {
            if (_activeTool != null) return;
            if (e.type != EventType.KeyDown && e.type != EventType.MouseDown) return;
            for (int i = 0; i < tools.Count; i++) { var tool = tools[i];
                if (!EventSatisfiesActivationRulePress(e, tool.activation)) continue;
                if (tool.onPress == null || tool.onPress()) {
                    _activeTool = tool;
                    e.Use();
                    return;
                }
            }
        }
        private bool EventSatisfiesActivationRulePress(Event @event, ActivationRule rule) {
            if (GUIUtility.hotControl != 0) return false;
            if (rule.modifiers != (@event.modifiers & ~EventModifiers.CapsLock & ~EventModifiers.FunctionKey)) return false;
            if (rule.mouseButton >= 0 && @event.isMouse && @event.button == rule.mouseButton) return true;  
            if (rule.key != KeyCode.None && @event.isKey && @event.keyCode == rule.key) return true;
            return false;
        }
        private void HandleEventDrag() {
            if (_activeTool == null) return;
            if (e.type != EventType.MouseDrag) return;
            e.Use();
            _activeTool.onDrag?.Invoke();
        }
        private void HandleEventUpdate() {
            if (_activeTool == null) return;
            if (_activeTool.onUpdate == null) return;
            _activeTool.onUpdate();
            EditorWindow.focusedWindow.Repaint();
        }
        private void HandleEventRelease() {
            if (_activeTool == null) return;
            if (!EventSatisfiesActivationRuleRelease(e, _activeTool.activation)) return;
            e.Use();
            _activeTool.onRelease?.Invoke();
            _activeTool = null;
        }
        private bool EventSatisfiesActivationRuleRelease(Event @event, ActivationRule rule) {
            // Mouse leaving the window triggers release because else after mouse has left the current window;
            // up condition does not exist therefore is never true; release does not get called
            if (rule.mouseButton >= 0 
                && (@event.type == EventType.MouseUp || @event.type == EventType.MouseLeaveWindow) 
                && rule.mouseButton == @event.button) {
                return true;
            }
            if (rule.key != KeyCode.None 
                && @event.type == EventType.KeyUp 
                && rule.key == @event.keyCode) {
                return true;
            }
            return false;
        }
        public class Tool {
            public string name;
            public ActivationRule activation;
            public Func<bool> onPress;
            public Action onDrag;
            public Action onRelease;
            public Action onUpdate;
            public Tool(string name) {
                this.name = name;
            }
        }
        public class ActivationRule {
            public EventModifiers modifiers = EventModifiers.None;
            public int mouseButton = -1;
            public KeyCode key = KeyCode.None;
        }
    }
}