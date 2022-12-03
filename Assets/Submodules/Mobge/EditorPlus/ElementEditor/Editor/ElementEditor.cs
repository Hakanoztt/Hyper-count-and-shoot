using System;
using System.Collections;
using System.Collections.Generic;
using Mobge.EnumExtensions;
using Mobge.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting;
using static Mobge.InspectorExtensions;

namespace Mobge {
    public partial class ElementEditor : IDisposable {
        private static Stack<ElementEditor> s_currentEditors = new Stack<ElementEditor>();
        private static SelectionPath s_selectionPath = new SelectionPath();
        public static ElementEditor CurrentEditor {
            get {
                if (s_currentEditors.Count == 0) return null;
                return s_currentEditors.Peek();
            }
        }
        public bool treeD;
        private Dictionary<int, AEditableElement> _tempElements = new Dictionary<int, AEditableElement>();
        public EditorGrid grid;
        private LogicEditor _logicEditor;
        private VisualHandler _visualHandler;
        private CustomOptionsEditor _customOptionsEditor;
        private List<NewButtonData> _newButtons = new List<NewButtonData>();
        private List<NewButtonData> _matchingButtons = new List<NewButtonData>();
        private Elements _elements = new Elements();
        private List<PassiveElement> _passiveElements = new List<PassiveElement>();
        private EditorTools _editorTools;
        private EditorTools _nonDisabledTools;
        private RefreshType _requiredRefresh = RefreshType.Deep;
        private EditorSelectionQueue<AEditableElement> _selectionQueue;
        private ElementSelection _selection;
        private HashSet<ElementEditor> _children = new HashSet<ElementEditor>();
        public Editor Editor { get; set; }
        private SelectionRect _selectionRect;
        private bool _passiveAddMode, _invisibleMode;
        private Vector3 _passiveModeOffset;
        private int _repaintHash;
        private static CopyData _copiedData = new CopyData();
        private UnityEngine.Object _editingObject;
        private static string s_filter;
        private static int _chosenOption = 0;
        private static bool _tabPressed = false;
        private EditorPopup _currentPopup = null;


		public VisualHandler visualHandler => _visualHandler;
		public List<AEditableElement>.Enumerator AllElements => _elements.GetEnumerator();
        public int ElementCount => _elements.Count;
        public List<PassiveElement> PassiveElements => _passiveElements;
        public TypedEnumerator<T> GetElementsWithType<T>() {
            return _elements.GetElementsWithType<T>();
        }
        public DataTypeEnumerator<T> GetElementsWithDataType<T>() {
            return _elements.GetElementsWithDataType<T>();
        }
        public Action<EditorPopup, LayoutRectSource, Ray> onOptionsMenuGUI;
        public bool IsPassiveAddModeEnabled(out Vector3 offset) {
            offset = _passiveModeOffset;
            return _passiveAddMode;
        }
        public void SetPassiveAddEnabled(bool enabled, Vector3 offset) {
            _passiveAddMode = enabled;
            _passiveModeOffset = offset;
        }
        public ElementSelection Selection => _selection;
        public bool LogicMode {
            get => _logicEditor.Enabled;
            set => _logicEditor.Enabled = value;
        }
        private ElementEditor(Editor editor, VisualHandler visualHandler, UnityEngine.Object editingObject = null) : this(editor, editingObject) {
            _visualHandler = visualHandler;
            Editor = editor;
        }
        private ElementEditor(Editor editor, UnityEngine.Object editingObject = null) {
            _editingObject = editingObject;
            _selection = new ElementSelection(this);
            InitEditorTools();

            _selectionQueue = new EditorSelectionQueue<AEditableElement>();
        }
        void InitEditorTools() {
            _logicEditor = new LogicEditor(this);
            _selectionRect = new SelectionRect(this);
            _customOptionsEditor = new CustomOptionsEditor(this);
            _editorTools = new EditorTools();
			_editorTools.AddTool(new EditorTools.Tool("Options") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 1,
                },
                onPress = OpenOptionsPopup,
            });
            _editorTools.AddTool(new EditorTools.Tool("Delete Selection") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.Backspace,
                },
                onPress = () => true,
                onRelease = DeleteSelectionDialog,
            });
            _editorTools.AddTool(new EditorTools.Tool("Delete Selection") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.Delete,
                },
                onPress = () => true,
                onRelease = DeleteSelectionDialog,
            });
            _editorTools.AddTool(new EditorTools.Tool("Do Nothing on Ctrl D") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.D,
                    modifiers = EventModifiers.Control,
                },
                onPress = () => true,
            });
            _editorTools.AddTool(new EditorTools.Tool("Copy Selection") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.C,
                    modifiers = EventModifiers.Control,
                },
                onPress = () => true,
                onRelease = CopySelection,
            });
            _editorTools.AddTool(new EditorTools.Tool("Cut Selection") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.X,
                    modifiers = EventModifiers.Control,
                },
                onPress = () => true,
                onRelease = CutSelection,
            });
            _editorTools.AddTool(new EditorTools.Tool("Paste") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.V,
                    modifiers = EventModifiers.Control,
                },
                onPress = () => true,
                onRelease = PasteSelection,
            });
            _editorTools.AddTool(new EditorTools.Tool("Paste With Preserving Position") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.V,
                    modifiers = EventModifiers.Control | EventModifiers.Shift,
                },
                onPress = () => true,
                onRelease = PasteSelectionPreservingPosition,
            });
            _editorTools.AddTool(new EditorTools.Tool("Select") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0,
                },
                onPress = _selectionRect.PressNormal,
                onDrag = _selectionRect.OnDrag,
                onRelease = _selectionRect.Release,
            });
            _editorTools.AddTool(new EditorTools.Tool("Additive Select (shift)") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0,
                    modifiers = EventModifiers.Shift,
                },
                onPress = _selectionRect.PressAdditive,
                onDrag = _selectionRect.OnDrag,
                onRelease = _selectionRect.Release,
            });
            _editorTools.AddTool(new EditorTools.Tool("Inverse Select") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0,
                    modifiers = EventModifiers.Control,
                },
                onPress = _selectionRect.PressInverse,
                onDrag = _selectionRect.OnDrag,
                onRelease = _selectionRect.Release,
            });
            _editorTools.AddTool(new EditorTools.Tool("Toggle Connections") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.S,
                },
                onPress = () => {
                    _logicEditor.Enabled = !_logicEditor.Enabled;
                    return true;
                },
            });
            _editorTools.AddTool(new EditorTools.Tool("Edit selected") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.D,
                },
                onRelease = () => {
                    var se = SingleSelection;
                    if (se != null && se.RequestsExclusiveEdit) {
                        _selection.ExclusiveEditElement = se;
                        Editor.Repaint();
                    }
                },
            });
            _editorTools.AddTool(new EditorTools.Tool("focus") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.F,
                },
                onRelease = () => {
                    FocusToSelection();
                },
            });
            _editorTools.AddTool(new EditorTools.Tool("snap to grid") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.G,
                },
                onPress = () => {
                    return grid != null;
                },
                onRelease = SnapSelectionToGrid
            });
            _editorTools.AddTool(new EditorTools.Tool("snap everything to grid") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.G,
                    modifiers = EventModifiers.Shift
                },
                onPress = () => {
                    return grid != null;
                },
                onRelease = SnapToGrid
            });
            _nonDisabledTools = new EditorTools();
            _nonDisabledTools.AddTool(new EditorTools.Tool("block edit") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.D,
                },
            });
            _nonDisabledTools.AddTool(new EditorTools.Tool("block logic") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.S,
                },
            });
            _nonDisabledTools.AddTool(new EditorTools.Tool("empty click handle") {
                activation = new EditorTools.ActivationRule() {
                    mouseButton = 0
                },
                onPress = () => {
                    var exe = _selection.ExclusiveEditElement;
                    if (exe != null) {
                        return true;
                    }
                    if (_logicEditor.Enabled) {
                        return true;
                    }
                    return false;
                },
                onRelease = () => {
                    var exe = _selection.ExclusiveEditElement;
                    ReleaseExclusiveEdit(exe);
                    if (_logicEditor.Enabled) {
                        _logicEditor.Enabled = false;
                    }
                    Repaint();
                },
            });
            _nonDisabledTools.AddTool(new EditorTools.Tool("exit current editor") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.Escape,
                },
                onPress = () => {
                    var exe = _selection.ExclusiveEditElement;
                    if (exe != null) {
                        return true;
                    }
                    return false;
                },
                onRelease = () => {
                    var exe = _selection.ExclusiveEditElement;

                    ReleaseExclusiveEdit(exe);
                    Repaint();
                }
            });
            _nonDisabledTools.AddTool(new EditorTools.Tool("deselect selection") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.Escape,
                },
                onPress = () => {
                    if (_selection.Count > 0) {
                        return true;
                    }
                    return false;
                },
                onRelease = () => {
                    _selection.Clear();
                    Repaint();
                }
            });
        }

        private void SnapToGrid() {
            var elements = _elements.GetEnumerator();
            while (elements.MoveNext()) {
                var e = elements.Current;
                e.Position = grid.Snap(e.Position);
            }
        }
        private void SnapSelectionToGrid() {
            var en = _selection.GetEnumerator();
            while (en.MoveNext()) {
                var e = en.Current;
                e.Position = grid.Snap(e.Position);
            }
        }
        private void HandleAutoSnapToGrid() {
            if (grid != null && grid.autoSnap) SnapToGrid();
        }

        public bool TryGetElement(int id, out AEditableElement element) {
            return _elements.TryGet(id, out element);
        }
        public void DeleteElement(AEditableElement element) {
            InternalDeleteElement(element, true);
            if (this.EditingObject != null) {
                EditorExtensions.SetDirty(this.EditingObject);
            }
        }
        private void InternalDeleteElement(AEditableElement element, bool deleteFromSelection = false, bool fixAllConnections = true) {
            if (element.Delete()) {
                var t = TemporaryEditorObjects.Shared;
                _elements.Remove(element);
                t.RemoveObject(element);
                if (deleteFromSelection) {
                    _selection.Remove(element);
                }
                if (fixAllConnections) {
                    _logicEditor.FixAllConnections();
                }
            }
        }
        private void DeleteSelectionDialog() {
            if (_selection.Count == 0) return;
            EditorApplication.delayCall += () => {
                if (EditorUtility.DisplayDialog("Warning!", $"Are you sure to delete {_selection.Count} elements.", "OK", "Cancel")) {
                    DeleteSelection();
                }
            };
        }
        private void DeleteSelection() {
            if (_selection.Count == 0) return;
            RecordMainObject("edit");
            foreach (var s in _selection) {
                InternalDeleteElement(s, false, false);
            }
            _logicEditor.FixAllConnections();
            _selection.Clear();
            if (this.EditingObject != null) {
                EditorExtensions.SetDirty(this.EditingObject);
            }
        }
        private void CopySelection() {
            _copiedData.SetElements(this, _selection);
        }
        private void CutSelection() {
            CopySelection();
            DeleteSelection();
        }
        private void PasteSelection() {
            PasteSelection(GetCreatePos());
        }
        private void PasteSelection(Vector3 createPos) {
            RecordMainObject("edit");
            _copiedData.PasteElements(this, createPos);
            _logicEditor.FixAllConnections();

            if (this.EditingObject != null) {
                EditorExtensions.SetDirty(this.EditingObject);
            }
        }
        private void PasteSelectionPreservingPosition() {
            PasteSelection(Vector3.zero);
        }
        private Vector3 GetCreatePos() {
            var ray = _visualHandler.MouseRay;
            if (Physics.Raycast(ray, out RaycastHit hit,float.PositiveInfinity, -1, QueryTriggerInteraction.Ignore)) {
                return hit.point;
            }
            if (_selection.Count <= 0) {
                return _visualHandler.MousePosition;
            }
            var oldCenter = GetSelectionCenter().position;
            var camera = SceneView.lastActiveSceneView.camera;
            var cameraTransform = camera.transform;
            var forward = cameraTransform.forward;
            forward.x = Mathf.Abs(forward.x);
            forward.y = Mathf.Abs(forward.y);
            forward.z = Mathf.Abs(forward.z);
            if (MathExtensions.ApproximatelyClose(forward, Vector3.right) ||
                MathExtensions.ApproximatelyClose(forward, Vector3.up) ||
                MathExtensions.ApproximatelyClose(forward, Vector3.forward)) {
                var plane = new Plane(forward, oldCenter);
                if (plane.Raycast(ray, out var enter)) {
                    return ray.GetPoint(enter);
                }
            }
            return _visualHandler.MousePosition;
        }
        public void ClosePopup() {
            if(_currentPopup!= null) {
                _currentPopup.Close();
            }
        }
        private bool OpenOptionsPopup() {
            s_filter = "";
            _chosenOption = 0;
            _tabPressed = false;
            var ray = _visualHandler.MouseRay;
            var createPos = GetCreatePos();
            var mouseElement = FindElementToSelect(true, false, null, true);


            _newButtons.Sort((data, buttonData) => {
                if (data.menuPriority != buttonData.menuPriority) {
                    return buttonData.menuPriority - data.menuPriority;
                }
                else {
                    return string.CompareOrdinal(data.name, buttonData.name);
                }
            });
            
            var popupEditorTools = new EditorTools();
            popupEditorTools.AddTool(new EditorTools.Tool("Add Element Menu Highlight Down") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.Tab,
                },
                onPress = () => {
                    _chosenOption++;
                    _tabPressed = true;
                    return false;
                },
            });
            popupEditorTools.AddTool(new EditorTools.Tool("Add Element Menu Highlight Up") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.Tab,
                    modifiers = EventModifiers.Shift,
                },
                onPress = () => {
                    _chosenOption--;
                    _tabPressed = true;
                    return false;
                },
            });
            popupEditorTools.AddTool(new EditorTools.Tool("Add Element Menu Highlight Down") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.DownArrow,
                },
                onPress = () => {
                    _chosenOption++;
                    _tabPressed = true;
                    return false;
                },
            });
            popupEditorTools.AddTool(new EditorTools.Tool("Add Element Menu Highlight Up") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.UpArrow,
                },
                onPress = () => {
                    _chosenOption--;
                    _tabPressed = true;
                    return false;
                },
            });
            popupEditorTools.AddTool(new EditorTools.Tool("Add Element Menu Space Add and Close") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.Space,
                },
                onPress = () => {
                    if (!_tabPressed) return false;
                    var b = _matchingButtons[_chosenOption];
                    CreateElement(b, createPos);
                    _currentPopup?.Close();
                    return true;
                },
            });
            popupEditorTools.AddTool(new EditorTools.Tool("Add Element Menu Enter to Add") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.Return,
                },
                onPress = () => {
                    if (_chosenOption < 0 || _chosenOption >= _matchingButtons.Count) return false;
                    var b = _matchingButtons[_chosenOption];
                    CreateElement(b, createPos);
                    _currentPopup?.Close();
                    return true;
                },
            });
            bool firstTime = true;
            int optionsIndex = -1;
            _currentPopup = new EditorPopup((rects, popup) => {
                if (mouseElement != null && !_selection.Contains(mouseElement)) {
                    SingleSelection = mouseElement;
                }
                popupEditorTools.OnSceneGUI();
                if (_matchingButtons.Count > 0) {
                    _chosenOption = (int) Mathf.Repeat(_chosenOption, _matchingButtons.Count);
                }
                
                var r = rects.NextRect();
                var rCopy = LayoutHelper.DivideLayoutRectHorizontallyWithPercentage(r, 0, 50);
                var rPaste = LayoutHelper.DivideLayoutRectHorizontallyWithPercentage(r, 50, 50); ;
                if (GUI.Button(rCopy, "copy")) {
                    CopySelection();
                    popup.Close();
                }
                using (Scopes.GUIEnabled(GUI.enabled && _copiedData.HasElements)) {
                    if (GUI.Button(rPaste, "paste")) {
                        PasteSelection(createPos);
                        popup.Close();
                    }
                }
                if (onOptionsMenuGUI != null) {
                    onOptionsMenuGUI(popup, rects, ray);
                }
                var filterRect = rects.NextRect(25f);
                GUI.SetNextControlName("popup filter");
                s_filter = CustomFields.SearchField(new LayoutRectSource(filterRect), s_filter);
                if (firstTime) {
                    GUI.FocusControl("popup filter");
                    firstTime = false;
                }
                _matchingButtons.Clear();
                int j = 0;
                for (int i = 0; i < _newButtons.Count; i++) {
                    var b = _newButtons[i];
                    if(TextMatchesSearch(b.name, s_filter)){
                        _matchingButtons.Add(b);
                        Color c = j++ != _chosenOption ? Color.white : new Color(0.8f, 0.8f, 0.8f,1);
                        var rect = rects.NextRect();
                        using (Scopes.GUIBackgroundColor(c)) {
                            if (b.optionsGUI != null) {
                                var r2 = rect;
                                var width = EditorGUIUtility.singleLineHeight;
                                r2.width = width;
                                r2.x += rect.width - width;
                                rect.width -= width;
                                if (GUI.Button(r2, "?")) {
                                    if (optionsIndex < 0) {
                                        optionsIndex = i;
                                    }
                                    else {
                                        optionsIndex = -1;
                                    }
                                }
                            }
                            if (GUI.Button(rect, b.name)){
                                CreateElement(b, createPos);
                                popup.Close();
                            }
                        }
                        if (optionsIndex == i) {

                            using (Scopes.GUIBackgroundColor(new Color(0.8f, 0.8f, 1f, 1f))) {
                                rects.NextRect(5);
                                rects.Indent += 15;
                                b.optionsGUI(rects, this);
                                rects.Indent -= 15;
                                rects.NextRect(5);
                            }
                        }
                    }
                }
                
            });
            _currentPopup.Show(new Rect(_visualHandler.ScreenMousePosition, Vector2.zero), new Vector2(300, 300));
            return true;
        }
        private void CreateElement(NewButtonData b, Vector3 createPos) {
            RecordMainObject("edit");
            var element = b.createElement();
            AddElement(element);
            SetPosition(element, createPos);
            SingleSelection = element;
        }
        public Matrix4x4 Matrix {
            get => _visualHandler.matrix;
            set {
                if (!_visualHandler.matrix.Equals(value)) {
                    _visualHandler.matrix = value;
                    var t = BeginMatrix();
                    for (int i = 0; i < _elements.Count; i++) {
                        UpdateVisualInstance(_elements[i]);
                    }
                    EndMatrix(t);
                }
            }
        }
        public void UpdateMatrix(Transform tr, Vector3 localOffset) {
            var m = tr.localToWorldMatrix;
            m.SetPosition(m.MultiplyPoint3x4(localOffset));
            Matrix = m;
        }
        public void UpdateMatrix(Vector3 position) {
            Matrix = Matrix4x4.Translate(position);
        }
        private Matrix4x4 GetParentMatrix(AEditableElement element) {
            if (!_elements.TryGetParent(element, out element)) {
                return Matrix4x4.identity;
            }
            return GetMatrix(element);
        }
        public Matrix4x4 GetMatrix(AEditableElement element) {
            Matrix4x4 mat = Matrix4x4.TRS(element.Position, element.Rotation, Vector3.one);
            while (_elements.TryGetParent(element, out element)) {
                mat = Matrix4x4.TRS(element.Position, element.Rotation, Vector3.one) * mat;
            }
            return mat;
        }
        private Vector3 GetPosition(AEditableElement element) {
            var mat = GetParentMatrix(element);
			return mat.MultiplyPoint3x4(element.Position);
		}
		public Pose GetPose(AEditableElement element) {
            var mat = GetParentMatrix(element);
            Pose p;
            p.position = mat.MultiplyPoint3x4(element.Position);
            p.rotation = mat.rotation * element.Rotation;
            return p;
        }
        public void SetPose(AEditableElement element, Pose pose) {
            var mat = GetParentMatrix(element).inverse;
            element.Position = mat.MultiplyPoint3x4(pose.position);
            if (element.HasRotation) {
                element.Rotation = mat.rotation * pose.rotation;
            }
        }
        private Quaternion GetRotation(AEditableElement element) {
            var mat = GetParentMatrix(element);
            return (mat.rotation * element.Rotation).normalized;
        }
        private void SetRotatoin(AEditableElement element, Quaternion value) {
            var mat = GetParentMatrix(element);
            element.Rotation = Quaternion.Inverse(mat.rotation) * value;
        }
        public void SetPosition(AEditableElement element, Vector3 value) {
            var mat = GetParentMatrix(element);
            element.Position = mat.inverse.MultiplyPoint3x4(value);
        }

        private AEditableElement FindElementToSelect(bool silent, bool checkEquality = false, AEditableElement equalityElement = null, bool ignoreScene = false) {
            bool addedToSelectionQueue = false;
            for (int i = 0; i < _elements.Count; i++) {
                var e = _elements[i];
                var d = _visualHandler.MouseRay;
                if (e.selectionArea.Collides(d)) {
                    _selectionQueue.AddCandidate(e);
                    addedToSelectionQueue = true;
                }
            }
            if (!addedToSelectionQueue && !ignoreScene) {
                var obj = GetElementFromTemporaryObjects();
                if (obj != null) {
                    _selectionQueue.AddCandidate(obj);
                }
            }
            AEditableElement selectedElement;
            if (checkEquality) {
                _selectionQueue.SelectIfEquals(equalityElement, out selectedElement, silent);
            }
            else {
                _selectionQueue.SelectOne(out selectedElement, silent);
            }
            return selectedElement;
        }
        private AEditableElement GetElementFromTemporaryObjects() {
            var picked = PickFromTemporaryEditorObjects();
            if (picked != null) {
                var selectNotifier = UnityExtensions.GetComponentInParent<OnEditorSelectNotifier>(picked.transform);
                if (selectNotifier != null && selectNotifier.editor == this) {
                    return (AEditableElement)selectNotifier.editableElement;
                }
            }
            return null;
        }
        private GameObject PickFromTemporaryEditorObjects() {
            return HandleUtility.PickGameObject(_visualHandler.ScreenMousePosition, true);
        }
        public AEditableElement SingleSelection {
            get {
                if (_selection.Count != 1) return null;
                var e = _selection.GetEnumerator();
                e.MoveNext();
                return e.Current;
            }
            set {
                _selection.Clear();
                _selection.Add(value);
                Editor.Repaint();
            }
        }
        public UnityEngine.Object EditingObject {
            get => _editingObject;
            set {
                if (_editingObject != value) {
                    RefreshContent(true);
                    _editingObject = value;
                }
            }
        }

        public static ElementEditor NewForScene(Editor editor, Plane plane, UnityEngine.Object editingObject = null) {

            return new ElementEditor(editor, new VisualHandler(), editingObject);
        }
        public static ElementEditor NewForScene(Editor editor, UnityEngine.Object editingObject = null) {

            return new ElementEditor(editor, new VisualHandler(), editingObject);
        }

        public void RefreshContent(bool deep) {
            if (deep) {
                _requiredRefresh = RefreshType.Deep;
            }
            else {
                _requiredRefresh = RefreshType.Shallow;
            }
            
        }
        private void ClearElements() {
            var t = TemporaryEditorObjects.Shared;
            if (t == null) {
                _elements.Clear();
                _passiveElements.Clear();
                return;
            }
            ClearVisuals();
            _elements.Clear();
            for (int i = 0; i < _passiveElements.Count; i++) {
                t.RemoveObject(_passiveElements[i].element);
            }
            _passiveElements.Clear();
            
        }
        public void ClearVisuals() {
            var t = TemporaryEditorObjects.Shared;
            if (t != null) {
                for (int i = 0; i < _elements.Count; i++) {
                    t.RemoveObject(_elements[i]);
                }
            }
        }
        internal void ReleaseExclusiveEdit(AEditableElement element) {
            if (_selection.ExclusiveEditElement == element) {
                _selection.ExclusiveEditElement = null;
            }
            this.Editor.Repaint();
        }
        internal bool ExclusiveEditField(string label, AEditableElement element) {
            bool exc = _selection.ExclusiveEditElement == element;
            EditorGUI.BeginChangeCheck();
            exc = EditorGUILayout.Toggle(label, exc);
            if (EditorGUI.EndChangeCheck()) {
                Repaint();
            }
            if (exc) {
                _selection.ExclusiveEditElement = element;
                _logicEditor.Enabled = false;
            }
            else {
                _selection.ExclusiveEditElement = null;
            }
            return exc;
        }
        private AEditableElement UpdateExclusiveEditElement() {
            if (_selection.ExclusiveEditElement == null) {
                return null;
            }
            if (_selection.Count != 1) {
                _selection.ExclusiveEditElement = null;
                return null;
            }
            var e = _selection.GetEnumerator();
            e.MoveNext();
            var eee = _selection.ExclusiveEditElement;
            if (e.Current == eee) {
                return eee;
            }
            return null;
        }
        private Matrix4x4 BeginMatrix() {
            return BeginMatrix(_visualHandler.matrix);
        }
        public Matrix4x4 BeginMatrix(in Matrix4x4 matrix) {
            var tempMatrix = Handles.matrix;
            Handles.matrix = tempMatrix * matrix;
            return tempMatrix;
        }
        public void EndMatrix(in Matrix4x4 tempMatrix) {
            Handles.matrix = tempMatrix;
        }
        void HandleDirty(Action updateContent) {
            switch (_requiredRefresh) {
                case RefreshType.Deep:
                    //ClearPreviousExclusive();
                    _selectionQueue.Clear();
                    _newButtons.Clear();
                    _selection.Clear();
                    ClearElements();
                    updateContent();
                    break;
                case RefreshType.Shallow:
                    //ClearPreviousExclusive();
                    _selectionQueue.Clear();
                    //_selection.Clear();
                    _newButtons.Clear();
                    BackupElements();
                    ClearElements();
                    _invisibleMode = true;
                    updateContent();
                    _invisibleMode = false;
                    ReplaceEditorsWithTemp();
                    var e = _elements.GetEnumerator();
                    while (e.MoveNext()) {
                        var ec = e.Current;
                        var tr = UpdateVisualInstance(ec);
                        if (tr) {
                            ec.UpdateVisuals(tr);
                        }
                    }
                    RefreshSelection();
                    break;
            }
            if(_requiredRefresh!= RefreshType.None) {
                HandleAutoSnapToGrid();
            }
            _requiredRefresh = RefreshType.None;
            //HandleAutoSnapToGrid();
        }
        public void OnDeselect() {
            _selection.ExclusiveEditElement = null;
            foreach (var item in _children) {
                item.ClearVisuals();
                item.OnDeselect();
            }
            _children.Clear();
        }
        public void Dispose() {
            ClearElements();
            _requiredRefresh = RefreshType.Deep;
        }
        void BackupElements() {
            var e = _elements.GetIdEnumerator();
            while (e.MoveNext()) {
                _tempElements.Add(e.Current.Key, e.Current.Value);
            }
        }
        void ReplaceEditorsWithTemp() {
            _elements.ReplaceFoundEditors(_tempElements);
            _tempElements.Clear();
        }
        void RefreshSelection() {
            var s = _selection.GetEnumerator();
            while (s.MoveNext()) {
                var e = s.Current;
                _tempElements.Add(e.Id, e);
            }
            _selection.Clear();
            var te = _tempElements.GetEnumerator();
            while (te.MoveNext()) {
                if(_elements.TryGet(te.Current.Key, out AEditableElement existing)) {
                    _selection.Add(existing);
                }
            }
            _tempElements.Clear();
        }
        public Transform CreateVisuals(Action updateContentIfNecessary, string name = "tr") {
            _invisibleMode = true;
            HandleDirty(updateContentIfNecessary);
            _invisibleMode = false;
            var tr = new GameObject(name).GetComponent<Transform>();
            for (int i = 0; i < _elements.Count; i++) {

                //Debug.Log("creating create: " + _elements[i]);
                var v = _elements[i].CreateVisuals();
                if (v != null) {
                    v.SetParent(tr, false);
                    var p = GetPose(_elements[i]);
                    v.localPosition = p.position;
                    v.localRotation = p.rotation;
                }
            }
            return tr;
        }
        private void DrawOrigin(in Matrix4x4 matrix) {
            var t = BeginMatrix(matrix);
            var rad = this._visualHandler.ElementRadius(Vector3.zero) * 2;
            Handles.color = Color.red;
            Handles.DrawLine(new Vector3(rad, 0, 0), new Vector3(-rad, 0, 0));
            Handles.color = Color.blue;
            Handles.DrawLine(new Vector3(0, 0, rad), new Vector3(0, 0, -rad));
            Handles.color = Color.green;
            Handles.DrawLine(new Vector3(0, rad, 0), new Vector3(0, -rad, 0));
            EndMatrix(t);
        }
        private void GetSelectionBoundingBox(out Vector3 min, out Vector3 max) {
            if(_selection.Count == 0) {
                min = Vector3.zero;
                max = Vector3.zero;
                return;
            }
            var sel = _selection.GetEnumerator();
            min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            
            while (sel.MoveNext()) {
                var c = sel.Current;
                var p = c.Position;
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
        }
        public void SceneGUI(Action updateContentIfNecessary) {
            if (s_currentEditors.Count > 0) {
                var eePeek = s_currentEditors.Peek();
                if (eePeek != this) {
                    eePeek._children.Add(this);
                }
            }
            s_currentEditors.Push(this);
            var tempMatrix = BeginMatrix();

            HandleDirty(updateContentIfNecessary);
            HandleSelectionPath();
            bool hasExclusive = UpdateExclusiveEditElement() != null;
            if (!hasExclusive) {
                if (!_logicEditor.Enabled) {
                    UpdateMoveControl();
                }
                _logicEditor.PreVisualSceneGUI();
            }
            var s = FindElementToSelect(true, false, null, true);
            // Rect r = new Rect(0, 0, -1, -1);
            int repaintHash = -1;
            
            _selectionRect.DrawScene();
            var se = SingleSelection;
            for (int i = 0; i < _elements.Count; i++) {
                var e = _elements[i];
                var selected = _selection.Contains(e);
                var pose = GetPose(e);
                var pos = pose.position;
                var rot = pose.rotation;
                AEditableElement.SceneParams @params;
                @params.selected = selected && !_logicEditor.Enabled;
                @params.solelySelected = (se == e) && !_logicEditor.Enabled;
                @params.position = pos;
                @params.rotation = rot;
                if (e.HasRotation) {
                    //float sqr = rot.x * rot.x + rot.y * rot.y + rot.z * rot.z + rot.w * rot.w;
                    //if (sqr < 0.9999f || sqr > 1.0001f) {
                    //    Debug.Log(sqr);
                    //}
                    float f = 1f / Mathf.Sqrt(rot.x * rot.x + rot.y * rot.y + rot.z * rot.z + rot.w * rot.w);
                    rot = new Quaternion(rot.x * f, rot.y * f, rot.z * f, rot.w * f);
                    @params.matrix = Matrix4x4.TRS(pos, rot, Vector3.one);
                }
                else {
                    @params.matrix = Matrix4x4.Translate(pos);
                }
                var eee = _selection.ExclusiveEditElement;
                if (e == eee) {
                    DrawOrigin(@params.matrix);
                }
                if (eee == null || e == eee || eee.DrawOtherObjectsGizmos) {
                    if (e.SceneGUI(@params)) {
                        e.UpdateData();
                        e.UpdateVisuals(UpdateVisualInstance(e));
                    }
                }
                if (!hasExclusive) {
                    bool highlight = (s == e || _selectionRect.Contains(pos));
                    if (highlight) {
                        repaintHash += (2 * i + 1);
                    }
                    if (selected) {
                        repaintHash += (3 * i + 1);
                    }
                    _visualHandler.DrawElement(e, pos, highlight, selected, _logicEditor.Enabled);
                }
            }
            if (!hasExclusive) {
                _logicEditor.PostVisualSceneGUI();
                _editorTools.OnSceneGUI();
                if (grid) {
                    GetSelectionBoundingBox(out Vector3 min, out Vector3 max);
                    EEditorGrid.DrawGizmos(grid, min, max);
                }
            }
            _nonDisabledTools.OnSceneGUI();
            bool repaint = repaintHash != _repaintHash;
            if (repaint) {
                Repaint();
                _repaintHash = repaintHash;
            }
            

            EndMatrix(tempMatrix);
            s_currentEditors.Pop();
        }
        public void Repaint() {
            var cdsv = SceneView.currentDrawingSceneView;
            if (!cdsv) {
                cdsv = SceneView.lastActiveSceneView;
            }
            if (cdsv) {
                cdsv.Repaint();
            }
        }
        private Pose GetSelectionCenter() {
                Vector3 center = Vector3.zero;
            int count = 0;
            Quaternion rot = Quaternion.identity;
            foreach (var e in _selection) {
                count++;
                center += GetPosition(e);
                rot = GetRotation(e);
            }

            center /= count;
            return new Pose(center, count > 1?Quaternion.identity:rot);
        }
        private bool IsAnchestorInSelection(AEditableElement element) {
            while (_elements.TryGetParent(element, out AEditableElement parent)) {
                if (_selection.Contains(parent)) {
                    return true;
                }
                element = parent;
            }
            return false;
        }
        private void UpdateSelectionCenter(Vector3 oldCenter, Vector3 newCenter) {
            if (oldCenter != newCenter) {
                var dif = newCenter - oldCenter;
                foreach (var e in _selection) {
                    if (!IsAnchestorInSelection(e)) {
                        SetPosition(e, GetPosition(e) + dif);
                    }
                    UpdateVisualInstance(e);
                }
            }
        }
        private void SetSelectionCenter(Vector3 center) {
            var oldCenter = GetSelectionCenter();
			UpdateSelectionCenter(oldCenter.position, center);
        }
        private void UpdateMoveControl() {
            int count = _selection.Count;

            if (count == 0) {
                return;
            }
            var se = SingleSelection;
            if (se!=null && !se.HandlesEnabled) return;
            var tm = Handles.matrix;
            Handles.matrix = Matrix4x4.identity;
            var selectionPose = GetSelectionCenter();
            var wCenter = tm.MultiplyPoint3x4(selectionPose.position);
            var preType = Event.current.type;
            var toolRotation = tm.rotation * selectionPose.rotation;
            if (Tools.pivotRotation == PivotRotation.Global) {
                toolRotation = Quaternion.identity;
            }
            var newPos = wCenter;
            if (Tools.current == Tool.Move) {
                newPos = Handles.PositionHandle(wCenter, toolRotation);
            }
            if (se != null && se.HasRotation) {
                var oldRot = tm.rotation * se.Rotation;
                var rot = oldRot;
                if (Tools.current == Tool.Rotate) {
                    rot = Handles.RotationHandle(oldRot, newPos);
                }
                if (rot != oldRot) {
                    se.Rotation = Quaternion.Inverse(tm.rotation) * rot;
                    UpdateVisualInstance(se);
                }
            }
            Handles.matrix = tm;
            newPos = tm.inverse.MultiplyPoint3x4(newPos);
            UpdateSelectionCenter(selectionPose.position, newPos);

            if (Event.current.type == EventType.Used) {
                if(preType == EventType.MouseUp) {
                    HandleAutoSnapToGrid();
                }
            }
        }
        private void TagField() {
            //EditorGUILayout.LabelField("as daf das daf");
        }

        private IEnumerable<EditorDrawer.Pair> ElementsWithIDParent(ElementEditor editor) {
            var e = editor.AllElements;
            while (e.MoveNext()) {
                var c = e.Current;
                if (c.Id >= 0 && c.IsParent) {
                    yield return new BaseEditorDrawer.Pair(c.Id, c.Id + ": " + c.DataObject.ToString());
                }
            }
        }

        private void UndoRedoPerformed() {
            RefreshContent(false);
        }
        public void InspectorField() {
            s_currentEditors.Push(this);
            if (_editingObject) {
                Undo.undoRedoPerformed = UndoRedoPerformed;
            }
            var tm = BeginMatrix();
            TagField();
            var se = SingleSelection;
            AEditableElement exclusive = UpdateExclusiveEditElement();
            if (se != null) {

                EditorGUILayout.BeginVertical("Box");

                EditorGUILayout.LabelField(se.Name);
                if (se.Id >= 0) {
                    EditorGUI.BeginChangeCheck();
                        var newId = EditorGUILayout.DelayedIntField("id", se.Id);
                    if (EditorGUI.EndChangeCheck()) {
                        if (_elements.TryGet(newId, out var element)) {
                            SingleSelection = element;
                        }
                    }
                }
                if (se.IsChild) {
                    var pos = GetPosition(se);
                    EditorGUI.BeginChangeCheck();
                    var newParent = EditorLayoutDrawer.Popup("parent", ElementsWithIDParent(this), se.Parent, "none");
                    if (EditorGUI.EndChangeCheck()) {
                        // todo: add circular parent reference check
                        se.Parent = newParent;
                        SetPosition(se, pos);
                        se.UpdateData();
                    }
                }
                
                var r = EditorGUILayout.GetControlRect(false, 2);
                EditorGUI.DrawRect(r, Color.gray);
                var pref = se.PrefabReference;
                Transform instace;
                // if (pref != null)
                // {
                //     instace = UpdateVisualPrefab(se);
                // }
                // else
                // {
                //     instace = null;
                //     RemoveVisualPrefab(se);
                // }
                instace = UpdateVisualInstance(se);
                se.InspectorGUILayout();
                if (se.PrefabReference != pref) {
                    RemoveVisualPrefab(se);
                    instace = UpdateVisualInstance(se);
                }
                se.UpdateVisuals(instace);



                EditorGUILayout.EndVertical();

            }
            if(exclusive == null) {
                _customOptionsEditor.OnGUI();
            }
            EndMatrix(tm);
            
            s_currentEditors.Pop();
        }
        private void RemoveVisualPrefab(AEditableElement element) {
            TemporaryEditorObjects.Shared.RemoveObject(element);
        }
        private void UpdateTransform(AEditableElement element, Transform transform) {
            if (element.HasRotation) {
                transform.localPosition = Handles.matrix.MultiplyPoint3x4(GetPosition(element));
                transform.localRotation = Handles.matrix.rotation * GetRotation(element);
            }
            else {
                transform.localPosition = Handles.matrix.MultiplyPoint3x4(GetPosition(element));
            }
        }
        public void FocusToSelection() {
            var en = _selection.GetEnumerator();
            if (!en.MoveNext()) {
                return;
            }

            Bounds merged = GetBounds(en.Current);

            while (en.MoveNext()) {

                merged.Encapsulate(GetBounds(en.Current));
            }
            SceneView.lastActiveSceneView.Frame(merged);
        }
        private Bounds GetBounds(AEditableElement e) {
            Bounds b;
            if (TemporaryEditorObjects.Shared.TryGetInstance(e, out Transform instance)) {

                b = EditorExtensions.GetSceneEditorBounds(instance.gameObject);
            }
            else {
                b = new Bounds(GetPosition(e), new Vector3(1, 1, 1));
            }
            return b;
        }
        public Transform UpdateVisualInstance(AEditableElement element) {
            if (EditorApplication.isPlaying) {
                return null;
            }
            var to = TemporaryEditorObjects.Shared;
            Transform instance;
            if (to.TryGetInstance(element, out instance)) {
                UpdateTransform(element, instance);
            }
            else {
                var p = element.PrefabReference;
                if (p) {
                    instance = to.EnsureObject(element, element.PrefabReference, GetSelectAction(element), this);
                    UpdateTransform(element, instance);
                }
                else {
                    //Debug.Log("creating update: " + element);
                    instance = element.CreateVisuals();
                    if (instance) {
                        to.SetObject(element, instance, GetSelectAction(element), this);
                        UpdateTransform(element, instance);
                    }
                }
            }
            return instance;
        }
        private Action GetSelectAction(AEditableElement element) {
            var ed = Editor;
            var ths = this;
            return () => {
                if (TemporaryEditorObjects.Shared.debugMode) return;
                for (int i = 0; i < UnityEditor.Selection.transforms.Length; i++) {
                    var tra = UnityEditor.Selection.transforms[i];
                    var notifier =  UnityExtensions.GetComponentInParent<OnEditorSelectNotifier>(tra);
                    if (notifier != null) {
                        if ((ElementEditor)notifier.editor == ths) {
                            s_selectionPath.AddId(((AEditableElement)notifier.editableElement).Id);
                        }
                    }
                }
                UnityEditor.Selection.activeObject = ed.target;
            };
        }
        private void HandleSelectionPath() {
            if (Editor != null && s_selectionPath.Exists) {
                _selection.Clear();
                while (s_selectionPath.Exists) {
                    var id = s_selectionPath.Consume();
                    if (_elements.TryGet(id, out AEditableElement e)) {
                        _selection.Add(e);
                    }
                }
                Editor.Repaint();
                SceneView.lastActiveSceneView.Repaint();
            }
            
        }
        public void AddElement(AEditableElement element) {
            element.ElementEditor = this;
            Transform tr = _invisibleMode ? null : UpdateVisualInstance(element);
            if (_passiveAddMode) {
                if (tr) {
                    element.UpdateVisuals(tr);
                    tr.localPosition += _passiveModeOffset;
                    _passiveElements.Add(new PassiveElement(element));
                }
            }
            else {
                if (tr) {
                    element.UpdateVisuals(tr);

                }
                _elements.Add(element);
            }
        }
        public void AddButtonData(NewButtonData buttonData) {
            if (_passiveAddMode) {
                return;
            }
            _newButtons.Add(buttonData);
        }

        public void RecordObject(UnityEngine.Object obj, string name) {
            Undo.RecordObject(obj, name);

        }
        public void RecordMainObject(string name) {
            if (_editingObject) {
                RecordObject(_editingObject, name);
            }
        }

        public delegate void GlobalComponentSettingsField(LayoutRectSource layout, ElementEditor editor);
        public struct NewButtonData {
            public string name;
            public int menuPriority;
            public Func<AEditableElement> createElement;
            public GlobalComponentSettingsField optionsGUI;
            private object _descriptor;
            /// <summary>
            /// Unique descriptor for the button. If no descriptor is supplied on constructor, name is used as the descriptor.</summary>
            public object Descriptor => _descriptor;

            public NewButtonData(string name, int menuPriority, Func<AEditableElement> createElement, GlobalComponentSettingsField optionsGUI = null) : this(name, menuPriority, createElement, name, optionsGUI) {

            }
            public NewButtonData(string name, int menuPriority, Func<AEditableElement> createElement, object descriptor, GlobalComponentSettingsField optionsGUI = null) {
                this.name = name;
                this.menuPriority = menuPriority;
                this.createElement = createElement;
                this._descriptor = descriptor;
                this.optionsGUI = optionsGUI;
            }
        }
        private class NewElementPopup : EditorPopup {
            public NewElementPopup(Action<LayoutRectSource, EditorPopup> drawContent) : base(drawContent) {
            }
        }
        public struct PassiveElement {
            public AEditableElement element;

            public PassiveElement(AEditableElement element) {
                this.element = element;
            }
        }
        public struct TypedEnumerator<T> : IEnumerator<T>{
            private IList _list;
            private int _index;

            public TypedEnumerator(IList list) {
                _list = list;
                _index = -1;
            }

            public T Current => (T)_list[_index];

            object IEnumerator.Current => _list[_index];

            public void Dispose() {

            }

            public bool MoveNext() {
                while(++_index < _list.Count) {
                    if(_list[_index] is T) {
                        return true;
                    }
                }
                return false;
            }

            public void Reset() {
                _index = -1;
            }
        }
        public struct DataTypeEnumerator<T> : IEnumerator<KeyValuePair<int,T>> {
            private List<AEditableElement>.Enumerator _e;
            private T _current;
            private int _id;

            public DataTypeEnumerator(List<AEditableElement> elements) {
                _e = elements.GetEnumerator();
                _current = default;
                _id = -1;
            }

            public void Reset() {
                _current = default;
                _id = -1;
            }

            public KeyValuePair<int, T> Current {
                get {
                    var cc = _e.Current;
                    return new KeyValuePair<int, T>(_id, _current);
                }
            }

            object IEnumerator.Current {
                get {
                    return Current;
                }
            }

            public void Dispose() {
                _e.Dispose();
            }

            public bool MoveNext() {
                while (_e.MoveNext()) {
                    var cc = _e.Current;
                    if (cc.DataObject is T t) {
                        _current = t;
                        _id = cc.Id;
                        return true;
                    }
                }
                return false;
            }

        }
        private class Elements {
            private List<AEditableElement> _elements;
            private Dictionary<int, AEditableElement> _elementsWithId;
            public Elements() {
                _elements = new List<AEditableElement>();
                _elementsWithId = new Dictionary<int, AEditableElement>();
            }

            public int Count => _elements.Count;
            public AEditableElement this[int index] {
                get => _elements[index];
            }
            public List<AEditableElement>.Enumerator GetEnumerator() {
                return _elements.GetEnumerator();
            }
            public TypedEnumerator<T> GetElementsWithType<T>() {
                return new TypedEnumerator<T>(_elements);
            }
            public Dictionary<int, AEditableElement>.Enumerator GetIdEnumerator() {
                return _elementsWithId.GetEnumerator();
            }

            public void Remove(AEditableElement s) {
                _elements.Remove(s);
                _elementsWithId.Remove(s.Id);
            }
            public void ReplaceFoundEditors(Dictionary<int, AEditableElement> editors) {
                _elementsWithId.Clear();
                var ts = TemporaryEditorObjects.Shared;
                for (int i = 0; i < _elements.Count; i++) {
                    var e = _elements[i];
                    if(e.Id >= 0) {
                        if(editors.TryGetValue(e.Id, out AEditableElement oldE)) {
                            //ts.ReplaceOwner(e, oldE);
                            ts.RemoveObject(e);
                            oldE.DataObject = e.DataObject;
                            _elements[i] = oldE;
                            _elementsWithId.Add(oldE.Id, oldE);
                        }
                        else {
                            _elementsWithId.Add(e.Id, e);
                        }
                    }
                }
            }
            public void Add(AEditableElement element) {
                _elements.Add(element);
                if (element.Id >= 0) {
                    _elementsWithId.Add(element.Id, element);
                }
            }
            public bool TryGet(int id, out AEditableElement element) {
                return _elementsWithId.TryGetValue(id, out element);
            }
            public bool TryGetParent(AEditableElement element, out AEditableElement parent) {

                int id;
                if (!element.IsChild || (id = element.Parent) == element.Id || !_elementsWithId.TryGetValue(id, out parent) || !parent.IsParent) {
                    parent = null;
                    return false;
                }
                return true;
            }
            public void Clear() {
                _elements.Clear();
                _elementsWithId.Clear();
            }

            internal DataTypeEnumerator<T> GetElementsWithDataType<T>() {
                return new DataTypeEnumerator<T>(_elements);
            }
        }
        private class SelectionRect {
            private const float ClickDeadzone = 20f;

            private EditorSelectionRect _rect;
            private ElementEditor _editor;
            //private bool _active;
            private SelectType _selectType;
            private AEditableElement _startElement;
            private Vector2 _screenMouseStart;
            private bool _clicked = false;
            private enum SelectType {
                Normal,
                Additive,
                Remove,
            }

            public SelectionRect(ElementEditor editor) {
                _editor = editor;
            }
            public bool PressAdditive() {
                return Press( SelectType.Additive);
            }
            public bool PressInverse() {
                return Press(SelectType.Remove);
            }
            public bool PressNormal() {
                return Press(SelectType.Normal);
            }
            private bool Press(SelectType selectType) {
                _selectType  = selectType;
                _startElement = _editor.FindElementToSelect(true);
                if (selectType == SelectType.Normal) {
                    if (_editor._selection.Count == 0 && _startElement == null) {
                        return false;
                    }
                    _editor._selection.Clear();
                }
                _screenMouseStart = _editor._visualHandler.ScreenMousePosition;
                _rect.OnPress();
                return true;
            }
            internal void OnDrag() {
                _rect.DrawScene();
                _editor.Editor.Repaint();
                _editor.Repaint();
            }
            public void Release() {
                AEditableElement editableElement;
                if (_rect.IsActive && Vector2.Distance(_screenMouseStart, _editor._visualHandler.ScreenMousePosition) > ClickDeadzone) {
                    switch (_selectType) {
                        default:
                            foreach (var e in GetIncludedElements()) {
                                _editor._selection.Add(e);
                            }
                            break;
                        case SelectType.Remove:
                            foreach (var e in GetIncludedElements()) {
                                _editor._selection.Remove(e);
                            }
                            break;
                    }
                }
                else if ((editableElement = _editor.FindElementToSelect(false, true, _startElement)) != null) {
                    if (_selectType == SelectType.Remove) {
                        _editor._selection.Remove(editableElement);
                    }
                    else {
                        _editor._selection.Add(editableElement);
                    }
                }
                else {
                    var picked = _editor.PickFromTemporaryEditorObjects();
                    if (picked != null) {
                        UnityEditor.Selection.activeObject = picked;
                    }
                }
                _rect.OnRelease();
                _editor.Editor.Repaint();
                _editor.Repaint();
            }
            public bool Active {
                get {
                    return _rect.IsActive;
                }
            }
            public IEnumerable<AEditableElement> GetIncludedElements() {
                
                for (int i = 0; i < _editor._elements.Count; i++) {
                    var e = _editor._elements[i];
                    if (Contains( this._editor.GetPosition(e))) {
                        yield return e;
                    }
                }
            }

            internal bool Contains(Vector3 pos) {
                return _rect.ContainsPoint(Handles.matrix.MultiplyPoint3x4(pos));
            }

            internal void DrawScene() {
                _rect.DrawScene();
            }

            
        }
        private class CopyData : Mobge.Serialization.BinarySerializationBase.ISurrogate {
            private List<Element> _elements;
            private Pose _selectionCenter;
            private object _copiedContext, _pastedContext;
            public bool HasElements => _elements.Count != 0;
            private BinarySerializationBase.Formatter _formatter;
            public CopyData() {
                _elements = new List<Element>();
                _formatter.surrogates.AddSurrogate(typeof(ElementReference), this);
                _idMap = new Dictionary<int, int>();
            }
            private struct Element {
                public Type type;
                public BinaryObjectData binaryData;
                public int id;
                public object buttonDescriptor;
            }
            private bool TryFindButton(ElementEditor editor, object descriptor, out NewButtonData button) {
                for (int i = 0; i < editor._newButtons.Count; i++) {
                    var b = editor._newButtons[i];
                    if (descriptor == b.Descriptor) {
                        button = b;
                        return true;
                    }
                }
                button = default(NewButtonData);
                return false;
            }
			public void SetElements(ElementEditor editor, IEnumerable<AEditableElement> elements) {
			    _elements.Clear();
                foreach (var e in elements) {
                    Element ee;
                    ee.type = e.DataObject.GetType();
                    ee.binaryData = new BinaryObjectData(e.DataObject, _formatter);
                    ee.id = e.Id;
                    ee.buttonDescriptor = e.ButtonDescriptor;
                    _elements.Add(ee);
                }
                _copiedContext = editor.EditingObject;
                _selectionCenter = editor.GetSelectionCenter();
               // previousLocalToWorldMatrix = editor._visualHandler.matrix;
            }
            struct AddedElement {
                public AEditableElement element;
                public BinaryObjectData sourceData;
            }
            private Dictionary<int, int> _idMap;
            public void PasteElements(ElementEditor editor, Vector3 position, bool preservePosition = false) {
                bool contextMismatch = false;
                List<AddedElement> newElements = new List<AddedElement>();
                for (int i = 0; i < _elements.Count; i++) {
                    var e = _elements[i];
                    if (!TryFindButton(editor, e.buttonDescriptor, out NewButtonData button)) {
                        contextMismatch = true;
                        continue;
                    }
                    var newElement = button.createElement();
                    AddedElement ae;
                    ae.element = newElement;
					ae.sourceData = e.binaryData;
                    newElements.Add(ae);
                    editor.AddElement(newElement);
                    if (e.id >= 0) {
                        _idMap[e.id] = newElement.Id;
                    }
                }
                _pastedContext = editor.EditingObject;
                editor._selection.Clear();
                for (int i = 0; i < newElements.Count; i++) {
                    var addedElement = newElements[i];
                    try {
                        addedElement.sourceData.UpdateValues(addedElement.element.DataObject, _formatter);
                    }catch {
                        Debug.LogError("sdF");
                    }
                    editor._selection.Add(addedElement.element);
                    addedElement.element.UpdateData();
                }
                if (((Event.current.modifiers & EventModifiers.Shift) != 0) || preservePosition) {
                    //paste with preserving world position
                    var localToWorldMatrix = editor._visualHandler.matrix;
                    editor.SetSelectionCenter(_selectionCenter.position);
                }
                else {
                    editor.SetSelectionCenter(position);
                }
                _idMap.Clear();
                if (contextMismatch) {
                    EditorApplication.delayCall += () => {
                        EditorUtility.DisplayDialog("Warning", "Some objects cannot be pasted because target context is not suitable for some of the copied objects.","ok");
                    };
                }
            }

            void BinarySerializationBase.ISurrogate.Deserialize(ref object obj, Dictionary<string, object> values) {
                var key = nameof(ElementReference.id);
                var fi = typeof(ElementReference).GetField(key);
                values.TryGetValue(key, out object id);
                int oldId = (int)id;
                if (!_idMap.TryGetValue(oldId, out int newId)) {
                    if (_copiedContext != _pastedContext) {
                        newId = -1;
                    }
                    else {
                        newId = oldId;
                    }
                }
                fi.SetValue(obj, newId);
                //@ref.Value = new ElementReference((int)values[nameof(@ref.Value.Id)]);
            }

            void BinarySerializationBase.ISurrogate.Serialize(object obj, Dictionary<string, object> values) {
                ElementReference @ref = (ElementReference)obj;
                values[nameof(ElementReference.id)] = @ref.id;
            }

            Type BinarySerializationBase.ISurrogate.FieldType(string key) {
                switch (key) {
                    case nameof(ElementReference.id):
                        return typeof(int);
                    default:
                        return typeof(object);
                }
            }
        }
        public class ElementSelection : IEnumerable<AEditableElement> {
            private HashSet<AEditableElement> _unsavables = new HashSet<AEditableElement>();
            private ElementEditor _editor;
            private ElementEditorSelection _data;
            private ElementEditorSelection Data {
                get {
                    if(_data == null) {
                        _data = ScriptableObject.CreateInstance<ElementEditorSelection>();
                        _data.hideFlags = HideFlags.DontSave;
                    }
                    return _data;
                }
            }
            public bool LogicMode {
                get => Data.logicMode;
                set {
                    var d = Data;
                    Undo.RecordObject(d, "logic mode");
                    d.logicMode = value;
                }
            }
            public int Count => Data.Selection.Count + _unsavables.Count;
            public ElementSelection(ElementEditor editor) {
                _editor = editor;
            }
            public void Clear(bool dontRecord = false) {
                var d = Data;
                if (!dontRecord) {
                    Undo.RecordObject(d, "selection clear");
                }
                _unsavables.Clear();
                d.Selection.Clear();
            }
            public AEditableElement ExclusiveEditElement {
                get => this[Data.exclusiveEditElement];
                set {
                    var d = Data;
                    Undo.RecordObject(d, "exclusive edit element");
                    if(value == null || value.Id < 0) {
                        d.exclusiveEditElement = -1;
                    }
                    else {
                        d.exclusiveEditElement = value.Id;
                    }
                }
            }
            private AEditableElement this[int id] {
                get {
                    _editor._elements.TryGet(id, out AEditableElement result);
                    return result;
                }
                set { /* set the specified index to value here */ }
            }
            public Enumerator GetEnumerator() {
                return new Enumerator(this);
            }

            IEnumerator<AEditableElement> IEnumerable<AEditableElement>.GetEnumerator() {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }

            internal bool Contains(AEditableElement element) {
                if(element.Id < 0) {
                    return _unsavables.Contains(element);
                }
                return Data.Contains(element.Id);
            }

            public bool Add(AEditableElement value, bool dontSave = false) {
                if (value == null) {
                    return false;
                }
                if(value.Id < 0) {
                    return _unsavables.Add(value);
                }
                else {
                    var d = Data;
                    if (!dontSave) {
                        Undo.RecordObject(d, "add selection");
                    }
                    return d.Add(value.Id);
                }
            }
            public bool Remove(AEditableElement value, bool dontSave = false) {
                if(value.Id < 0) {
                    return _unsavables.Remove(value);
                }
                var d = Data;
                if(!dontSave) {
                    Undo.RecordObject(d, "remove selection");
                }
                return d.Remove(value.Id);
            }
            public struct Enumerator : IEnumerator<AEditableElement> {
                private ElementSelection _selection;
                private bool _unsavables;
                private int _index;
                private HashSet<AEditableElement>.Enumerator _unsavableEnum;
                internal Enumerator(ElementSelection selection) {
                    _selection = selection;
                    _unsavables = true;
                    _index = -1;
                    _unsavableEnum = _selection._unsavables.GetEnumerator();
                }

                public AEditableElement Current {
                    get {
                        if (_unsavables) {
                            return _unsavableEnum.Current;
                        }
                        else {
                            return _selection[_selection.Data.Selection[_index]];
                        }
                    }
                }

                object IEnumerator.Current => Current;

                public void Dispose() {
                    _selection = null;
                    _unsavableEnum.Dispose();
                }

                public bool MoveNext() {
                    if (_unsavables) {
                        if (_unsavableEnum.MoveNext()) {
                            return true;
                        }
                        else {
                            _unsavables = false;
                            return MoveDataNext();
                        }
                    }
                    return MoveDataNext();
                }
                private bool MoveDataNext() {
                    _index++;
                    return _selection.Data.Selection.Count > _index;
                }

                public void Reset() {
                    _unsavables = true;
                    _index = -1;
                    _unsavableEnum.Dispose();
                    _unsavableEnum = _selection._unsavables.GetEnumerator();
                }
            }
        }
        private class SelectionPath {
            private Queue<int> _ids = new Queue<int>();
            public bool Exists {
                get {
                    return _ids.Count > 0;
                }
            }

            public void AddId(int id) {
                if (id >= 0) {
                    _ids.Enqueue(id);
                }
            }

            public int Consume() {
                if(_ids.Count > 0) {
                    int next = _ids.Dequeue();
                    return next;
                }
                return -1;
            }
        }
        private enum RefreshType {
            None,
            Deep,
            Shallow,
        }

        public struct PlaneFeatures {
            private Plane _plane;
            private PlaneMode _mode;
            private Plane _lastPlane;
            public PlaneFeatures(Plane plane, PlaneMode mode) {
                _plane = plane;
                _mode = mode;
                _lastPlane = _plane;
            }
            public PlaneFeatures(Plane plane) : this(plane, PlaneMode.Static) {
            }
            public Plane GetLocalPlane(Vector3 localCenter) {
                {
                    switch (_mode) {
                        default:
                        case PlaneMode.Static:
                            return _plane;
                        case PlaneMode.Automatic:
                            var cam = Camera.current;
                            if (cam == null) {
                                return _lastPlane;
                            }

                            var cameraDir = cam.cameraToWorldMatrix.MultiplyVector(new Vector3(0, 0, 1));
                            cameraDir = Handles.matrix.inverse.MultiplyVector(cameraDir);
                            //Debug.DrawLine(center, cameraDir + center);
                            _lastPlane = new Plane(cameraDir, localCenter);
                            return _lastPlane;

                    }
                }
            }
        }
        public enum PlaneMode {
            Static = 0,
            Automatic = 1,
        }
    }
    public class ElementEditorSelection : ScriptableObject {
        public int exclusiveEditElement = -1;
        public bool logicMode = false;
        [SerializeField]
        private List<int> _selection = new List<int>();
        public List<int> Selection => _selection;
        public bool Contains(int id) {
            for (int i = 0; i < _selection.Count; i++) {
                if (_selection[i] == id) {
                    return true;
                }
            }
            return false;
        }
        internal bool Remove(int value) {
            return _selection.Remove(value);
        }
        internal bool Add(int value) {
            if (Contains(value)) {
                return false;
            }
            _selection.Add(value);
            return true;
        }
    }
}
