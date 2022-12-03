using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mobge {

    public class EditorFoldGroups {
        
        private Group _oldRoot;
        private Group _root;
        private FilterMode _filterMode;
        private GUIStyle _style;
        private string _styleName;
        private Group _currentGroup;
        private string _filter;
        public string CurrentGroupName => _currentGroup == null ? null : _currentGroup.ToString();
        public void GuilayoutField(Action<Group> createGroupsIfNecessary, string label=null) {
            if (_root == null || !_root.HasChildren) {
                _root = new Group(this, "root");
                _currentGroup = _oldRoot;
                createGroupsIfNecessary(_root);
                _currentGroup = null;
                //if(_oldRoot!=null){
                //    _root.CopyFrom(_oldRoot);
                //    _oldRoot = null;
                //}
            }
            if(_style == null) {
                _style = _styleName;
            }
            switch(_filterMode) {
                case FilterMode.NoFilter:
                    if(!string.IsNullOrEmpty(label)){
                        UnityEditor.EditorGUILayout.LabelField(label);
                    }
                    _root.DrawContent(_style);
                    break;
                case FilterMode.HasFilter:
                    _filter = EditorGUILayout.TextField(label, _filter);
                    _root.DrawContent(_style, _filter);
                    break;
                case FilterMode.ShowOnlyIfFilter:
                    _filter = EditorGUILayout.TextField(label, _filter);
                    if(!string.IsNullOrEmpty(_filter)){
                        _root.DrawContent(_style, _filter);
                    }
                    break;
            }
        }
        public EditorFoldGroups(FilterMode filterMode) : this(filterMode, "Box") {
        }
        public EditorFoldGroups(FilterMode filterMode, string styleName) {
            this._filterMode = filterMode;
            this._styleName = styleName;
        }
        private void EnsureCurrentGroup() {
            if(_currentGroup == null) {
                throw new Exception("This operation can only be made inside field callback of " + nameof(Group.AddChild) + " method of class " + typeof(Group));
            }
        }
        public string StringField(string name, string defaultValue = null)
        {
            EnsureCurrentGroup();
            var o = _currentGroup.GetObject(name, defaultValue);
            o = EditorGUILayout.TextField(name, o);
            _currentGroup.SetObject(name, o);
            return o;
        }
        public T ObjectField<T>(string name) where T : UnityEngine.Object {
            EnsureCurrentGroup();
            var o = _currentGroup.GetObject<T>(name, null);
            o = EditorLayoutDrawer.ObjectField(name, o);
            _currentGroup.SetObject(name, o);
            return o;
        }
        public bool ToggleField(string name, bool defaultValue = false) {
            EnsureCurrentGroup();
            var o = _currentGroup.GetObject<bool>(name, defaultValue);
            o = EditorGUILayout.Toggle(name, o);
            _currentGroup.SetObject(name, o);
            return o;
        }
        public float FloatField(string name, float defaultValue = 0) {
            EnsureCurrentGroup();
            var o = _currentGroup.GetObject<float>(name, defaultValue);
            o = EditorGUILayout.FloatField(name, o);
            _currentGroup.SetObject(name, o);
            return o;
        }
        public int IntField(string name, int defaultValue = 0) {
            EnsureCurrentGroup();
            var o = _currentGroup.GetObject<int>(name, defaultValue);
            o = EditorGUILayout.IntField(name, o);
            _currentGroup.SetObject(name, o);
            return o;
        }
        public Vector3 Vector3Field(string name, Vector3 defaultValue) {
            EnsureCurrentGroup();
            var o = _currentGroup.GetObject<Vector3>(name, defaultValue);
            o = EditorGUILayout.Vector3Field(name, o);
            _currentGroup.SetObject(name, o);
            return o;
        }

        public void Refresh()
        {
            _oldRoot = _root;
            _root = null;
        }

        public T GetObject<T>(string name, T defaultValue) {
            if (_currentGroup == null) return defaultValue;
            return _currentGroup.GetObject<T>(name, defaultValue);
        }
        public T GetClassEnsured<T>(string name) where T : class, new() {
            return _currentGroup.GetClassEnsured<T>(name);
        }
        public void SetObject(string name, object o) {
            _currentGroup.SetObject(name, o);
        }
        public class Group {
            private bool _open;
            private Action _action;
            private Dictionary<string, Group> _children;
            private Dictionary<string, object> _state;
            private EditorFoldGroups _groups;
            private string _name;
            public override string ToString() {
                return _name;
            }
            internal Group(EditorFoldGroups groups, string name) {
                _groups = groups;
                _name = name;
            }
            private Dictionary<string, Group> EnsuredChildren {
                get {
                    if (_children == null) {
                        _children = new Dictionary<string, Group>();
                    }
                    return _children;
                }
            }
            internal void CopyFrom(Group group) {
                _open = group._open;
                _state = group._state;
                if(_children!=null){
                    foreach(var p in _children) {
                        Group c;
                        if(group._children.TryGetValue(p.Key, out c)){
                            p.Value.CopyFrom(c);
                        }
                    }
                }
            }
            internal T GetClassEnsured<T>(string name) where T : class, new() {
                object o;
                if(_state==null || !_state.TryGetValue(name, out o)) {
                    o = new T();
                    SetObject(name, o);
                }
                return (T)o;
            }
            internal T GetObject<T>(string name, T defaultValue){
                object o;
                if(_state == null || !_state.TryGetValue(name, out o)){
                    return defaultValue;
                }
                return (T)o;
            }
            internal void SetObject(string name, object o){
                if(_state == null) {
                    _state = new Dictionary<string, object>();
                }
                _state[name] = o;
            }
            public bool HasChildren { get { return _children != null; } }

            public void AddChild(string name, Action field, Action<Group> addSubChilds = null) {
                Group child = new Group(_groups, name);
                child._action = field;
                EnsuredChildren.Add(name, child);
                var old = _groups._currentGroup;

                if (old != null) {
                    _state = _groups._currentGroup._state;
                    _open = _groups._currentGroup._open;
                    if (old._children != null && old._children.TryGetValue(name, out Group oc)) {
                        _groups._currentGroup = oc;
                    }
                }

                if (addSubChilds != null) {
                    addSubChilds(child);
                }
                _groups._currentGroup = old;
            }
            public void AppendField(Action field) {
                _action += field;
            }
            public Group AddOrGetChild(string name) {
                var c = EnsuredChildren;
                Group g;
                if(!c.TryGetValue(name, out g)){
                    g = new Group(_groups, name);
                    c.Add(name, g);
                }
                return g;
            }
            internal void DrawContent(GUIStyle style, string filter = null) {
                EditorGUI.indentLevel++;
                if (_action != null) {
                    _groups._currentGroup = this;
                    _action();
                    _groups._currentGroup = null;
                }
                if (_children != null) {
                    foreach(var pair in _children) {
                        if(string.IsNullOrEmpty(filter) || InspectorExtensions.TextMatchesSearch(pair.Key, filter)) {
                            pair.Value.Draw(_groups, pair.Key, style);
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
            internal void Draw(EditorFoldGroups groups, string name, GUIStyle style) {
                bool open = _open;
                if (open) {
                    EditorGUILayout.BeginVertical(style);
                }
                _open = EditorGUILayout.Foldout(_open, name, true);
                if (_open) {
                    DrawContent(style);
                }

                if (open) {
                    EditorGUILayout.EndVertical();
                }
            }
        }
        public enum FilterMode {
            NoFilter = 0,
            HasFilter = 1,
            ShowOnlyIfFilter = 2,
        }
    }
}