using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mobge {
    public class GridHelper {
        private GUILayoutOption[] _seperatorOptions;
        private List<Column> _columns = new List<Column>();
        private int _index;
        private Rect _headerRect;
        public GridHelper() {
            _seperatorOptions = new GUILayoutOption[] {
                GUILayout.Width(7)
            };
        }
        bool OpenColumnSettingsPopup() {
            var mpos = Event.current.mousePosition;
            if(!_headerRect.Contains(mpos)){
                return false;
            }
            EditorPopup p = new EditorPopup((rects, popup) => {
                for(int i = 0; i < _columns.Count; i++){
                    var c = _columns[i];
                    c.on = EditorGUI.Toggle(rects.NextRect(), c.name, c.on);
                }
            });
            p.Show(new Rect(mpos, Vector2.zero));
            return true;
        }
        public Column AddColumn(string name, float width, bool expand = false, bool hideLabel = false) {
            var r = new Column();
            r.on = true;
            r.name = name;
            r.hideLabel = hideLabel;
            r.InitOptions(width, expand);
            _columns.Add(r);
            return r;
        }
        public void DrawHeader() {
            var c = GUI.contentColor;
            if(GUI.Button(_headerRect, "")) {
                OpenColumnSettingsPopup();
            }
            EditorGUILayout.BeginHorizontal();
            for(int i = 0; i < _columns.Count; i++){
                var r = _columns[i];
                if(r.on) {
                    if(i != 0) {
                        if(r.hideLabel){
                            Seperator("");
                        }
                        else{
                            Seperator();
                        }
                    }
                    if(r.hideLabel){
                        GUILayout.Label("", r.options);
                    }
                    else{
                        GUILayout.Label(r.name, r.options);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            _index = 0;
            var rect = GUILayoutUtility.GetLastRect();
            if(rect.width>2){
                _headerRect = rect;
            }
        }
        private void Seperator(string t = "|") {
            EditorGUILayout.LabelField(t, _seperatorOptions);
        }
        public bool IsColumnVisible(string columnName, out GUILayoutOption[] options) {
            Column column;
            int start = _index;
            int cindex;
            do {
                column = _columns[_index];
                cindex = _index;
                _index++;
                if(_index == _columns.Count){
                    _index = 0;
                }
                if(_index == start){
                    throw new InvalidOperationException("There is no column with name " + column.name + ".");
                }
            }
            while(column.name != columnName);
            if(cindex != 0 && column.on) {
                Seperator();
            }
            options = column.options;
            return column.on;
        }
        public class Column {
            internal bool on;
            public GUILayoutOption[] options;
            internal string name;
            internal bool hideLabel;
            public Column SetIsOn(bool on) {
                this.on = on;
                return this;
            }

            public void InitOptions(float width, bool expand) {
                options = new GUILayoutOption[2];
                options[0] = GUILayout.Width(width);
                options[1] = GUILayout.ExpandWidth(expand);
            }
        }
    }
}