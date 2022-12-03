using UnityEditor;
using UnityEngine;

namespace Mobge.Core.Components {

    [CustomEditor(typeof(PrefabSpawnerComponent))]
    public class EPrefabSpawnerComponent : EComponentDefinition {

        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new PSEditor(dataObject as PrefabSpawnerComponent.Data, this);
        }

        public class PSEditor : Editor<PrefabSpawnerComponent.Data> {

            public PSEditor(PrefabSpawnerComponent.Data dataObject, EComponentDefinition editor) : base(dataObject, editor) {}

            public override void DrawGUILayout() {
                base.DrawGUILayout();
                var data = DataObjectT;
                GUI.changed = FixComponent(ref data) || GUI.changed;
            }
        }

        public static bool FixComponent(ref PrefabSpawnerComponent.Data component) {
            if (component.res != null) {
                if (!(component.res is IComponentExtension)) {
                    var ce = component.res.GetComponent<IComponentExtension>();
                    if (ce != null) {
                        component.res = (Component) ce;
                        return true;
                    }
                }
            }
            return false;
        }

        public override ElementEditor.GlobalComponentSettingsField GetOptionsGUI() {
            return (rects, editor) => {
                if(GUI.Button(rects.NextRect(), new GUIContent("Fix All!"))) {
                    ComponentFixer.FixComponentData<PrefabSpawnerComponent.Data>(FixComponent);
                }
            };
        }

        private bool FixComponent(Piece piece, int id, ref PrefabSpawnerComponent.Data component) {
            return FixComponent(ref component);
        }

        public class Editor<T> : EditableElement<T> where T : PrefabSpawnerComponent.BaseData {
            public virtual bool DrawDefaultResInspector {
                get {
                    return true;
                }
            }
            public Editor(T dataObject, EComponentDefinition editor) : base(dataObject, editor) {
            }
            public override void UpdateVisuals(Transform instance) {
                base.UpdateVisuals(instance);
                if (instance) {
                    instance.localScale = DataObjectT.Scale;
                }
            }
            public override void DrawGUILayout() {
                base.DrawGUILayout();
                var data = DataObjectT;// as PrefabSpawnerComponent.Data;
                //data.position = EditorGUILayout.Vector3Field("position", data.position);
                data.Rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("rotation", data.Rotation.eulerAngles));
                data.Scale = EditorGUILayout.Vector3Field("scale", data.Scale);
                if (DrawDefaultResInspector && data is PrefabSpawnerComponent.Data d) {
                    d.res = (Component)InspectorExtensions.CustomFields.LabelPicker.DrawLabeledObjectPicker("res", d.res, typeof(Component));
                }
            }
            public override bool SceneGUI(in SceneParams @params) {
                if (@params.solelySelected) {
                    if (Tools.current == Tool.Scale) {
                        var m = ElementEditor.BeginMatrix(@params.matrix);
                        DataObjectT.Scale = Handles.ScaleHandle(DataObjectT.Scale, Vector3.zero, Quaternion.identity, HandleUtility.GetHandleSize(Vector3.zero));
                        ElementEditor.EndMatrix(m);
                    }
                }
                var c = Event.current.type;
                return c == EventType.Used;
            }
        }
    }
}