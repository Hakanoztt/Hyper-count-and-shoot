using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.Components {
    
    public class EBasePlatformComponent : EComponentDefinition {
        public abstract class Editor <T> : EditableElement<T> where T : BasePlatformComponent<T>.Data {
            protected Editor(T component, EComponentDefinition editor) : base(component, editor) {
            }
            private IPolygonRenderer _renderer;
            protected bool editMode;
            public override void DrawGUILayout() {
                var comp = DataObjectT;
                EditorGUI.BeginChangeCheck();
                if (level.decorationSet != null && level.decorationSet.RuntimeKeyIsValid()) {
                    comp.visualsIndex = EditorLayoutDrawer.Popup("skin", level.decorationSet.LoadedAsset.PolygonVisualizers, comp.visualsIndex, "none");
                }
                if (comp.polygonVisualizer == null) {
                    comp.polygonVisualizer = new AssetReferencePolygonVisualizer();
                }
                comp.polygonVisualizer.SetEditorAsset((UnityEngine.Object)InspectorExtensions.CustomFields.LabelPicker.DrawLabeledObjectPicker<IPolygonVisualizer>("Override skin", comp.polygonVisualizer.EditorAsset));
                if (EditorGUI.EndChangeCheck()) {
					if(_renderer != null && _renderer.Transform) {
						// UpdateVisuals(_renderer.Transform);
                        DestroyImmediate(_renderer.Transform.gameObject);
					}
				}
				comp.physicsMaterial = EditorLayoutDrawer.ObjectField("physics material", comp.physicsMaterial, false);
                editMode = ExclusiveEditField("edit on scene");
                comp.position = EditorGUILayout.Vector3Field("position", comp.position);
                comp.Rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("rotation", comp.Rotation.eulerAngles));
                comp.color = EditorGUILayout.ColorField("color", comp.color);
                comp.Mode = (PlatformWorkMode)EditorGUILayout.EnumPopup("mode", comp.Mode);
                if (comp.Mode != PlatformWorkMode.Fixed) {
                    comp.disableOnStart = EditorGUILayout.Toggle("disable on start", comp.disableOnStart);
                    comp.layer = EditorGUILayout.LayerField("layer", comp.layer);
                }
                if(comp.Mode == PlatformWorkMode.Dynamic || comp.Mode == PlatformWorkMode.Kinematic) {
                    comp.collisionDetectionMode =
                        (CollisionDetectionMode2D)EditorGUILayout.EnumPopup("collision type", comp.collisionDetectionMode);
                    comp.mass = EditorGUILayout.FloatField("mass", comp.mass);
                    comp.drag = EditorGUILayout.FloatField("drag", comp.drag);
                    comp.gravityScale = EditorGUILayout.FloatField("gravity scale", comp.gravityScale);
                    comp.bodyConstrainst = (RigidbodyConstraints2D)EditorGUILayout.EnumFlagsField("constrainst", comp.bodyConstrainst);
                }
                if((comp.Mode != PlatformWorkMode.Visual && comp.Mode != PlatformWorkMode.Kinematic) && ((IChild)comp).Parent != -1) {
                    var p = this.ElementEditor.GetPose(this);
                    ((IChild)comp).Parent = -1;
                    this.ElementEditor.SetPose(this, p);
                }
                //base.DrawGUILayout();
            }
            public override Transform CreateVisuals() {
                if (DataObjectT.GetPolygons() == null) return null;
                _renderer = DataObjectT.CreateRenderer(level, null, Vector3.zero, Quaternion.identity, true);
                if (_renderer == null) return null;
                return _renderer.Transform;
            }
            public override void UpdateVisuals(Transform instance) {
                if (_renderer != null && _renderer.Transform) {
                    DataObjectT.UpdateRenderer(level, _renderer);
                }
            }
        }
    }
}
