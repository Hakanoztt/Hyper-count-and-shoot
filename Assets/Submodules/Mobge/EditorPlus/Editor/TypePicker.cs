using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Mobge.InspectorExtensions;

namespace Mobge {
    /// <summary>
    /// This TypePicker class provides a method to draw a button with given label.
    /// Use this class to get user supplied Type (User chooses the Type from all loaded assemblies).
    /// Other classes are helper classes for this class, thus should not be used directly. 
    /// </summary>
    public class TypePicker {
        private TypePickerPopup _window;
        private Type _returnType;
        private bool _popupOpened;
        
        /// <summary>
        /// This method draws a button with given label. When the button is pressed, spawns a popup window with search
        /// functionality. Keywords can be partial and case insensitive.
        /// Multiple keywords can be supplied with space in between.
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public Type Field(string label) {
            if (GUILayout.Button(label)) {
                _window = EditorWindow.GetWindow<TypePickerPopup>();
                _popupOpened = true;
            }

            // User has picked a type from popup? If did, return the chosen type
            if (_popupOpened && _window.HasClosed) {
                _returnType = _window.ReturnType;
                _popupOpened = false;
            }
            return _returnType;
        }
    }

    #region Intenally Used Classes
    /// <summary>
    /// This class collects types from all loaded assemblies and draws buttons for filtered types.
    /// </summary>
    internal class TypePickerButtonDrawer {
        private Dictionary<string, Type> _allTypes;
        private Dictionary<string, Type> _filteredTypes;
        private string _internallyCachedKeywords;

        /// <summary>
        /// Collect all types from all assemblies
        /// </summary>
        /// <remarks>It may be good idea to filter out dynamic types in here. I have not run into problems with my test cases.
        /// But if dynamic types causes problems, this is where it should be filtered out.</remarks>
        public TypePickerButtonDrawer() {
            _allTypes = new Dictionary<string, Type>();
            _filteredTypes = new Dictionary<string, Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (var type in assembly.GetTypes()) {
                    var typeName = type.ToString();
                    if (!_allTypes.ContainsKey(typeName))
                        _allTypes.Add(typeName, type);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keywords">Keywords(Types) to search for inside all assemblies</param>
        /// <returns></returns>
        internal Type SearchTypesAndDrawButtons(string keywords) {
            // Early exit, do not attempt to draw unfiltered types
            if (string.IsNullOrEmpty(keywords))
                return null;

            // If cache miss occured, rebuild cache
            if (_internallyCachedKeywords != keywords) {
                _internallyCachedKeywords = keywords;
                InvalidateCache();
            }

            // Draw filtered types pickers (buttons)
            using (var en = _filteredTypes.GetEnumerator()) {
                while (en.MoveNext()) {
                    var kp = en.Current;
                    if (GUILayout.Button(kp.Key)) {
                        return kp.Value;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Rebuilds filtered type cache
        /// </summary>
        private void InvalidateCache() {
            _filteredTypes.Clear();
            foreach (var key in _allTypes.Keys) {
                if (TextMatchesSearch(key, _internallyCachedKeywords))
                    _filteredTypes.Add(key, _allTypes[key]);
            }
        }
    }

    /// <summary>
    /// This class spawns popup for the TypePicker. Popup window contains a field to enter search keywords.
    /// After user supplies keywords, any found type is drawn inside the popup as a button
    /// </summary>
    internal class TypePickerPopup : EditorWindow {
        public bool HasClosed { get; private set; }
        public Type ReturnType { get; private set; }

        private TypePickerPopup _popup;
        private string _keywords;
        private TypePickerButtonDrawer _buttonDrawer;
        private Vector2 _scroll;

        private void OnEnable() {
            _buttonDrawer = new TypePickerButtonDrawer();
            _popup = GetWindow<TypePickerPopup>();
            Texture2D pickerIcon = EditorGUIUtility.FindTexture("d_Search Icon");
            _popup.titleContent.image = pickerIcon;
            _popup.titleContent.text = "Type Picker";
            _popup.ShowPopup();
        }

        private void OnGUI() {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (string.IsNullOrEmpty(_keywords)) {
                EditorGUILayout.HelpBox("Search keywords are case insensetive.", MessageType.Info);
                EditorGUILayout.HelpBox(
                    "Multiple keywords seperated by space can be given at the same time." +
                    "\nExample : \"mobge animation\"", MessageType.Info);
            }
            _keywords = EditorGUILayout.DelayedTextField("Search keywords", _keywords);

            ReturnType = _buttonDrawer.SearchTypesAndDrawButtons(_keywords);
            if (ReturnType != null) {
                HasClosed = true;
                _popup.Close();
            }
            EditorGUILayout.EndScrollView();
        }
    }
    
    #endregion
}