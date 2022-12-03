using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(BezierPlatformComponent))]
    public class EBezierPlatformComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as BezierPlatformComponent.Data, this);
        }
        public class Editor : EBasePlatformComponent.Editor<BezierPlatformComponent.Data> {
            public Editor(BezierPlatformComponent.Data component, EComponentDefinition editor) : base(component, editor) {
            }
            private readonly EBezierPath _pathEditor = new EBezierPath();
            private void EnsureObject() {
                if (DataObjectT.bezierData == null || DataObjectT.bezierData.Points == null) {
                    DataObjectT.bezierData = BezierPlatformComponent.Data.DefaultBezierPath;
                }
            }
            public override void DrawGUILayout() {
                EnsureObject();
                base.DrawGUILayout();
                DataObjectT.subdivisionCount = EditorGUILayout.IntSlider("subdivision count", DataObjectT.subdivisionCount, 1, 32);
                _pathEditor.OnInspectorGUI(DataObjectT.bezierData);
            }
            public override bool SceneGUI(in SceneParams @params) {
                EnsureObject();
                var edited = false;
                var mat = Handles.matrix;
                Handles.matrix = mat * @params.matrix;
                if (editMode && @params.solelySelected) {
                    edited = _pathEditor.OnSceneGUI(DataObjectT.bezierData);
                }
                else {
                    _pathEditor.DrawBezierLine(DataObjectT.bezierData);
                }
                Handles.matrix = mat;
                return editMode && edited;
            }
        }
    }
}
