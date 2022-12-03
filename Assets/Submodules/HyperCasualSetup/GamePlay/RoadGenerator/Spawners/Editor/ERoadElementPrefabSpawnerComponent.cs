using Mobge.Core;
using Mobge.HyperCasualSetup.RoadGenerator;
using System;
using UnityEditor;
using UnityEngine;

namespace Mobge.RoadGenerator {
    [CustomEditor(typeof(RoadElementPrefabSpawnerComponent))]
    public class ERoadElementPrefabSpawnerComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as RoadElementPrefabSpawnerComponent.Data, this);
        }
        public class Editor : Core.Components.EPrefabSpawnerComponent.PSEditor, IERoadElement {
            //private bool _editMode = false;
            private static bool _editingData;
            public override Vector3 Position {
                get {
                    return this.GetPosition(ElementEditor);
                }
                set {
                   base.Position = this.SetPosition(ElementEditor, value);
                }
            }

            public override Quaternion Rotation {
                get {
                    return this.GetRotation(ElementEditor);
                }
                set {
                    base.Rotation = this.SetRotation(ElementEditor, value);
                }
            }

            RoadElementData IERoadElement.Data { get => DataObjectT.roadElementData; set => DataObjectT.roadElementData = value; }
            public new RoadElementPrefabSpawnerComponent.Data DataObjectT {
                get => (RoadElementPrefabSpawnerComponent.Data)base.DataObjectT;
            }

            public Editor(RoadElementPrefabSpawnerComponent.Data component, EComponentDefinition editor) : base(component, editor) {
            }


            public override void DrawGUILayout() {
                base.DrawGUILayout();
                this.InspectorGUI("road data", ref _editingData, ElementEditor);
                
            }
            public override bool SceneGUI(in SceneParams @params) {
                bool changed = this.OnSceneGUI(ElementEditor);
                return base.SceneGUI(@params) || changed;
            }
        }

    }
}
             