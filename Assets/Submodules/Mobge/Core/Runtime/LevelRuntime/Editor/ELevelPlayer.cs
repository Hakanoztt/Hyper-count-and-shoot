using UnityEngine;
using UnityEditor;
using System;
using Mobge.Utility;

namespace Mobge.Core {
    [CustomEditor(typeof(LevelPlayer))]
    public class ELevelPlayer : Editor {
        private LevelPlayer _go;
        private int _selectedPieceIndex = -1;
        private static ELevelPlayer s_current;
        public static ELevelPlayer Current => s_current;

        protected ElementEditor _elementEditor;
        public ElementEditor ElementEditor => _elementEditor;
        protected virtual void OnEnable() {
            s_current = this;
            _go = target as LevelPlayer;
            var eef = ExtraEditorFields.Shared;
            // following logic is necessary to prevent object duplication in temporary objects
            // when repeaetdly selecting level player
            if (eef.TryGetField(_go.gameObject, "elementEditor", out _elementEditor)) {
                _elementEditor.Editor = this;
                _elementEditor.RefreshContent(true);
            }
            else {
                _elementEditor = ElementEditor.NewForScene(this, new Plane(new Vector3(0, 0, -1), 0));
                eef.AttachField(_go.gameObject, "elementEditor", _elementEditor);
            }
            DefaultToolsVisibility.HideTools();
        }
        private void OnDisable() {
            _elementEditor.OnDeselect();
            if (s_current == this) {
                s_current = null;
            }
            DefaultToolsVisibility.UnHideTools();
        }
        protected void OnSceneGUI() {
            SceneGUI();
        }
        protected virtual void SceneGUI() {
            ElementSceneGUI();
        }
        private void ElementSceneGUI() {
            if (!_go.level) {
                return;
            }
            _elementEditor.UpdateMatrix(_go.transform, Vector3.zero);
            _elementEditor.SceneGUI(UpdateElements);
        }
        private void UpdateElements() {
            UpdateElements(this._go.level, _elementEditor);
        }
        protected virtual void UpdateElements(Piece selectedPiece, ElementEditor elements) {
            var passiveMode = _elementEditor.IsPassiveAddModeEnabled(out Vector3 poffset);
            if (!passiveMode) {
                EComponentDefinition.AddButtons(_elementEditor, _go.level, selectedPiece);
            }
            Piece.PieceRef pf;
            pf.piece = selectedPiece;
            pf.offset = Vector3Int.zero;
            EComponentDefinition.UpdateElements(_elementEditor, _go.level, pf);
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            SaveLevelEditorToSceneButton();
            if (!_go.level) {
                EditorGUILayout.LabelField("Please select level.");
            }
            EditorGUI.BeginChangeCheck();
            _go.level = (Level) EditorGUILayout.ObjectField("Level", _go.level, typeof(Level), true);
            if (EditorGUI.EndChangeCheck()) {
                if (!_go.level) _go.transform.DestroyAllChildren();
                _selectedPieceIndex = -1;
            }
            if (_go.level) {
                _go.level.GameSetup =
                    EditorGUILayout.ObjectField("Game Setup", _go.level.GameSetup, _go.level.GameSetupType, false) as
                        GameSetup;
                PieceEditor();
                _elementEditor.EditingObject = _go.level;
                _elementEditor.InspectorField();
            }
            _elementEditor.grid = _go.editorGrid;
            if (GUI.changed) {
                EditorExtensions.SetDirty(_go);
                if (_go.level) {
                    EditorExtensions.SetDirty(_go.level);
                }
            }
        }
        private void SaveLevelEditorToSceneButton() {
            var savedToScene = _go.gameObject.hideFlags == HideFlags.None;
            using (Scopes.GUIEnabled(!savedToScene)) {
                if (GUILayout.Button("Save Level Editor To Scene")) {
                    _go.gameObject.hideFlags = HideFlags.None;
                }
            }
        }
        #region Inspector Fields

        private void PieceEditor() {
            if (_go.level.decorationSet == null) {
                _go.level.decorationSet = new AssetReferenceDecorSet();
            }
            using (Scopes.GUIEnabled(!Application.isPlaying)) {
                if (!Application.isPlaying) {
                    if (_go.level.decorationSet != null && _go.level.decorationSet.RuntimeKeyIsValid()) {
                        _go.level.decorationSet.SetEditorAsset(
                            EditorLayoutDrawer.ObjectField<DecorationSet>("Decoration Set",
                                _go.level.decorationSet.LoadedAsset, false));
                    }
                    else {
                        _go.level.decorationSet.SetEditorAsset(
                            EditorLayoutDrawer.ObjectField<DecorationSet>("Decoration Set", null, false));
                    }
                }
                else {
                    EditorLayoutDrawer.ObjectField<DecorationSet>("Decoration Set", _go.level.decorationSet.LoadedAsset,
                        false);
                }
            }
            if (_go.level.decorationSet != null && _go.level.decorationSet.RuntimeKeyIsValid()) {
                if (!_go.level.decorationSet.editorAsset) {
                    EditorGUILayout.LabelField(nameof(_go.level.decorationSet) + " field must be set to edit piece.");
                    return;
                }
            }
            EditorGUILayout.BeginVertical("Box");
            var lrs = new LayoutRectSource();
            lrs.Reset(GUILayoutUtility.GetAspectRect(float.PositiveInfinity));
            lrs.ConvertToLayout();
            EditorGUILayout.EndVertical();
        }
        #endregion
        public struct SceneMode {
            public string name;
            public Action SceneGUI;
            public SceneMode(string name, Action SceneGUI) : this() {
                this.name = name;
                this.SceneGUI = SceneGUI;
            }
            public override string ToString() => name;
        }
    }
}