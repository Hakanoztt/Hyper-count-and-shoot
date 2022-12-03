using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.IO;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;

namespace Mobge {
    public abstract class BaseEditorDrawer
    {
        protected static List<Pair> s_tempPairs = new List<Pair>();


        public static IEnumerable<Pair> FilterList<T>(IList list, Predicate<T> predicate) where T : class {
            if (list != null) {
                for (int i = 0; i < list.Count; i++) {
                    if (predicate(list[i] as T)) {
                        yield return new Pair(i, list[i]);
                    }
                }
            }
        }

        protected static string[] GetNames(List<Pair> pairs, bool includeIds) {
            string[] ss = new string[pairs.Count];
            for (int i = 0; i < pairs.Count; i++) {
                ss[i] = "" + pairs[i].o;
                if (includeIds) {
                    ss[i] = i + " " + ss[i];
                }
            }
            return ss;
        }
        protected static int IndexOf(List<Pair> pairs, int index) {
            for (int i = 0; i < pairs.Count; i++) {
                if (pairs[i].index == index) {
                    return i;
                }
            }
            return -1;
        }
        public struct Pair
        {
            public Pair(int index, object o) {
                this.index = index;
                this.o = o;
            }
            public int index;
            public object o;
        }
        protected static void PrepareTempPairList(string noneOptionName) {
            s_tempPairs.Clear();
            if (!string.IsNullOrEmpty(noneOptionName)) {
                s_tempPairs.Add(new Pair(-1, noneOptionName));
            }
        }
        protected static void FillTempPairList(IList content, string noneOptionName) {
            PrepareTempPairList(noneOptionName);
            for (int i = 0; i < content.Count; i++) {
                s_tempPairs.Add(new Pair(i, content[i]));
            }
        }

        protected static void FillTempPairList<T>(AutoIndexedMap<T> content, string noneOptionName, Func<T, string> toString = null) {
            PrepareTempPairList(noneOptionName);
            var e = content.GetKeyEnumerator();
            while (e.MoveNext()) {
                var c = e.Current;
                object o;
                if (toString == null) {
                    o = content[c];
                }
                else {
                    o = toString(content[c]);
                }
                s_tempPairs.Add(new Pair(c, o));
            }
        }
        protected struct PopupData
        {
            public string[] names;
            public int popupIndex;
            public int selectedIndex;
        }
        protected static PopupData PrePopup(List<Pair> pairs, int selectedIndex, bool includeIds) {
            PopupData pd;
            pd.popupIndex = IndexOf(pairs, selectedIndex);
            pd.names = GetNames(pairs, includeIds);
            pd.selectedIndex = selectedIndex;
            return pd;
        }
        protected static int PreConvert<K, T>(IList<K> content, T element, Func<K, T> p, out IList cont, string noneOption = null) {
            int index = -1;
            cont = new ListToNonGeneric<K>(content);
            for (int i = 0; i < content.Count; i++) {
                if (p(content[i]).Equals(element)) {
                    index = i;
                }
            }
            FillTempPairList(cont, noneOption);
            EditorGUI.BeginChangeCheck();
            return index;
        }
        protected static T PostConvert<K, T>(IList<K> content, int index, T element, Func<K, T> p, out K selected) {
            bool changed = EditorGUI.EndChangeCheck();
            T result;
            if (index < 0 || index >= content.Count) {
                selected = default(K);
                result = default(T);
            }
            else {
                selected = content[index];
                result = p(content[index]);
            }
            return changed ? result : element;
        }
        protected static int PostPopup(List<Pair> pairs, PopupData data) {
            if (data.popupIndex >= 0 && data.popupIndex < pairs.Count) {
                return pairs[data.popupIndex].index;
            }
            return data.selectedIndex;
        }
        private static void CustomCollectionField<T>(string label, ReorderableListHelper.ListAttributes list, Func<LayoutRectSource, T, T> customField, ref int selectedRow, bool useLayout, LayoutRectSource position, bool useCustomHeight = true) {
            CustomCollectionField<T>(label, list, (l, index) => {
                var t = (T)list.GetCurrentArray()[index];
                list.GetCurrentArray()[index] = customField(l, t);
            }, ref selectedRow, useLayout, position, useCustomHeight);
        }
        private static void CustomCollectionField<T>(string label, ReorderableListHelper.ListAttributes list, Action<LayoutRectSource, int> customField, ref int selectedRow, bool useLayout, LayoutRectSource position, bool useCustomHeight = true) {

            int row = selectedRow;
            list.displayName = label;
            list.list.drawElementCallback = (rect, index, isActive, isFocused) => {
                var layoutRects = list.layoutRects;
                layoutRects.Reset(rect);
                bool selected = row == index;
                var element = list.GetCurrentArray()[index];
                string displayElement;
                if (list.constantElements != null && list.constantElements.Length > index) {
                    displayElement = list.constantElements[index];
                }
                else {
                    displayElement = element.ToString();

                }
                bool newSelected;
                if (row != int.MaxValue) {
                    var r = layoutRects.NextRect();
                    r.xMin += 10;
                    newSelected = EditorGUI.Foldout(r, selected, displayElement, true);
                    if (newSelected != selected) {
                        if (newSelected) {
                            row = index;
                        }
                        else {
                            row = -1;
                        }
                    }

                }
                else {
                    //EditorGUI.LabelField(layoutRects.NextRect(), displayElement);
                    newSelected = true;
                }
                if (newSelected) {
                    customField(layoutRects, index);
                }
                if (useCustomHeight) {
                    list.SetHeight(index, layoutRects.Height);
                }
                //list.GetCurrentArray()[index] = element;
            };
            if (useLayout) {
                list.list.DoLayoutList();
            }
            else {
                list.list.DoList(position.NextRect(list.list.GetHeight()));
            }
            selectedRow = row;
        }
        protected static ReorderableListHelper.ListAttributes CustomListField<T>(string label, IList list, Func<LayoutRectSource, T, T> customField, ref int selectedRow, bool useLayout, LayoutRectSource position) {

            var l = ReorderableListHelper.GetListForObject<T>(list);
            CustomCollectionField(label, l, customField, ref selectedRow, useLayout, position);
            return l;
        }
        protected static ReorderableListHelper.ListAttributes CustomArrayField<T>(string label, ref T[] array, Func<LayoutRectSource, T, T> customField, ref int selectedRow, bool useLayout, LayoutRectSource position, bool useCustomHeight = true) {
            var a = array;
            var ra = CustomArrayField<T>(label, ref a, (l2, index) => {
                a[index] = customField(l2, a[index]);
            }, ref selectedRow, useLayout, position, useCustomHeight);

            array = a;
            return ra;
        }
        protected static ReorderableListHelper.ListAttributes CustomArrayField<T>(string label, ref T[] array, Action<LayoutRectSource, int> customField, ref int selectedRow, bool useLayout, LayoutRectSource position, bool useCustomHeight = true) {
            if (array == null) {
                array = new T[0];
            }
            var list = ReorderableListHelper.GetListForObject<T>(array, useCustomHeight);
            CustomCollectionField<T>(label, list, customField, ref selectedRow, useLayout, position, useCustomHeight);
            var aa = list.GetCurrentArray();
            ReorderableListHelper.UpdateKey(ref array, aa);
            return list;
        }
        protected static ReorderableListHelper.ListAttributes CustomListField(SerializedProperty property, Action<LayoutRectSource, SerializedProperty, int> customField, bool useLayout, LayoutRectSource position) {
            var list = ReorderableListHelper.GetListForProperty(property);
            list.displayName = property.displayName;
            list.list.serializedProperty = property;
            list.list.drawElementCallback = (rect, index, isActive, isFocused) => {
                var layoutRects = list.layoutRects;
                layoutRects.Reset(rect);
                var element = property.GetArrayElementAtIndex(index);

                string displayElement;
                if (list.constantElements != null && list.constantElements.Length > index) {
                    displayElement = list.constantElements[index];
                }
                else {
                    displayElement = element.displayName;
                }
                element.isExpanded = EditorGUI.Foldout(layoutRects.NextRect(), element.isExpanded, displayElement);
                if (element.isExpanded) {
                    customField(layoutRects, element, index);
                }
                list.SetHeight(index, layoutRects.Height);
            };
            if (useLayout) {
                list.list.DoLayoutList();
            }
            else {
                list.list.DoList(position.NextRect(list.list.GetHeight()));
            }
            return list;
        }
        public static class ReorderableListHelper
        {
            private static PropertyDescriptor _tempKey = new PropertyDescriptor();
            private static Dictionary<object, ListAttributes> _reorderableLists = new Dictionary<object, ListAttributes>();
            internal static ListAttributes GetListForObject<T>(IList array, bool useCustomHeight = true) {
                ListAttributes list;
                if (!_reorderableLists.TryGetValue(array, out list)) {
                    list = NewReorderableList<T>(array);
                    _reorderableLists.Add(array, list);
                    if (useCustomHeight) {
                        list.list.elementHeightCallback = list.GetHeight;
                    }
                    list.list.drawHeaderCallback = list.DrawHeader;
                }
                return list.Prepare();
            }
            internal static ListAttributes GetListForProperty(SerializedProperty property) {
                _tempKey.SetProperty(property);
                ListAttributes list;
                if (!_reorderableLists.TryGetValue(_tempKey, out list) || list.list.serializedProperty.serializedObject != property.serializedObject) {
                    list = NewReorderableList(property);
                    _reorderableLists[new PropertyDescriptor(property)] = list;
                    list.list.elementHeightCallback = list.GetHeight;
                    list.list.drawHeaderCallback = list.DrawHeader;
                }
                return list.Prepare();
            }
            private static ListAttributes NewReorderableList<T>(IList list) {
                var p = new ListAttributes();
                var array = list as T[];
                if (array != null) {
                    ArrayToList<T> c = new ArrayToList<T>(array);
                    p.list = new ReorderableList(c, typeof(T));
                    p.GetCurrentArray = c.GetCurrentArray;
                }
                else {
                    p.list = new ReorderableList(list, typeof(T));
                    p.GetCurrentArray = () => list;
                }
                p.list.onReorderCallbackWithDetails = (l, o, i) => {
                    GUI.changed = true;
                };
                p.elementType = typeof(T);
                return p;
            }
            private static ListAttributes NewReorderableList(SerializedProperty property) {
                var p = new ListAttributes();
                p.list = new ReorderableList(property.serializedObject, property);
                return p;
            }
            internal static void UpdateKey<T>(ref T oldKey, object newKey) where T : class {
                if (newKey != oldKey) {
                    var p = _reorderableLists[oldKey];
                    _reorderableLists.Remove(oldKey);
                    oldKey = (T)newKey;
                    _reorderableLists[newKey] = p;
                }
            }
            public class ListAttributes
            {
                public ReorderableList list;
                internal Type elementType;
                public Func<IList> GetCurrentArray;
                public string displayName;
                public string[] constantElements;
                public bool ignoreConstantElementCounts;
                internal ListAttributes() {
                }
                internal LayoutRectSource layoutRects = new LayoutRectSource();
                internal ListAttributes Prepare() {
                    int fixedCount;
                    if (constantElements != null && constantElements.Length > 0 && !ignoreConstantElementCounts) {
                        fixedCount = constantElements.Length;
                        list.displayAdd = false;
                        list.displayRemove = false;
                        if (list.serializedProperty != null) {
                            list.serializedProperty.arraySize = fixedCount;
                        }
                        else {
                            var l = list.list;
                            while (l.Count > fixedCount) {
                                l.RemoveAt(l.Count - 1);
                            }
                            while (l.Count < fixedCount) {
                                l.Add(Activator.CreateInstance(elementType));
                            }
                        }
                    }
                    else {
                        fixedCount = -1;
                    }
                    return this;
                }
                private List<float> heights = new List<float>();

                private void EnsureIndex(int index) {
                    while (index >= heights.Count) {
                        heights.Add(EditorGUIUtility.singleLineHeight);
                    }
                }
                public void SetHeight(int index, float value) {
                    EnsureIndex(index);
                    heights[index] = value;
                }
                public float GetHeight(int index) {
                    list.drawElementCallback(new Rect(-1000, -1000, 10, 10), index, false, false);
                    EnsureIndex(index);
                    return heights[index];
                }
                internal void DrawHeader(Rect rect) {
                    EditorGUI.LabelField(rect, displayName);
                }
            }
        }
    }
    public sealed class EditorDrawer : BaseEditorDrawer {
        private EditorDrawer() {

        }
        public static ReorderableListHelper.ListAttributes CustomArrayField<T>(LayoutRectSource rectSource, string label, ref T[] array, Action<LayoutRectSource, int> customField, ref int selectedRow) {
            return CustomArrayField(label, ref array, customField, ref selectedRow, false, rectSource);
        }
        public static ReorderableListHelper.ListAttributes CustomArrayField<T>(LayoutRectSource rectSource, string label, ref T[] array, Action<LayoutRectSource, int> customField) {
            int s = int.MaxValue;
            return CustomArrayField<T>(rectSource, label, ref array, customField, ref s);
        }
        public static ReorderableListHelper.ListAttributes CustomArrayField<T>(LayoutRectSource rectSource, string label, ref T[] array, Func<LayoutRectSource, T, T> customField, ref int selectedRow) {
            return CustomArrayField(label, ref array, customField, ref selectedRow, false, rectSource);
        }
        public static ReorderableListHelper.ListAttributes CustomArrayField<T>(LayoutRectSource rectSource, string label, ref T[] array, Func<LayoutRectSource, T, T> customField) {
            int selectedRow = int.MaxValue;
            return CustomArrayField(label, ref array, customField, ref selectedRow, false, rectSource);
        }
        public static ReorderableListHelper.ListAttributes CustomListField(LayoutRectSource rectSource, SerializedProperty property, Action<LayoutRectSource, SerializedProperty, int> customField) {
            return CustomListField(property, customField, false, rectSource);
        }
        public static ReorderableListHelper.ListAttributes CustomListField<T>(LayoutRectSource rectSource, string label, List<T> list, Func<LayoutRectSource, T, T> customField, ref int selectedRow) {
            return CustomListField<T>(label, list, customField, ref selectedRow, false, rectSource);
        }
        
        public static void CustomListField<T>(LayoutRectSource rectSource, string label, AutoIndexedMap<T> list, Action<LayoutRectSource, int> fieldForKey, ref bool isOpen){
            
            isOpen = EditorGUI.Foldout(rectSource.NextRect(), isOpen, label, true);
            if(isOpen) {
                float buttonWidth = 30;
                EditorGUI.indentLevel++;
                var e = list.GetKeyEnumerator();
                var rr = rectSource.CurrentRect;
                rr.width -= buttonWidth;
                rectSource.CurrentRect = rr;
                while(e.MoveNext()) {
                    var key = e.Current;
                    fieldForKey(rectSource, key);
                    rr = rectSource.CurrentRect;
                    rr.y -= EditorGUIUtility.singleLineHeight;
                    rr.height = EditorGUIUtility.singleLineHeight;
                    rr.x += rr.width;
                    rr.width = buttonWidth;
                    if(GUI.Button(rr,"-")) {
                        list.RemoveElement(key);
                    }
                }
                
                rr = rectSource.CurrentRect;
                rr.width += buttonWidth;
                rectSource.CurrentRect = rr;
                EditorGUI.indentLevel--;
                var r = rectSource.NextRect();
                r.x += r.width - buttonWidth;
                r.width = buttonWidth;
                if(GUI.Button(r, "+")) {
                    var type = typeof(T);
                    if(typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)) || !type.IsClass) {
                        list.AddElement(default(T));
                    }
                    else {
                        var i = (T)Activator.CreateInstance(type);
                        list.AddElement(i);
                    }
                }
            }
        }
        public static int Popup(LayoutRectSource rectSource, string label, IList content, int selectedIndex, string noneOptionName = null) {
            FillTempPairList(content, noneOptionName);
            return InternalListPopup(rectSource.NextRect(), label, s_tempPairs, selectedIndex);
        }
        public static int Popup(Rect rect, string label, IList content, int selectedIndex, string noneOptionName = null, bool includeIndexes = false) {
            FillTempPairList(content, noneOptionName);
            return InternalListPopup(rect, label, s_tempPairs, selectedIndex, includeIndexes);
        }
        public static int Popup<T>(LayoutRectSource rectSource, string label, AutoIndexedMap<T> map, int selectedIndex, string noneOptionName = null) {
            FillTempPairList(map, noneOptionName);
            return InternalListPopup(rectSource.NextRect(), label, s_tempPairs, selectedIndex);
        }
        public static int Popup(Rect rect, string label, IEnumerable<Pair> pairs, int selectedId, string noneOptionName = null) {
            PrepareTempPairList(noneOptionName);
            s_tempPairs.AddRange(pairs);
            return InternalListPopup(rect, label, s_tempPairs, selectedId);
        }
        public static int Popup<T>(Rect rect, string label, AutoIndexedMap<T> map, int selectedIndex, string noneOptionName = null, Func<T, string> toString = null) {
            FillTempPairList(map, noneOptionName, toString);
            return InternalListPopup(rect, label, s_tempPairs, selectedIndex);
        }
        public static T Popup<T, K>(Rect rect, string label, IList<K> content, T element, Func<K, T> convert, out K selected, string noneOption = null) {
            IList cont;
            int index = PreConvert(content, element, convert, out cont, noneOption);
            index = InternalListPopup(rect, label, s_tempPairs, index);
            return PostConvert(content, index, element, convert, out selected);
        }
        public static T Popup<T, K>(Rect rect, string label, IList<K> content, T element, Func<K, T> convert, string noneOption = null) {
            K selected;
            return Popup(rect, label, content, element, convert, out selected, noneOption);
        }
        private static int InternalListPopup(Rect rect, string label, List<Pair> pairs, int selectedIndex, bool includeIds = false) {
            var pd = PrePopup(pairs, selectedIndex, includeIds);
            if(label == null) {
                pd.popupIndex = EditorGUI.Popup(rect, pd.popupIndex, pd.names);
            }
            else{
                pd.popupIndex = EditorGUI.Popup(rect, label, pd.popupIndex, pd.names);
            }
            return PostPopup(pairs, pd);
        }
        public static T ObjectField<T>(LayoutRectSource rectSource, string label, T obj, bool allowSceneObjects = true) where T : class {
            return EditorGUI.ObjectField(rectSource.NextRect(), label, obj as UnityEngine.Object, typeof(T), allowSceneObjects) as T;
        }
        public static T ObjectField<T>(Rect rect, string label, T obj, bool allowSceneObjects = true) where T : class {
            return EditorGUI.ObjectField(rect, label, obj as UnityEngine.Object, typeof(T), allowSceneObjects) as T;
        }
    }
    
    public sealed class EditorLayoutDrawer : BaseEditorDrawer {
        static LayoutRectSource _rectSource = new LayoutRectSource();
        private EditorLayoutDrawer() {
            
        }


        public static ReorderableListHelper.ListAttributes CustomArrayField<T>(string label, ref T[] array, Action<LayoutRectSource, int> customField, ref int selectedRow) {
            return CustomArrayField(label, ref array, customField, ref selectedRow, true, null);
        }
        public static ReorderableListHelper.ListAttributes CustomArrayField<T>(string label, ref T[] array, Action<LayoutRectSource, int> customField) {
            int s = int.MaxValue;
            return CustomArrayField<T>(label, ref array, customField, ref s);
        }

        public static ReorderableListHelper.ListAttributes CustomArrayField<T>(string label, ref T[] array, Func<LayoutRectSource, T, T> customField, ref int selectedRow) {
            return CustomArrayField(label, ref array, customField, ref selectedRow, true, null);
        }
        public static ReorderableListHelper.ListAttributes CustomArrayField<T>(string label, ref T[] array,  Func<LayoutRectSource, T, T> customField, bool useCustomHeight = true) {
            var s = int.MaxValue;
            return CustomArrayField(label, ref array, customField, ref s, true, null, useCustomHeight);
        }
        public static ReorderableListHelper.ListAttributes CustomListField<T>(string label, IList list,  Func<LayoutRectSource, T, T> customField, ref int selectedRow) {
            return CustomListField(label, list, customField, ref  selectedRow, true, null);
        }
        
        public static void CustomListField<T>(string label, AutoIndexedMap<T> list, Action<LayoutRectSource, int> fieldForKey, ref bool isOpen) {
            var r = GUILayoutUtility.GetAspectRect(float.MaxValue);
            r.y+=r.height;
            r.height = 0;
            _rectSource.Reset(r);
            EditorDrawer.CustomListField(_rectSource, label, list, fieldForKey, ref isOpen);
            _rectSource.ConvertToLayout();
        }
        public static ReorderableListHelper.ListAttributes CustomListField<T>(string label, IList list,  Func<LayoutRectSource, T, T> customField) {
            int selectedRow = int.MaxValue;
            return CustomListField(label, list, customField, ref  selectedRow, true, null);
        }
        public static ReorderableListHelper.ListAttributes CustomCollectionField(SerializedProperty property, Action<LayoutRectSource, SerializedProperty, int> customField) {
            return CustomListField(property, customField, true, null);
        }
        public static int Popup(string label, IList content, int selectedIndex, string noneOptionName = null) {
            FillTempPairList(content, noneOptionName);
            return InternalListPopup(label, s_tempPairs, selectedIndex);
        }
        
        public static int Popup(string label, IEnumerable<Pair> pairs, int selectedId, string noneOptionName = null) {
            PrepareTempPairList(noneOptionName);
            s_tempPairs.AddRange(pairs);
            return InternalListPopup(label, s_tempPairs, selectedId);
        }
        public static int Popup<T>(string label, AutoIndexedMap<T> map, int selectedIndex, string noneOptionName = null, Func<T, string> toString = null) {
            FillTempPairList(map, noneOptionName, toString);
            return InternalListPopup(label, s_tempPairs, selectedIndex);
        }
        public static T Popup<T, K>(string label, IList<K> content, T element, Func<K, T> convert, out K selected) {
            IList cont;
            int index = PreConvert(content, element, convert, out cont);
            index = InternalListPopup(label, s_tempPairs, index);
            return PostConvert(content, index, element, convert, out selected);
        }
        public static T Popup<T, K>(string label, IList<K> content, T element, Func<K, T> convert) {
            K s;
            return Popup(label, content, element, convert, out s);
        }
        private static int InternalListPopup(string label, List<Pair> pairs, int selectedIndex, bool includeIds = false) {
            var pd = PrePopup(pairs, selectedIndex, includeIds);
            pd.popupIndex = EditorGUILayout.Popup(label, pd.popupIndex, pd.names);
            return PostPopup(pairs, pd);
        }
        public static T ObjectField<T>(string label, T obj, bool allowSceneObjects = true, params GUILayoutOption[] options) where T : class {
            return EditorGUILayout.ObjectField(label, obj as UnityEngine.Object, typeof(T), allowSceneObjects, options) as T;
        }
        public static T ObjectField<T>(T obj, bool allowSceneObjects = true, params GUILayoutOption[] options) where T : class {
            return EditorGUILayout.ObjectField(obj as UnityEngine.Object, typeof(T), allowSceneObjects, options) as T;
        }
        
    }
    /// <summary>
    /// Supplies Rect for Unity GUI functions that requires Rect as location.
    /// </summary>
    public class LayoutRectSource {
        private Rect _sourceRect;
        private float startY;
        private Rect _lastRect;
        private float _border;
        private float _indent;

        public float Width => _sourceRect.width;

        public LayoutRectSource() {
        }
        public LayoutRectSource(Rect initialRect, float border = 0) {
            Reset(initialRect, border);
        }
        public void Reset(Rect initialRect, float border = 0){
            _border = border;
            _sourceRect = initialRect;
            _sourceRect.height = 0;
            startY = _sourceRect.y;
            _sourceRect.y+=_border;
            _sourceRect.width -= 2*_border;
            _sourceRect.x += _border;
            _lastRect = _sourceRect;
            
        }
        public void ResetInLayout(float border = 0) {
            this.Reset(GUILayoutUtility.GetAspectRect(float.PositiveInfinity), border);
        }
        public float Height{
            get{
                return _sourceRect.y + _border - startY;
            }
        }
        public float AbsoluteHeight {
            get => _sourceRect.y;
        }
        public Rect CurrentRect{
            get{return _sourceRect;}
            set{_sourceRect = value;}
        }
        public Rect ConvertToLayout(){
            return GUILayoutUtility.GetRect(_sourceRect.width + _border * 2, Height);
        }
        public Rect NextRect() {
            return NextRect(EditorGUIUtility.singleLineHeight);
        }
        public Rect NextSplitRect(float width1, out Rect r1, out Rect r2) {
            var rect = NextRect();
            r1 = rect;
            r1.width = width1;
            r2 = rect;
            r2.x += width1;
            r2.width -= width1;
            return rect;
        }
        public float Indent {
            get => _indent;
            set => _indent = value;
        }
        public Rect LastRect() {
            return _lastRect;
        }
        public Rect NextRect(float lineHeight) {
            var r = _sourceRect;
            r.height = lineHeight;
            r.x += _indent;
            r.width -= _indent;
            _lastRect = r;
            _sourceRect.y+=lineHeight;
            return r;
        }
        public Rect NextRectWithLineCount(int count){
            return NextRect(EditorGUIUtility.singleLineHeight * count);
        }
    }
    public class ListToNonGeneric<T> : IList {
        private IList<T> _list;

        public ListToNonGeneric(IList<T> list) {
            _list = list;
        }

        public object this[int index] { get => _list[index]; set => _list[index] = ToT(value); }

        public bool IsFixedSize => false;

        public bool IsReadOnly => _list.IsReadOnly;

        public int Count => _list.Count;

        public bool IsSynchronized => false;

        public object SyncRoot => this;
        private T ToT(object value) {
            return value is T ? (T)value : default(T);
        }
        public int Add(object value)
        {
            var count = _list.Count;
            _list.Add(ToT(value));
            return count;
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(object value)
        {
            return _list.Contains(ToT(value));
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(object value)
        {
            return _list.IndexOf(ToT(value));
        }

        public void Insert(int index, object value)
        {
            _list.Insert(index, ToT(value));
        }

        public void Remove(object value)
        {
            _list.Remove(ToT(value));
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }
    }
    public class ArrayToList<T> : IList {
        public static void _testPrint(object message){
            UnityEngine.Debug.Log(message);
        }
        private T[] _array;
        public ArrayToList(T[] array) {
            _array = array;
            if(_array == null){
                _array = new T[0];
            }
        }

        public object this[int index] { get => _array[index]; set => _array[index] = ToT(value); }

        public T[] GetCurrentArray(){
            return _array;
        }

        public bool IsFixedSize => false;

        public bool IsReadOnly => false;

        public int Count => _array.Length;

        public bool IsSynchronized => _array.IsSynchronized;

        public object SyncRoot => _array.SyncRoot;

        public int Add(object value)
        {
            ArrayUtility.Add(ref _array, ToT(value));
            return _array.Length-1;
        }

        public void Clear()
        {
            if(_array.Length!=0){
                _array = new T[0];
            }
        }

        public bool Contains(object value)
        {
            for(int i = 0; i < _array.Length; i++){
                if(value.Equals(_array[i])) return true;
            }
            return false;
        }

        public void CopyTo(Array array, int index)
        {
            Array.Copy(_array, 0, array, index, _array.Length);
        }

        public IEnumerator GetEnumerator()
        {
            return _array.GetEnumerator();
        }

        public int IndexOf(object value)
        {
            for(int i = 0; i < _array.Length; i++){
                if(value.Equals(_array[i])) return i;
            }
            return -1;
        }

        public void Insert(int index, object value)
        {
            ArrayUtility.Insert(ref _array, index, ToT(value));
        }

        public void Remove(object value)
        {
            ArrayUtility.Remove(ref _array, ToT(value));
        }

        public void RemoveAt(int index)
        {
            ArrayUtility.RemoveAt(ref _array, index);
        }
        private T ToT(object o){
            if(o is T) return (T)o;
            return default(T);
        }
    }
    public class PropertyDescriptor
    {
        private UnityEngine.Object _object;
        private string _path;
        public PropertyDescriptor(){

        }
        public void SetProperty(SerializedProperty property){
            _object = property.serializedObject.targetObject;
            _path = property.propertyPath;
        }
        public void Clear(){
            _object = null;
            _path = null;
        }
        public PropertyDescriptor(SerializedProperty property){
            SetProperty(property);
        }
        public override bool Equals(object other){
            var o = other as PropertyDescriptor;
            if(o == null) return false;
            return _object == o._object && _path == o._path;
        }
        public override int GetHashCode() {
            return _object.GetInstanceID() + _path.GetHashCode();
        }
    }
    public static class EditorExtensions {
        private static void GetAssetPathAndName(UnityEngine.Object assetOrPrefabInstance, out string path, out string name) {
            
            if(assetOrPrefabInstance != null) {
                var ownerPath = AssetDatabase.GetAssetPath((UnityEngine.Object)assetOrPrefabInstance);
                
                if(string.IsNullOrEmpty(ownerPath)) {
                    GameObject root;
                    if(PrefabUtility.GetPrefabAssetType(assetOrPrefabInstance) == PrefabAssetType.Regular) {
                        root = PrefabUtility.GetNearestPrefabInstanceRoot((UnityEngine.Object)assetOrPrefabInstance);
                    }
                    else{
                        root = PrefabUtility.GetNearestPrefabInstanceRoot((UnityEngine.Object)assetOrPrefabInstance);
                    }
                    if(root!=null){
                        var prefabObj = PrefabUtility.GetCorrespondingObjectFromSource(root);
                        if(prefabObj!=null){
                            ownerPath = AssetDatabase.GetAssetPath(prefabObj);
                        }
                    }
                }
                if(!string.IsNullOrEmpty(ownerPath)){
                    path = Path.GetDirectoryName(ownerPath);
                    name = assetOrPrefabInstance.name;
                    return;
                }
            }
            path = null;
            name = null;
        }
        public static T CreateAssetForObject<T>(string title, string message = null, UnityEngine.Object ownerObject = null, string ownerNameExtension = null, T copySource = null) where T : ScriptableObject {
            string path, ownerName;
            GetAssetPathAndName(ownerObject, out path, out ownerName);
            
            if(ownerName != null){
                ownerName += ownerNameExtension;
            }
            var p = EditorUtility.SaveFilePanelInProject(title, ownerName, "asset", message, path);
            if(!string.IsNullOrEmpty(p)){
                T o;
                if(copySource==null){
                    o = ScriptableObject.CreateInstance<T>();
                }
                else{
                    o = UnityEngine.Object.Instantiate(copySource);
                }
                AssetDatabase.CreateAsset(o, p);
                return AssetDatabase.LoadAssetAtPath<T>(p);
            }
            return null;
        }
        public static bool AreFoldersTheSame(UnityEngine.Object o1, UnityEngine.Object o2) {
            string p1, p2, n1, n2;
            GetAssetPathAndName(o1, out p1, out n1);
            GetAssetPathAndName(o2, out p2, out n2);
            return p1 == p2;
        }

        public static void SetDirty(UnityEngine.Object go) {
            EditorUtility.SetDirty(go);
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null) {
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }
        }
        public static Bounds GetSceneEditorBounds(GameObject instance) {
            Bounds b;
            var rends = instance.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0) {
                b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) {
                    b.Encapsulate(rends[i].bounds);
                }
            }
            else {
                b = new Bounds(instance.transform.position, new Vector3(1, 1, 1));
            }
            return b;
        }
        public static bool IsSceneEdited {
            get {
                var t = Event.current.type;
                return t == EventType.MouseUp || t == EventType.KeyUp || t == EventType.Used;
            }
        }
    }
}