using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace Mobge.Fx {
    [CustomEditor(typeof(LiquidSimulationComponent))]
    public class ELiquidSimulationComponent : EComponentDefinition {

        private static PointEditor<Vector2> s_pointEditor = new PointEditor<Vector2>(v => v, (ref Vector2 v, Vector3 d) => v = d);

        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as LiquidSimulationComponent.Data, this);
        }
        public class Editor : EditableElement<LiquidSimulationComponent.Data> {
            private bool _editMode;
            private int _selectedPolygon;
            public Editor(LiquidSimulationComponent.Data component, EComponentDefinition editor) : base(component, editor) {
            }
            // In this method, implement the logic you would normally implement under OnInspectorGUI() 
            public override void DrawGUILayout() {
                // Inspector: Boiler plate for exclusive editing of the element
                base.DrawGUILayout();
                var data = DataObjectT;
                var ge = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.Vector2Field("physical size", data.PhysicalSize);
                GUI.enabled = ge;
                EnsureData();
                s_pointEditor.OnInspectorGUI(ref _selectedPolygon, data.walls.Length, _editMode);
                _editMode = ExclusiveEditField("edit on scene");
            }
            private Vector2[][] ToDoubleArray(LiquidSimulationComponent.Polygon[] polygons) {
                Vector2[][] pols = new Vector2[polygons.Length][];
                for(int i = 0; i < pols.Length; i++) {
                    pols[i] = polygons[i].points;
                }
                return pols;
            }
            private void UpdateData(LiquidSimulationComponent.Polygon[] polygons, Vector2[][] pols) {
                var min = Mathf.Min(pols.Length, polygons.Length);
                for(int i = 0; i <  min; i++) {
                    polygons[i].points = pols[i];
                }
            }
            void EnsureData() {
                var data = DataObjectT;
                if (data.walls == null) {
                    data.walls = new LiquidSimulationComponent.Polygon[0];
                }
            }
            // In this method, implement the logic you would normally implement under OnSceneGUI() 
            public override bool SceneGUI(in SceneParams @params) {
                var data = DataObjectT;
                EnsureData();
                // Scene: Boiler plate for exclusive editing of the element
                bool enabled = @params.selected && _editMode;
                bool edited = false;
                var tempMat = ElementEditor.BeginMatrix(@params.matrix);

                var pols = ToDoubleArray(data.walls);
                edited = s_pointEditor.OnSceneGUI(pols, enabled);
                UpdateData(data.walls, pols);

                Handles.DrawSolidRectangleWithOutline(new Rect(-data.PhysicalSize * 0.5f, data.PhysicalSize), new Color(1,1,1,0.05f), Color.grey);
                ElementEditor.EndMatrix(tempMat);
                // Logic here. Explicit edit is on, do what you need
                // On Change set edited variable to true to save the data and update the visual

                return enabled && edited;
            }
        }
    }
}
