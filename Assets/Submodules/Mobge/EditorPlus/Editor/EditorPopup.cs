using System;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Mobge {
    public class EditorPopup : PopupWindowContent
    {
        private const float s_border = 7;
        private Action<LayoutRectSource, EditorPopup> _drawContent;
        private float _height;
        private Vector2 _scrollPos;
        private LayoutRectSource _rects;
        public Action Closed { get; set; }
        private Vector2 _size;
        EditorTools _tools;
        private bool _isOpen;
        public EditorPopup(Action<LayoutRectSource, EditorPopup> drawContent) {
            _drawContent = drawContent;
            _tools = new EditorTools();
            _tools.AddTool(new EditorTools.Tool("hold mouse") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0,
                },
                onPress = () => {
                    //Debug.Log(_isOpen);
                    var b = _isOpen;
                    _isOpen = false;
                    return b;
                },
            });
        }
        public void Show(Rect activatorRect) {
            Show(activatorRect, new Vector2(300,300));
        }
        public void Show(Rect activatorRect, Vector2 size) {
            //Debug.Log(activatorRect);
            if (size.y == 0) {
                size.y = DrawContent(new Rect(0, 0, 100, 0));
            }
            _size = size;
            PopupWindow.Show(activatorRect, this);
        }
        public void OnSceneGUI() {
            _tools.OnSceneGUI();
            
        }
        public override void OnClose() {
            base.OnClose();
            if(Closed != null) {
                Closed();
                Closed = null;
            }
            //_isOpen = false;
        }
        public override void OnOpen() {
            base.OnOpen();
            _isOpen = true;
        }
        public override void OnGUI(Rect rect) {
            var rin = rect;
            if (_height > rect.height) {
                rin.width -= EditorGUIUtility.singleLineHeight;
            }
            _scrollPos = GUI.BeginScrollView(rect, _scrollPos, new Rect(rect.position, new Vector2(rin.width, _height)));
            _height = DrawContent(rin);
            GUI.EndScrollView();
        }
        public float DrawContent(Rect rect) {
            if (_rects == null) {
                _rects = new LayoutRectSource();
            }
            _rects.Reset(rect, s_border);
            _drawContent(_rects, this);
            return _rects.Height;// + s_border * 2;

        }
        public void Close() {
            editorWindow.Close();
        }
        public override Vector2 GetWindowSize() {
            var b = s_border * 2f;
            return _size + new Vector2(b, b);
        }
    }
}