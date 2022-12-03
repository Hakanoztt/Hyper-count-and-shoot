using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Mobge.Core.Components.DecorationComponent;
using Object = UnityEngine.Object;

namespace Mobge.Core.Components {
	[CustomEditor(typeof(DecorationComponent))]
	public class EDecorationComponent : EComponentDefinition {
		private string _filter;
		public override EditableElement CreateEditorElement(BaseComponent dataObject) {
			return new Editor(dataObject as DecorationComponent.Data, this);
		}
		public override ElementEditor.GlobalComponentSettingsField GetOptionsGUI() {
			return OptionsGUI;
		}
		private void OptionsGUI(LayoutRectSource rects, ElementEditor editor) {
			var e = editor.GetElementsWithType<Editor>();
			if (GUI.Button(rects.NextRect(), "select all")) {
				editor.Selection.Clear();
				while (e.MoveNext()) {
					editor.Selection.Add(e.Current);
				}
				editor.ClosePopup();

				editor.FocusToSelection();
			}
			_filter = EditorGUI.TextField(rects.NextRect(), "filter", _filter);
			if (!string.IsNullOrEmpty(_filter)) {
				e.Reset();
				while (e.MoveNext()) {
					var de = e.Current;
					var d = de.DataObjectT;
					if (!string.IsNullOrEmpty(d.name) && InspectorExtensions.TextMatchesSearch(d.name, _filter)) {
						if (GUI.Button(rects.NextRect(), d.name)) {
							editor.SingleSelection = de;
							editor.ClosePopup();
							editor.FocusToSelection();
						}
					}
				}
			}
		}
		public class Editor : EditableElement<DecorationComponent.Data> {
			#region Decoration root element's editor
			private bool _editEnabled;
			private DecorationComponent.RendererList _renderer;
			private readonly ElementEditor _elementEditor;
			private Mode _activeMode;
			private enum Mode { Position, Rotate, Scale }
			// private AutoFocusModule _focusModule;
			private bool _handlesEnabled = true;

			#region Overrides and constructor
			public Editor(DecorationComponent.Data component, EDecorationComponent editor) : base(component, editor) {
				_elementEditor = ElementEditor.NewForScene(editor, new Plane(new Vector3(0, 0, -1), 0));
			}
			public override bool HandlesEnabled => _handlesEnabled;
			public override object DataObject {
				get => base.DataObject;
				set {
					_elementEditor.RefreshContent(false);
					base.DataObject = value;
				}
			}
			public override bool SceneGUI(in SceneParams @params) {
				var comp = DataObjectT;
				if (@params.selected) {
					if (_editEnabled && @params.selected) {
						_elementEditor.Matrix = Matrix4x4.TRS(@params.position, comp.rotation, comp.scale);
						_elementEditor.SceneGUI(UpdateElements);
					}
				}
				var t = Event.current.type;
				return _editEnabled && (t == EventType.Used || t == EventType.MouseUp);
			}
			public override void UpdateVisuals(Transform instance) {
				if (_renderer != null && _renderer.Transform) {
					var comp = DataObjectT;
					comp.UpdateRenderer(level, _renderer);
					_renderer.Transform.localScale = comp.scale;
				}
			}
			public override Transform CreateVisuals() {
				_renderer = DataObjectT.CreateRenderers(level, Vector3.zero, false, null);
				return _renderer != null ? _renderer.Transform : null;
			}

			private VisualSet GetVisualSet() {
				var comp = DataObjectT;
				return comp.GetVisualSet(level);
			}
			public override void DrawGUILayout() {
				var comp = DataObjectT;
				EnsureObject();

				_editEnabled = ExclusiveEditField("edit on scene");
				var guiEnabled = GUI.enabled;
				GUI.enabled = guiEnabled && !_editEnabled;

				comp.IsFinal = UnityEngine.GUILayout.Toggle(comp.IsFinal, "BackGround ?");
				EditorGUI.BeginChangeCheck();
				comp.name = EditorGUILayout.TextField("name", comp.name);
				if (level.decorationSet != null && level.decorationSet.RuntimeKeyIsValid()) {
					comp.setId = EditorLayoutDrawer.Popup("Visual Set", level.decorationSet.LoadedAsset.VisualSets, comp.setId);
				}
				if (comp.visualSet == null) {
					comp.visualSet = new DecorationComponent.AssetReferenceVisualSet();
				}
				comp.visualSet.SetEditorAsset(EditorLayoutDrawer.ObjectField<VisualSet>("Override Visual Set", comp.visualSet.EditorAsset, false));
				if (EditorGUI.EndChangeCheck()) {
					// RecreateCache(DataObjectT);
				}
				using (new GUILayout.VerticalScope("Box")) {
					Position = EditorGUILayout.Vector3Field("Position", Position);
					comp.rotation = InspectorExtensions.CustomFields.Rotation.DrawAsVector3(comp.rotation);
					comp.scale = InspectorExtensions.CustomFields.Scale.Draw(comp.scale);
				}
				GUI.enabled = guiEnabled;
				if (_editEnabled)
					_elementEditor.InspectorField();
			}
			public override string Name => (DataObjectT.IsFinal ? "Background" : "Foreground") + " Decoration Group";
			#endregion
			private void EnsureObject() {
				var comp = DataObjectT;
				if (comp.nodes == null) {
					comp.nodes = DecorationComponent.Node.NewEmptyData();
				}
			}
			private readonly object _decorationDescriptor = "DecorationDescriptor";
			private void UpdateElements() {
				var comp = DataObjectT;
				var bd = new ElementEditor.NewButtonData("New Decoration", 0, CreateDecorationElement, _decorationDescriptor);
				_elementEditor.AddButtonData(bd);
				for (int i = 0; i < comp.nodes.Length; i++)
					_elementEditor.AddElement(new VisualDecorationElement(comp.nodes[i], bd.Descriptor, this, i));
			}
			public AEditableElement CreateDecorationElement() {
				var comp = DataObjectT;
				var length = comp.nodes.Length;
				var node = new DecorationComponent.Node {
					transformInfo = new DecorationComponent.TransformInfo(base.Position, Quaternion.identity, Vector3.one),
					decorId = 0
				};
				ArrayUtility.Add(ref comp.nodes, node);

				var visualSet = GetVisualSet();
				var visual = comp.GetVisual(visualSet, 0);
				_renderer.renderers.Add(visual.Visualize(visualSet, node, false, _renderer.Transform));

				UpdateData();
				return new VisualDecorationElement(comp.nodes[length], _decorationDescriptor, this, length);
			}
			#endregion

			#region Decoration nested element's editor (aka node)
			private class VisualDecorationElement : AEditableElement<DecorationComponent.Node> {
				private readonly Editor _editor;
				private int NodeIndex => ArrayUtility.IndexOf(_editor.DataObjectT.nodes, DataObjectT);
				private readonly InspectorExtensions.PreviewPicker _previewPicker = new InspectorExtensions.PreviewPicker();
				#region Overrides and constructor
				public VisualDecorationElement(DecorationComponent.Node node, object buttonDescriptor, Editor editor, int id) : base(buttonDescriptor) {
					this.Id = id;
					DataObjectT = node;
					_editor = editor;
				}
				public override string Name => "Decoration Element";
				public override void UpdateVisuals(Transform instance) {
					if (_editor._renderer != null && _editor._renderer.Transform) {
						_editor.DataObjectT.UpdateRenderer(_editor.level, _editor._renderer);
					}
				}
				public override Vector3 Position {
					get => DataObjectT.transformInfo.position;
					set {
						DataObjectT.transformInfo.position = value;
						var i = NodeIndex;
						if (i > -1) {
							_editor.DataObjectT.nodes[i] = DataObjectT;
						}
					}
				}
				public override bool Delete() {
					var i = NodeIndex;
					ArrayUtility.RemoveAt(ref _editor.DataObjectT.nodes, i);

					var renderer = _editor._renderer.renderers[i];
					renderer.Transform.gameObject.DestroySelf();
					_editor._renderer.renderers.RemoveAt(i);

					_editor.UpdateData();
					_editor._elementEditor.RefreshContent(true);
					return true;
				}
				public override void UpdateData() {
					var i = NodeIndex;
					if (i > -1) {
						var comp = DataObjectT;
						_editor.DataObjectT.nodes[i] = comp;
						_editor.UpdateData();
					}
				}
				public override bool SceneGUI(in SceneParams @params) {
					var comp = DataObjectT;
					if (@params.selected) {
						Vector3 wCurPos;
						Vector3 currentPosition;
						Matrix4x4 tm;
						switch (Tools.current) {
							case Tool.Move:
								_editor._handlesEnabled = true;
								break;
							case Tool.Rotate:
								_editor._handlesEnabled = false;
								tm = Handles.matrix;
								Handles.matrix = Matrix4x4.identity;
								currentPosition = comp.transformInfo.position;
								wCurPos = tm.MultiplyPoint3x4(currentPosition);
								comp.transformInfo.rotation =
									Handles.RotationHandle(
										comp.transformInfo.rotation,
										wCurPos);
								Handles.matrix = tm;
								break;
							case Tool.Scale:
								_editor._handlesEnabled = false;
								tm = Handles.matrix;
								Handles.matrix = Matrix4x4.identity;
								currentPosition = comp.transformInfo.position;
								wCurPos = tm.MultiplyPoint3x4(currentPosition);
								var curRot = comp.transformInfo.rotation;
								var wCurRot = tm.rotation * curRot;

								//because middle of this scale handle does not work correctly,
								//the composite scale handle drawn below
								comp.transformInfo.scale = Handles.ScaleHandle(
									comp.transformInfo.scale,
									wCurPos,
									wCurRot,
									HandleUtility.GetHandleSize(wCurPos));

								var oldCompositeScale = comp.transformInfo.scale.x;
								var newCompositeScale = Handles.ScaleValueHandle(
									oldCompositeScale,
									wCurPos,
									wCurRot,
									HandleUtility.GetHandleSize(wCurPos) * 1.1f,
									Handles.CubeHandleCap,
									0f);
								var compositeScaleMultiplierDelta = newCompositeScale / oldCompositeScale;
								comp.transformInfo.scale *= compositeScaleMultiplierDelta;

								Handles.matrix = tm;
								break;
						}
					}
					var t = Event.current.type;
					return @params.selected && (t == EventType.Used || t == EventType.MouseUp);
				}
				public override void InspectorGUILayout() {
					var comp = DataObjectT;
					using (new GUILayout.VerticalScope("Box")) {
						comp.transformInfo.position = EditorGUILayout.Vector3Field("Position", comp.transformInfo.position);
						comp.transformInfo.rotation = InspectorExtensions.CustomFields.Rotation.DrawAsVector3(comp.transformInfo.rotation);
						comp.transformInfo.scale = InspectorExtensions.CustomFields.Scale.Draw(comp.transformInfo.scale);
						comp.color = EditorGUILayout.ColorField("Color", comp.color);
						comp.flipX = EditorGUILayout.Toggle("FlipX", comp.flipX);
						comp.flipY = EditorGUILayout.Toggle("FlipY", comp.flipY);
						comp.receiveShadows = EditorGUILayout.Toggle("Receive Shadows", comp.receiveShadows);
						var visualSet = _editor.GetVisualSet();
						if (visualSet != null) {
							comp.materialId = EditorLayoutDrawer.Popup("Material", visualSet.materials, comp.materialId);
							comp.decorId = InspectorExtensions.AutoIndexWrapper.WrapExecute(visualSet.visuals, comp.decorId, (list, i) => {
								List<Object> objects = new List<Object>();
								foreach (var v in list) {
									if (v.Reference == null) {
										EditorGUILayout.HelpBox("Sprite reference is null on visual set, maybe someone deleted a sprite from the sprite sheet", MessageType.Error);
										return comp.decorId;
									}
									objects.Add(v.Reference);
								}
								return _previewPicker.Draw("Visual", objects, i, true);
							});
						}
					}
				}
				#endregion
			}
			#endregion
		}
	}
}