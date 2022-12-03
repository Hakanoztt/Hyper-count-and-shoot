using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(PolygonComponent))]
    public class EPolygonComponent : EComponentDefinition
    {
        private static readonly PointEditor<Corner> s_pointEditor = 
            new PointEditor<Corner>(
                ToVector3, 
                UpdateCorner, 
                new PointEditor<Corner>.VisualSettings() {
                    lineWidth = 3f, 
                    outlineWidth = 1.5f, 
                    mode = PointEditor<Corner>.Mode.Path,
                });
        private static Vector3 ToVector3(Corner c) => c.position;
        private static void UpdateCorner(ref Corner t, Vector3 position) {
            t.position = position;
        }
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as PolygonComponent.Data, this);
        }
        public class Editor : EBasePlatformComponent.Editor<PolygonComponent.Data> {
            private readonly EPolygonComponent _editor;
            public Editor(PolygonComponent.Data component, EPolygonComponent editor) : base(component, editor) {
                _editor = editor;
            }

            private void EnsureObject() {
                if (DataObjectT.polygons == null) {
                    DataObjectT.polygons = PolygonComponent.Data.DefaultPolygon;
                }
            }



            public override void DrawGUILayout() {
                EnsureObject();
                base.DrawGUILayout();
                
                var comp = DataObjectT;
                DataObjectT.subdivisionCount = EditorGUILayout.IntSlider("subdivision count", DataObjectT.subdivisionCount, 0, 32);
                if(comp.subdivisionCount > 1){
               		comp.cubicness = EditorGUILayout.Slider("cubicness", comp.cubicness, 0f, 1f);
               	}
                var s = s_pointEditor.SelectedPolygon;
                EditorGUILayout.LabelField("count: " + comp.GetPolygons().Length);
                EditorLayoutDrawer.CustomArrayField("polygons", ref comp.polygons, (rects, polygon) => {
                    polygon.noCollider = EditorGUI.Toggle(rects.NextRect(), "no collider", polygon.noCollider);
                    polygon.skinScale = EditorGUI.FloatField(rects.NextRect(), "skin scale", polygon.skinScale);
                    polygon.height = EditorGUI.FloatField(rects.NextRect(), "height", polygon.height);
                    if (GUI.Button(rects.NextRect(), "Reverse")) {
                        polygon.corners.ReverseDirection();
                    }
                    if (GUI.Button(rects.NextRect(), "Shift Right")) {
                        polygon.corners.Shift(1);
                    }
                    if (GUI.Button(rects.NextRect(), "Shift Left")) {

                        polygon.corners.Shift(-1);
                    }
                    return polygon;
                }, ref s);
                s_pointEditor.SelectedPolygon = s;
            }



            public override bool SceneGUI(in SceneParams @params) {
                EnsureObject();
                bool edited = false;
                var comp = DataObjectT;
                if (comp.polygons == null) return false;
                var mat = ElementEditor.BeginMatrix(@params.matrix);
                Corner[][] corners = new Corner[comp.polygons.Length][];
                for(int i = 0; i < corners.Length; i++) {
                    corners[i] = comp.polygons[i].corners;
                }
                s_pointEditor.HandleMoveCenter = MoveCenter;
                edited = s_pointEditor.OnSceneGUI(corners, editMode && @params.solelySelected);
                s_pointEditor.HandleMoveCenter = null;
                for(int i = 0; i < corners.Length; i++) {
                    comp.polygons[i].corners = corners[i];
                }
                ElementEditor.EndMatrix(mat);
                return edited && editMode;
            }

            private Vector3 MoveCenter(Vector3 dif) {
                return MoveCenterByLocal(dif);
            }


        }
    }
}