using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace Mobge {
    [CustomEditor(typeof(BezierRepeaterComponent))]
    public class EBezierRepeaterComponent : EComponentDefinition {



        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as BezierRepeaterComponent.Data, this);
        }
        public class Editor : EditableElement<BezierRepeaterComponent.Data> {

            private readonly EBezierPath _pathEditor = new EBezierPath();
            private bool _editMode;

            private void EnsureObject() {

                DataObjectT.repeater.EnsurePath();
            }

            //private bool _editMode = false;
            public Editor(BezierRepeaterComponent.Data component, EComponentDefinition editor) : base(component, editor) {
            }
            public override Transform CreateVisuals() {
                var go = new GameObject("bezier repeater").transform;
                return go;
            }
            public static void UpdateEditorVisuals(Transform parent, in BezierRepeaterComponent.Repeater repeater) {
                var to = TemporaryEditorObjects.Shared;
                for (int i = 0; i < parent.childCount; i++) {
                    var ins = parent.GetChild(i);
                    if (to.PrefabCache.ContainsInstance(ins)) {
                        to.PrefabCache.Push(ins);
                        ins.SetParent(to.transform, false);
                    }
                    else {
                        ins.gameObject.DestroySelf();
                        i--;
                    }
                }

                if (repeater.IsValid()) {
                    var e = new BezierRepeaterComponent.PoseEnumerator(repeater, Pose.identity);
                    while (e.MoveNext()) {
                        var ins = to.PrefabCache.Pop(repeater.GetRandomReference().transform);
                        ins.SetParent(parent, false);
                        to.SetHideFlagsForAdding(ins);
                        var p = e.Current;
                        ins.localPosition = p.position;
                        ins.localRotation = p.rotation;
                    }
                }
            }
            public override void UpdateVisuals(Transform instance) {
                base.UpdateVisuals(instance);
                if (instance) {
                    var data = DataObjectT;
                    UpdateEditorVisuals(instance, data.repeater);
                }
            }
            public override void DrawGUILayout() {
                base.DrawGUILayout();
                EnsureObject();
                _editMode = ExclusiveEditField("Edit On Scene");
                var data = DataObjectT;
                _pathEditor.OnInspectorGUI(data.repeater.path);
            }
            public override bool SceneGUI(in SceneParams @params) {
                bool enabled = @params.solelySelected;
                bool edited = false;
                var matrix = ElementEditor.BeginMatrix(@params.matrix);

                var data = DataObjectT;

                if (enabled && _editMode) {
                    edited = _pathEditor.OnSceneGUI(data.repeater.path) || edited;
                }

                ElementEditor.EndMatrix(matrix);
                return enabled && edited;
            }
        }
    }
}
             