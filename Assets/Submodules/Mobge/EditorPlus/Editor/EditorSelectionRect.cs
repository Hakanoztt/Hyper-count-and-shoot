using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge {
    public struct EditorSelectionRect {
        private Vector2 _screenMouseStart;
        private bool _active;
        public void OnPress() {
            _screenMouseStart = Event.current.mousePosition;
            _active = true;
        }
        public void OnRelease() {
            _active = false;
        }
        public void DrawScene() {
            DrawScene(new Color(0, 0, 0, 0.5f));
        }
        public void DrawScene(Color color) {
            if (!_active) {
                return;
            }
            var r = CurrentRect;
            Handles.BeginGUI();
            EditorGUI.DrawRect(r, new Color(0.6f, 0.6f, 1, 0.1f));
            Handles.EndGUI();
        }
        public bool IsActive => _active;
        public Rect CurrentRect {
            get {
                Vector2 pos = Event.current.mousePosition;
                var min = _screenMouseStart;
                if (min.x > pos.x) {
                    float t = pos.x;
                    pos.x = min.x;
                    min.x = t;
                }
                if (min.y > pos.y) {
                    float t = pos.y;
                    pos.y = min.y;
                    min.y = t;
                }
                return new Rect(min, pos - min);
            }
        }
        public int RepaintHash() {
            return CurrentRect.GetHashCode();
        }
        public Vector2 ToScreen(Vector3 worldPoint) {
            var lsw = SceneView.lastActiveSceneView;
            Vector2 r = lsw.camera.WorldToScreenPoint(worldPoint);
            //r = GUIUtility.ScreenToGUIPoint(r);
            var scnPos = lsw.position;

            //r.x += scnPos.x;
            r.y = scnPos.height - r.y - 20; // todo: figure out where is this 20 coming from
            return r;
        }
        public bool ContainsPoint(Vector3 worldPoint) {
            if (!_active) {
                return false;
            }
            var r = CurrentRect;
            return r.Contains(ToScreen(worldPoint));
        }
    }
}