using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    [CustomEditor(typeof(RoadGeneratorComponent))]
    public class ERoadGeneratorComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as RoadGeneratorComponent.Data, this);
        }
        public class Editor : EditableElement<RoadGeneratorComponent.Data> {
            private bool _editMode = false;
            EditorTools _tools;
            private int _pressedIndex;
            private bool _pressedInsert;
            private bool _needsUpdateVisuals;
            public Editor(RoadGeneratorComponent.Data component, EComponentDefinition editor) : base(component, editor) {

                _tools = new EditorTools();
                _tools.AddTool(new EditorTools.Tool("open node edit window") {
                     activation = new EditorTools.ActivationRule() {
                         mouseButton = 1,
                     },
                     onPress = () => {
                         _pressedIndex = GetMouseElementIndex(out _pressedInsert);
                         if( _pressedIndex >= 0 && !_pressedInsert) {
                             OpenEditWindow();
                             return true;
                         }
                         return false;
                     },
                    onRelease = () => {

                    }
                });
            }

            private void OpenEditWindow() {
                var p = new EditorPopup((rects, popup) => {
                    EditorGUI.BeginChangeCheck();
                    var data = DataObjectT;
                    //var newId = EditorGUI.IntField(rects.NextRect(), data.items[_pressedIndex].id);
                    var newId = EditorDrawer.Popup(rects, "item", data.prefabReferences, data.items[_pressedIndex].id);


                    data.items[_pressedIndex].id = Mathf.Clamp(newId, 0, data.prefabReferences.Length - 1);

                    data.items[_pressedIndex].flipZ = EditorGUI.Toggle(rects.NextRect(), "flip z", data.items[_pressedIndex].flipZ);

                    //data.items[_pressedIndex].scale = EditorGUI.Vector3Field(rects.NextRect(EditorGUIUtility.singleLineHeight*2), "scale", data.items[_pressedIndex].scale);
                    if (GUI.Button(rects.NextRect(), "Duplicate")) {
                        ArrayUtility.Insert(ref DataObjectT.items, _pressedIndex, data.items[_pressedIndex]);
                        _pressedIndex++;
                        HandleDuplicate(_pressedIndex);
                    }
                    if (GUI.Button(rects.NextRect(), "Delete")) {
                        ArrayUtility.RemoveAt(ref DataObjectT.items, _pressedIndex);
                        popup.Close();
                        HandleDelete(_pressedIndex);
                    }
                    if (EditorGUI.EndChangeCheck()) {
                        this.UpdateData();
                        this._needsUpdateVisuals = true;

                    }
                });
                p.Show(new Rect(ElementEditor.visualHandler.ScreenMousePosition, Vector2.zero), new Vector2(200, 150));
            }

            private void HandleDelete(int deletedIndex) {
                var e = ElementEditor.GetElementsWithType<IERoadElement>();
                while (e.MoveNext()) {
                    var c = e.Current;
                    var element = c.Data;
                    if(element.roadGenerator.id == Id) {
                        if(element.pieceIndex > deletedIndex) {
                            element.pieceIndex--;
                            c.Data = element;
                            this._needsUpdateVisuals = true;
                        }
                    }
                }
            }
            private void HandleDuplicate(int insertedIndex) {
                var e = ElementEditor.GetElementsWithType<IERoadElement>();
                while (e.MoveNext()) {
                    var c = e.Current;
                    var element = c.Data;
                    if (element.roadGenerator.id == Id) {
                        if (element.pieceIndex > insertedIndex) {
                            element.pieceIndex++;
                            c.Data = element;
                            this._needsUpdateVisuals = true;
                        }
                    }
                }
            }

            public int GetMouseElementIndex(out bool insert) {
                var mRay = ElementEditor.visualHandler.MouseRay;
                var pe = new PoseEnumerator(DataObjectT);
                while (pe.MoveNext()) {
                    var pose = pe.Current;
                    if (Intersects(pose.position, mRay)) {
                        insert = false;
                        return pe.CurrentIndex;
                    }
                    //if (Intersects(pe.CurrentEndPose.position, mRay)) {
                    //    insert = true;
                    //    return pe.CurrentIndex + 1;
                    //}
                }
                insert = false;
                return -1;
            }
            bool Intersects(Vector3 position, Ray ray) {
                var size = this.GetNodeRadius(position);
                ElementEditor.Sphere s;
                s.position = position;
                s.radius = size;
                return s.Contains(ray);
            }

            public override void DrawGUILayout() {
                base.DrawGUILayout();


                _editMode = ExclusiveEditField("edit on scene");

                var data = DataObjectT;
                for (int i = 0; i < data.prefabReferences.Length; i++) {
                    var pr = data.prefabReferences[i];
                    if (pr.res != null) {
                        if (!(pr.res is RoadGeneratorComponent.IRoadPiece)) {
                            var ce = pr.res.GetComponent<RoadGeneratorComponent.IRoadPiece>();
                            if (ce != null) {
                                pr.res = (Component)ce;
                                data.prefabReferences[i] = pr;
                                GUI.changed = true;
                            }
                        }
                    }
                }

                //if (GUILayout.Button("example button")) {
                //    Debug.Log("button does stuff");
                //}
                //GUILayout.Label("example label");
            }

            public override Transform CreateVisuals() {
                var go = new GameObject("road").transform;
                return go;
            }

            public override void UpdateVisuals(Transform instance) {
                base.UpdateVisuals(instance);
                if (instance) {
                    var to = TemporaryEditorObjects.Shared;
                    for (int i = 0; i < instance.childCount; i++) {
                        var ins = instance.GetChild(i);
                        if (to.PrefabCache.ContainsInstance(ins)) {
                            to.PrefabCache.Push(ins);
                            ins.SetParent(to.transform, false);
                        }
                        else {
                            ins.gameObject.DestroySelf();
                            i--;
                            //break;
                        }
                    }
                    var data = DataObjectT;
                    if (data.IsValid()) {
                        data.InitReferences();
                        var pose = new Pose(Vector3.zero, Quaternion.identity);
                        for (int i = 0; i < data.items.Length; i++) {
                            var index = data.items[i];
                            var pRef = data.prefabReferences[index.id];
                            var piece = to.PrefabCache.Pop(pRef.res.transform);
                            piece.transform.SetParent(instance, false);
                            //piece.transform.localScale = Vector3.Scale(piece.transform.localScale, data.items[i].scale);
                            TemporaryEditorObjects.SetHideFlags(piece, to.gameObject.hideFlags);
                            piece.transform.localScale = pRef.res.transform.localScale;
                            data.PlacePiece(piece, pRef.StartPose, pRef.EndPose, ref pose);
                            if (data.items[i].flipZ) {
                                RoadGeneratorComponent.Data.FlipItem(piece);
                            }
                        }
                    }
                }
            }
            public float GetNodeRadius(in Vector3 position) {
                return HandleUtility.GetHandleSize(position) * 0.16f;
            }
            public float GetNodeRadius(in Pose pose) {
                return GetNodeRadius(pose.position);
            }
            public override bool SceneGUI(in SceneParams @params) {

                if (_editMode) {
                    var mat = ElementEditor.BeginMatrix(@params.matrix);
                    var data = DataObjectT;
                    if (data.IsValid()) {
                        PoseEnumerator pe = new PoseEnumerator(data);
                        while (pe.MoveNext()) {
                            var itemPose = pe.Current;
                            var size = GetNodeRadius(itemPose);
                            Handles.color = Color.white;
                            Handles.SphereHandleCap(0, itemPose.position, itemPose.rotation, size, Event.current.type);
                            //Handles.color = Color.gray;
                            //var insertPose = pe.CurrentEndPose;
                            //Handles.SphereHandleCap(0, insertPose.position, insertPose.rotation, size, Event.current.type);
                        }
                    }
                    _tools.OnSceneGUI();

                    ElementEditor.EndMatrix(mat);
                }
                if (_needsUpdateVisuals) {
                    _needsUpdateVisuals = false;

                    UpdateVisuals(ElementEditor.UpdateVisualInstance(this));
                }

                return false;
            }
            public PoseEnumerator GetPoseEnumerator(bool startFromIdentity = true) {
                return new PoseEnumerator(DataObjectT, startFromIdentity);
            }
        }
        public struct PoseEnumerator {
            private RoadGeneratorComponent.Data _data;
            private int _index;
            private Pose _endPose, _pose;
            public int CurrentIndex { get => _index; }

            public PoseEnumerator(RoadGeneratorComponent.Data data, bool startFromIdentity = true) {
                data.InitReferences();
                _data = data;
                _index = -1;
                _pose = Pose.identity;
                if (startFromIdentity) {
                    _endPose = Pose.identity;
                }
                else {
                    _endPose = new Pose(data.position, data.rotation);
                }
            }
            public bool MoveNext() {
                _index++;
                if (_index >= _data.items.Length) {
                    return false;
                }

                var item = _data.items[_index];
                var pr = _data.prefabReferences[item.id];
                //Vector3.Scale(pr.res.transform.localScale, item.scale)
                _pose = _data.IteratePose(pr.res.transform.localScale, pr.StartPose, pr.EndPose, ref _endPose);

                return true;
            }
            public Pose CurrentEndPose {
                get => _endPose;
            }
            public Pose Current {
                get => _pose;
            }
        }
    }
}
