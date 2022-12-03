using System;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static Mobge.InspectorExtensions;

namespace Mobge.Core
{
	[CustomEditor(typeof(PieceComponent))]
	public class EPieceComponent : EComponentDefinition
	{
		public override EditableElement CreateEditorElement(BaseComponent dataObject)
		{
			return new Editor(dataObject as PieceComponent.Data, this);
		}

		public class Editor : EditableElement<PieceComponent.Data>
		{
			private bool _editMode;
			private ElementEditor _elementEditor;
			private bool _deleteMode;
			private bool _showIOSettings;
			private Piece.InnerConnections _innerConnections;

			public Editor(PieceComponent.Data component, EPieceComponent editor) : base(component, editor)
			{
				_elementEditor = ElementEditor.NewForScene(null, new Plane(new Vector3(0, 0, -1), 0));
				EnsureEditorInnerConnections();
			}

			private void EnsureEditorInnerConnections()
			{
				_innerConnections = new Piece.InnerConnections {
					piece = DataObjectT.piece
				};
			}
			public override void DrawGUILayout()
			{

				_editMode = ExclusiveEditField("edit piece");

				var guiEnabled = GUI.enabled;
				GUI.enabled = guiEnabled && !_editMode;

				var comp = DataObjectT;

                EditorGUI.BeginChangeCheck();
                comp.piece = (Piece)CustomFields.LabelPicker.DrawLabeledObjectPicker(
	                "piece", 
	                comp.piece,
	                level.PieceType,
	                null,
	                false);
				if (EditorGUI.EndChangeCheck() || comp.piece == null) {
					ReleaseExclusiveEdit();
					_editMode = false;
                }
                comp.position = EditorGUILayout.Vector3Field("position", comp.position);
                comp.rotation.eulerAngles = EditorGUILayout.Vector3Field("rotation", comp.rotation.eulerAngles);
				GUI.enabled = guiEnabled;
				if (comp.piece) {
					comp.piece.editorGrid = EditorLayoutDrawer.ObjectField("grid", comp.piece.editorGrid, false);
				}
                if (comp.piece) {
                    _elementEditor.Editor = ElementEditor.Editor;
                    _elementEditor.EditingObject = comp.piece;
					EditorGUI.BeginChangeCheck();
					if (EditorGUI.EndChangeCheck()) {
						_deleteMode = !_editMode;
						_elementEditor.RefreshContent(true);
						_deleteMode = false;
					}
					if (_editMode) {
						_elementEditor.InspectorField();
					} else {
						MetaLogicField(true);
					}
				}
			}

			private int selectionInput = -1;
			private int selectionOutput = -1;
			private void MetaLogicField(bool divide)
			{
                var comp = DataObjectT;
				_showIOSettings = FoldoutFieldInTheBox(_showIOSettings, "Logic input/output settings: " + comp.piece.name, divide);
				if (_showIOSettings) {
					using (new GUILayout.VerticalScope("Box")) {
						LogicFieldSelector("in connection", ref comp.piece.inputSlotProperties,
						ref selectionInput, EditorColors.PastelOliveGreen, true);
						LogicFieldSelector("out connection", ref comp.piece.outputSlotProperties,
							ref selectionOutput, EditorColors.PastelOrange, true);
					}
				}
			}
			private void LogicFieldSelector(string connectionTitle, ref Piece.SlotInfo[] slotInfos, ref int selection, in Color color, bool showReturn) {
				using (Scopes.GUIBackgroundColor(color)) {
					EditorLayoutDrawer.CustomArrayField(connectionTitle, ref slotInfos, (layout, slot) => {
						slot = slot ?? new Piece.SlotInfo();
						var r = layout.NextRect();
						var rname = LayoutHelper.DivideLayoutRectHorizontallyWithPercentage(r, 0, 38);
						slot.name = EditorGUI.TextField(rname, slot.name);
						if (showReturn) {
							var rtype1 = LayoutHelper.DivideLayoutRectHorizontallyWithPercentage(r, 40, 28);
							var rtype2 = LayoutHelper.DivideLayoutRectHorizontallyWithPercentage(r, 70, 28);
							slot.type = (Piece.SlotType)EditorGUI.EnumPopup(rtype1, slot.type);
							slot.returnType = (Piece.SlotType)EditorGUI.EnumPopup(rtype2, slot.returnType);
						} else {
							var rtype1 = LayoutHelper.DivideLayoutRectHorizontallyWithPercentage(r, 40, 58);
							slot.type = (Piece.SlotType)EditorGUI.EnumPopup(rtype1, slot.type);
						}
						return slot;
					}, ref selection);
				}
			}
			public override bool SceneGUI(in SceneParams @params)
			{
				if (_editMode) {
                    _elementEditor.Matrix = @params.matrix;
					_elementEditor.grid = DataObjectT.piece.editorGrid;
					_elementEditor.SceneGUI(UpdateElements);
				}
                else {
                    var mm = _elementEditor.BeginMatrix(@params.matrix);
                    var e =_elementEditor.AllElements;
                    SceneParams prms;
                    prms.selected = false;
                    prms.solelySelected = false;


					var ee = _elementEditor.AllElements;
					int count = 0;
					while (ee.MoveNext()) {
						count++;
					}
					while (e.MoveNext()) {
                        var el = e.Current;
                        var ps = _elementEditor.GetPose(el);
                        prms.position = ps.position;
                        prms.rotation = ps.rotation;
                        prms.matrix = _elementEditor.GetMatrix(el);
                        el.SceneGUI(prms);
                    }
                    _elementEditor.EndMatrix(mm);
                }
                var t = Event.current.type;
                return _editMode && (t == EventType.Used || t == EventType.MouseUp);
            }
			private bool HandleDeselect()
			{
				ReleaseExclusiveEdit();
				_elementEditor.Repaint();
				_editMode = false;
				return true;
			}
			public override void UpdateData()
			{
				base.UpdateData();
                var comp = DataObjectT;
				if (comp.piece) {
                    EditorExtensions.SetDirty(comp.piece);
				}
			}
			private void UpdateElements() {
                var comp = DataObjectT;
                if (!comp.piece) {
					return;
				}
				if (_deleteMode) {
					return;
				}
				var passiveMode = _elementEditor.IsPassiveAddModeEnabled(out Vector3 poffset);
				if (!passiveMode) {
					AddButtons(_elementEditor, level, comp.piece);
				}
				EComponentDefinition.UpdateElements(_elementEditor, level, new Piece.PieceRef {
					piece = comp.piece,
					offset = Vector3Int.zero
				});
                var pih = new PieceInfoHub(comp.piece, comp.piece.name, this);
                pih.DataObjectT = _innerConnections;

                _elementEditor.AddElement(pih);
			}
			public override Transform CreateVisuals()
			{
                var comp = DataObjectT;

                if (_editMode) return null;
				if (comp.piece == null) return null;
				var tr = _elementEditor.CreateVisuals(UpdateElements, comp.piece.name);
				
				return tr;
			}
			public override void UpdateVisuals(Transform instance)
			{
				if (_editMode) {
					if(instance) {
						instance.gameObject.DestroySelf();
					}
				}
				else {
					_elementEditor.ClearVisuals();
				}
			}
			public override string ToString()
			{
				return "(" + Position + ", " + DataObjectT.piece + ")";
			}

			/// <summary>
			/// Temporary piece display element; makes editor behave properly while inside itself
			/// Draws logic connections selectable element and metalogic inspector while Piece is in edit mode, in itself.
			/// </summary>
			private class PieceInfoHub : AEditableElement<Piece.InnerConnections>
			{
				private readonly Piece _piece;
				private Editor _editor;
				public PieceInfoHub(Piece piece, object buttonDescriptor, Editor editor) : base(buttonDescriptor)
				{
					_piece = piece;
					_editor = editor;
					Id = Piece.InnerConnections.ID;
				}
				public override Texture2D IconTexture => _editor.IconTexture;
				public override Vector3 Position { get => Vector3.zero; set => value = Vector3.zero; }
				public override string Name => _piece.name;
				public override bool Delete()
				{
					_editor.Delete();
					return true;
				}
				public override void InspectorGUILayout()
				{
					_editor.MetaLogicField(false);
				}
			}
		}
	}
}