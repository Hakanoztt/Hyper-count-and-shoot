using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(PhysicalComplex))]
    public class EPhysicalComplex : EComponentDefinition {
            private static Dictionary<int, object> s_tempDatas = new Dictionary<int, object>();
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as PhysicalComplex.Data, this);
        }
        public class Editor : EditableElement<PhysicalComplex.Data> {
            //private bool _editMode = false;

            private Dictionary<int, ElementEditor.Sphere> _buttons = new Dictionary<int, ElementEditor.Sphere>();

            private EditorTools _tools;
            private bool _editMode;

            public Editor(PhysicalComplex.Data component, EComponentDefinition editor) : base(component, editor) {
                _tools = new EditorTools();
                _tools.AddTool(new EditorTools.Tool("open windows") {
                    activation = new EditorTools.ActivationRule() {
                        mouseButton = 1,
                    },
                    onRelease = TryOpenWindow
                });
            }

            private void TryOpenWindow() {
                var e = _buttons.GetEnumerator();
                var ray = ElementEditor.visualHandler.MouseRay;
                var data = DataObjectT;
                while (e.MoveNext()) {
                    var c = e.Current;
                    if (c.Value.Contains(ray)) {
                        Mobge.EditorPopup popup = new EditorPopup((layout, pop) => {
                            EditorGUI.BeginChangeCheck();
                            ElementEditor.TryGetElement(c.Key, out var selected);
                            if(selected.DataObject is JointComponentData joint) {
                                var jd = data.jointDatas[c.Key];
                                jd.breakForce = EditorGUI.FloatField(layout.NextRect(),"break force", jd.breakForce);
                                //jd.value = EditorGUI.FloatField(layout.NextRect(), "value", jd.value);
                                data.jointDatas[c.Key] = jd;
                            }
                            if (EditorGUI.EndChangeCheck()) {
                                this.UpdateData();
                            }
                        });
                        popup.Show(new Rect(ElementEditor.visualHandler.ScreenMousePosition, new Vector2()), new Vector2(200, 60));
                        break;
                    }
                }
            }

            public override void DrawGUILayout() {
                base.DrawGUILayout();
                _editMode = ExclusiveEditField("edit on scene");
                //if (GUILayout.Button("example button")) {
                //    Debug.Log("button does stuff");
                //}
                //GUILayout.Label("example label");
            }
            public override bool SceneGUI(in SceneParams @params) {
                //bool enabled = @params.selected;
                bool edited = false;

                edited = SyncDatas<JointComponentData, PhysicalComplex.JointData>(ElementEditor ,ref DataObjectT.jointDatas, (id) => {
                    return new PhysicalComplex.JointData() {
                        breakForce = 1,
                        //value = 1
                    };
                });
                edited = SyncDatas<PrefabSpawnerComponent.Data, PhysicalComplex.RigidbodyData>(ElementEditor, ref DataObjectT.physicalBodies, (id) => {
                    PhysicalComplex.RigidbodyData d;
                    d.body = null;
                    return d;
                }, (id, psc)=> {
                    return ((IChild)psc).Parent < 0;
                }) || edited;
                if (@params.solelySelected && _editMode) {
                    UpdateButtons();
                    _tools.OnSceneGUI();
                }

                // Handles.matrix = oldMatrix;
                return edited;
            }
            private void UpdateButtons() {
                var data = DataObjectT;
                _buttons.Clear();
                var e = data.jointDatas.GetEnumerator();
                while (e.MoveNext()) {
                    var cc = e.Current;
                    AddButton(cc.Key, new Color(0, 0, 1, 1));
                }
                var rbe = data.physicalBodies.GetEnumerator();
                while (rbe.MoveNext()) {
                    var cc = rbe.Current;
                    AddButton(cc.Key, new Color(1, 0, 0, 1));

                }

            }
            ElementEditor.Sphere AddButton(int id, Color color) {

                ElementEditor.TryGetElement(id, out var targetElement);
                var pos = targetElement.GetPosition();
                var size = HandleUtility.GetHandleSize(pos);
                ElementEditor.Sphere sphere;
                pos.y += size * 0.4f;
                sphere.position = pos;
                sphere.radius = size * 0.1f;
                _buttons.Add(id, sphere);


                Handles.color = color;
                var normal = -Camera.current.CameraToWorldPointRay(Handles.matrix.MultiplyPoint3x4(sphere.position)).direction;
                Handles.DrawSolidDisc(sphere.position, normal, sphere.radius);

                return sphere;
            }
        }

        public static bool SyncDatas<T, K>(ElementEditor editor, ref Dictionary<int, K> dic, Func<int, K> newFunction, Func<int, T, bool> predicate = null) where T : BaseComponent {
            if (dic == null) {
                dic = new Dictionary<int, K>();
            }
            var de = dic.GetEnumerator();
            s_tempDatas.Clear();
            int oldCount = dic.Count;
            while (de.MoveNext()) {
                var cc = de.Current;
                s_tempDatas.Add(cc.Key, cc.Value);
            }
            dic.Clear();
            var e = editor.GetElementsWithDataType<T>();
            int unchangedCount = 0;
            int newCount = 0;
            while (e.MoveNext()) {
                var c = e.Current;
                if (predicate != null && !predicate(c.Key, c.Value)) {
                    continue;
                }
                if (s_tempDatas.TryGetValue(c.Key, out var val)) {
                    unchangedCount++;
                    dic.Add(c.Key, (K)val);
                }
                else {
                    newCount++;
                    dic.Add(c.Key, newFunction(c.Key));
                }
            }
            return unchangedCount != oldCount || newCount > 0;

        }
    }
}
             