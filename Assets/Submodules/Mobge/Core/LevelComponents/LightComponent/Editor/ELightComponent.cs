using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Mobge.Core.Components.LightComponent;

namespace Mobge.Core.Components
{
	[CustomEditor(typeof(LightComponent))]
	public class ELightComponent : EComponentDefinition
	{
		public override EditableElement CreateEditorElement(BaseComponent dataObject)
		{
			return new Editor(dataObject as LightComponent.Data, this);
		}
		public class Editor : EditableElement<LightComponent.Data>
		{
			private bool _editEnabled;
			private ElementEditor _elementEditor;
			private LightComponent.Data.LightRenderer _renderer;
			#region Overrides and constructor

			public Editor(LightComponent.Data component, ELightComponent editor) : base(component, editor)
			{
				_elementEditor = ElementEditor.NewForScene(editor, new Plane(new Vector3(0, 0, -1), 0));
			}
			public override object DataObject {
				get => base.DataObject;
				set {
					_elementEditor.RefreshContent(false);
					base.DataObject = value;
				}
			}
			public override bool SceneGUI(in SceneParams @params)
			{
				var comp = DataObjectT;
				if (@params.selected) {

					switch (comp.type) {
						case LightType.Spot:
							// todo draw spot light arc
							break;
						case LightType.Point:
							var temp = Handles.color;
							Handles.color = comp.color;
							Handles.DrawWireDisc(@params.position, Vector3.back, comp.range);
							Handles.color = temp;
							break;
					}

					if (_editEnabled) {
						_elementEditor.Matrix = Matrix4x4.Translate(@params.position);
					}
				}
				var t = Event.current.type;
				return _editEnabled && (t == EventType.Used || t == EventType.MouseUp);
			}
			public override void UpdateVisuals(Transform instance)
			{
				if (_renderer != null && _renderer.Transform) {
					var comp = DataObjectT;
					comp.UpdateRenderer(_renderer);
				}
			}
			public override Transform CreateVisuals()
			{
				_renderer = DataObjectT.CreateRenderer(Vector3.zero, null);
				return _renderer.Transform;
			}

			public override void DrawGUILayout()
			{
				var comp = DataObjectT;
				_editEnabled = ExclusiveEditField("edit on scene");
				if (_editEnabled) {
					_elementEditor.InspectorField();
					EditorGUI.BeginChangeCheck();
					using (new GUILayout.VerticalScope("Box")) {
						Position = EditorGUILayout.Vector3Field("Position", Position);
						comp.enabled = EditorGUILayout.Toggle("Enabled", comp.enabled);
						comp.type = (LightType)EditorGUILayout.EnumPopup("Light type", comp.type);
						switch (comp.type) {
							case LightType.Spot:
								comp.spotAngle = EditorGUILayout.FloatField("Spot Angle", comp.spotAngle);
								comp.spotAngle = Mathf.Clamp(comp.spotAngle, 1, 180);
								comp.range = EditorGUILayout.FloatField("Range", comp.range);
								break;
							case LightType.Point:
								comp.range = EditorGUILayout.FloatField("Range", comp.range);
								break;
						}
						
						comp.renderMode = (LightComponent.Data.RenderMode)EditorGUILayout.EnumPopup("Render mode", comp.renderMode);
						comp.color = EditorGUILayout.ColorField("Color", comp.color);
						comp.intensity = EditorGUILayout.FloatField("Intensity", comp.intensity);
						comp.indirectMultiplier = EditorGUILayout.FloatField("Bounciness", comp.indirectMultiplier);
						comp.shadows = (LightShadows)EditorGUILayout.EnumPopup("Shadows", comp.shadows);
					}
					if (EditorGUI.EndChangeCheck()) 
						UpdateVisuals(null);
				}
			}
			public override string Name => "Light component";

			#endregion Overrides and constructor
			private bool HandleDeselect()
			{
				ReleaseExclusiveEdit();
				_elementEditor.Repaint();
				_editEnabled = false;
				return true;
			}
		}
	}
}