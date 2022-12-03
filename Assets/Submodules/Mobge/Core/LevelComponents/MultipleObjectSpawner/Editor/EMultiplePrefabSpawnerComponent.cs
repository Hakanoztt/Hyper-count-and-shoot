using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(MultiplePrefabSpawnerComponent))]
    public class EMultiplePrefabSpawnerComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as MultiplePrefabSpawnerComponent.Data, this);
        }
        public class Editor : EditableElement<MultiplePrefabSpawnerComponent.Data> {


            private static HashSet<int> s_tempIds = new HashSet<int>();

            private bool _editMode;
            private Mobge.ElementEditor _editor;
            private EditorGrid _grid;
            public Editor(MultiplePrefabSpawnerComponent.Data component, EComponentDefinition editor) : base(component, editor) {
                _editor = ElementEditor.NewForScene(null);
                
            }

            // In this method, implement the logic you would normally implement under OnInspectorGUI() 
            public override void DrawGUILayout() {
                // Inspector: Boiler plate for exclusive editing of the element
                EditorGUILayout.HelpBox("You can use " + typeof(IComponentExtension) + " to have better control over levels.", MessageType.Info);
                base.DrawGUILayout();

                _editMode = ExclusiveEditField("edit on scene");
                var data = DataObjectT;
                data.Rotation = InspectorExtensions.CustomFields.Rotation.DrawAsVector3(data.Rotation);


                if (data.objects == null) {
                    data.objects = new MultiplePrefabSpawnerComponent.ObjectReference[0];
                }

                PrefabReferencesField(ref data.prefabReferences);

                ConnectionDataField();
                _grid = EditorLayoutDrawer.ObjectField("grid", _grid);
                if (_editMode) {
                    _editor.Editor = Editor;
                    //_editor.EditingObject = ElementEditor.EditingObject;
                    _editor.InspectorField();
                }
            }
            public override Transform CreateVisuals() {
                // var data = DataObjectT;
                CacheBehaviour cache = new GameObject("objects").AddComponent<CacheBehaviour>();
                cache.Initialize();
                UpdateVisuals(cache.transform); 
                return cache.transform;
            }
            public void UpdateVisuals() {

                if (TemporaryEditorObjects.Shared.TryGetInstance(_editor, out Transform instance)) {
                    UpdateVisuals(instance);
                }
            }
            public override void UpdateVisuals(Transform instance) {
                if (instance == null) {
                    return;
                }
                var cache = instance.GetComponent<CacheBehaviour>();
                if(cache == null) {
                    return;
                }
                for (int i = 0; i < cache.transform.childCount; i++) {
                    var obj = cache.transform.GetChild(i);
                    if (obj != null && obj.gameObject.activeSelf) {
                        if (!cache.cache.TryPush(obj)) {
                            obj.gameObject.DestroySelf();
                            //cache.cache.Push(obj);
                        }
                    }
                }
                var data = DataObjectT;
                if (data.objects != null && data.prefabReferences != null && data.prefabReferences.Length > 0) {
                    for (int i = 0; i < data.objects.Length; i++) {
                        var o = data.objects[i];
                        var index = o.index;
                        Component c = null;
                        if (index < 0) {
                            if (data.prefabReferences.Length >0) {
                                c = data.prefabReferences[UnityEngine.Random.Range(0, data.prefabReferences.Length)];
                            }
                        }
                        else if (index < data.prefabReferences.Length) {
                            c = data.prefabReferences[index];
                        }
                        if (c != null) {
                            var obj = cache.cache.Pop(c);
                            obj.transform.SetParent(cache.transform, false);
                            TemporaryEditorObjects.Shared.SetHideFlagsForAdding(obj.transform);
                            obj.transform.localPosition = o.position;
                            obj.transform.localRotation = o.rotation;
                        }
                    }
                }
            }
            private void ConnectionDataField() {
                var data = DataObjectT;
                if (data.prefabReferences == null) {
                    return;
                }
                if (ElementEditor == null) {
                    return;
                }
                if(data.componentConnections == null) {
                    data.componentConnections = MultiplePrefabSpawnerComponent.Data.s_defaultMaps;
                }
                if (ElementEditor.LogicMode) {
                    EditorGUILayout.HelpBox("Close logic editor to edit \"Component Connections\".", MessageType.Info);
                    if (GUILayout.Button("Close logic editor")) {
                        ElementEditor.LogicMode = false;
                    }
                }
                else {
                    EditorLayoutDrawer.CustomArrayField("Component Connections", ref data.componentConnections, (layout, map) => {
                        map.componentIndex = EditorDrawer.Popup(layout, "component", data.prefabReferences, map.componentIndex);
                        if (map.componentIndex >= 0 && map.componentIndex < data.prefabReferences.Length) {
                            var c = data.prefabReferences[map.componentIndex] as ILogicComponent;
                            if (c != null) {
                                var slots = MultiplePrefabSpawnerComponent.Data.s_tempSlots;
                                slots.Clear();
                                c.EditorOutputs(slots);
                                map.componentOutputId = EditorDrawer.Popup(layout, "output: " + map.slotId, slots, map.componentOutputId);
                            }
                        }
                        map.all = EditorGUI.Toggle(layout.NextRect(), "Wait for all.", map.all);
                        return map;
                    });
                    // make sure all slots are unique
                    s_tempIds.Clear();
                    for(int i = 0; i < data.componentConnections.Length; i++) {
                        var m = data.componentConnections[i];
                        if(m.slotId < MultiplePrefabSpawnerComponent.Data.c_componentSlotIdStart || s_tempIds.Contains(m.slotId)) {
                            m.slotId = FindUniqueSlotId(data);
                            data.componentConnections[i] = m;
                        }
                        s_tempIds.Add(m.slotId);

                    }
                }

            }
            private static int FindUniqueSlotId(MultiplePrefabSpawnerComponent.Data data) {
                int slot = MultiplePrefabSpawnerComponent.Data.c_componentSlotIdStart;
                bool @continue;
                do {
                    @continue = false;
                    for (int i = 0; i < data.componentConnections.Length; i++) {
                        var cc = data.componentConnections[i];
                        if (cc.slotId == slot) {
                            slot++;
                            @continue = true;
                            break;
                        }
                    }
                }
                while (@continue);
                return slot;
            }
            // In this method, implement the logic you would normally implement under OnSceneGUI() 
            public override bool SceneGUI(in SceneParams @params) {
                // Scene: Boiler plate for exclusive editing of the element
                bool enabled = @params.selected /* && _editMode */ ;
                var data = DataObjectT;

                var temp = ElementEditor.BeginMatrix(@params.matrix);
                if (_editMode && enabled) {
                    //_editor.Matrix = @params.matrix;
                    _editor.grid = _grid;
                    _editor.SceneGUI(UpdateElements);
                    DrawDirection();
                }
                ElementEditor.EndMatrix(temp);
                var t = Event.current.type;
                return enabled && (t == EventType.Used || t == EventType.MouseUp);
            }
            void DrawDirection() {
                var data = DataObjectT;
                if (data.objects != null && data.objects.Length > 0) {
                    var obj = data.objects[0];
                    for (int i = 1; i < data.objects.Length; i++) {
                        var nextObj = data.objects[i];

                        DrawArrow(obj.position, nextObj.position);

                        obj = nextObj;
                    }
                }
            }
            void DrawArrow(Vector3 p1, Vector3 p2) {
                var mid = (p1 + p2) * 0.5f;
                var m = Handles.matrix;
                var midWorld = m.MultiplyPoint3x4(mid);
                var camDir = -Camera.current.CameraToWorldPointRay(midWorld).direction;
                camDir = m.inverse.MultiplyVector(camDir);
                var dif = p2 - p1;
                float length = dif.magnitude;
                float arrowDim = length * 0.15f;
                var dir = dif / length;
                var normal = Vector3.Cross(camDir, dir).normalized;
                var arrowFront = mid + arrowDim * dir;
                var arrowBack = mid - arrowDim * dir;
                var arrowBack1 = arrowBack + normal * arrowDim;
                var arrowBack2 = arrowBack - normal * arrowDim;

                Handles.color = Color.blue;
                DrawOutlinedLine(p1, p2);
                DrawOutlinedLine(arrowFront, arrowBack1);
                DrawOutlinedLine(arrowFront, arrowBack2);
                Handles.color = Color.white;
            }
            void DrawOutlinedLine(Vector3 p1, Vector3 p2, Color color, Color outline) {
                Handles.color = outline;
                Handles.DrawAAPolyLine(7, p1, p2);
                Handles.color = color;
                Handles.DrawAAPolyLine(3, p1, p2);

            }
            void DrawOutlinedLine(Vector3 p1, Vector3 p2) {
                DrawOutlinedLine(p1, p2, Color.white, Color.black);
            }
            private static object s_buttonDescriptor = typeof(NodeEditor);

            private void UpdateElements() {
                var data = DataObjectT;
                var bd = new ElementEditor.NewButtonData("object", 0, NewObject, s_buttonDescriptor);
                _editor.AddButtonData(bd);
                for (int i = 0; i < data.objects.Length; i++) {
                    _editor.AddElement(new NodeEditor(i, this, s_buttonDescriptor));
                }
            }

            private AEditableElement NewObject() {
                var data = DataObjectT;
                int index = data.objects.Length;
                ArrayUtility.Insert(ref data.objects, index, new MultiplePrefabSpawnerComponent.ObjectReference() {
                    index = -1,
                    rotation = Quaternion.identity,
                    position = Vector3.zero
                });
                NodeEditor ne = new NodeEditor(index, this, s_buttonDescriptor);
                return ne;
            }
            [Serializable]
            private class ENode : IRotationOwner {
                //public Vector3 eulerAngles;
                [NonSerialized] public int index;
                [NonSerialized] public Editor editor;
                [SerializeField] MultiplePrefabSpawnerComponent.ObjectReference @ref;
                public ENode(Editor editor, int index) {
                    this.index = index;
                    @ref = editor.DataObjectT.objects[index];
                    this.editor = editor;
                    UpdateEulerAngles();
                }
                public MultiplePrefabSpawnerComponent.ObjectReference Ref {
                    get => editor.DataObjectT.objects[index];
                    set {
                        @ref = value;
                        editor.DataObjectT.objects[index] = value;
                    }
                }
            

                Quaternion IRotationOwner.Rotation {
                    get {
                        return Ref.rotation;
                    }
                    set {
                        var r = Ref;
                        if (r.rotation != value) {
                            r.rotation = value;
                            Ref = r;
                            UpdateEulerAngles();
                            ApplyChanges();
                        }
                    }
                }
                public void ApplyChanges() {
                    UpdateData();
                    editor.UpdateVisuals();
                    editor.Editor.Repaint();
                }
                public void UpdateData() {
                    editor.DataObjectT.objects[index] = @ref;
                    editor.UpdateData();
                }

                internal bool Delete() {
                    ArrayUtility.RemoveAt(ref editor.DataObjectT.objects, index);

                    editor.UpdateData();
                    editor.ElementEditor.RefreshContent(true);
                    return true;
                }

                internal void UpdateEulerAngles() {
                    //eulerAngles = Ref.rotation.eulerAngles;
                }
            }
            private class NodeEditor : AEditableElement<ENode> {


                public NodeEditor(int index, EMultiplePrefabSpawnerComponent.Editor editor, object buttonDescriptor) : base(buttonDescriptor) {
                    var node = new ENode(editor, index);
                    node.index = index;
                    DataObject = node;
                }
                public override object DataObject {
                    get => base.DataObject;
                    set {
                        base.DataObject = value;
                        DataObjectT.UpdateEulerAngles();
                    }
                }
                public override Vector3 Position {
                    get {
                        return DataObjectT.Ref.position;
                    }
                    set {
                        var r = DataObjectT.Ref;
                        if (r.position != value) {
                            r.position = value;
                            DataObjectT.Ref = r;
                            DataObjectT.ApplyChanges();
                        }
                    }
                }
                public override void UpdateData() {
                    DataObjectT.UpdateData();
                    base.UpdateData();
                }

                public override string Name => "node editor";

                public override bool Delete() {
                    return DataObjectT.Delete();
                }
                public override void InspectorGUILayout() {
                    var data = DataObjectT;
                    var root = data.editor.DataObjectT;
                    var node = data.Ref;
                    node.position = EditorGUILayout.Vector3Field("position", node.position);
                    node.rotation.eulerAngles = EditorGUILayout.Vector3Field("euler angles", node.rotation.eulerAngles);
                    //node.rotation = Quaternion.Euler(node.rotation.eulerAngles);
                    node.index = EditorLayoutDrawer.Popup("index", root.prefabReferences, node.index, "-random-");
                    data.Ref = node;
                }
            }
        }

        public static void PrefabReferencesField(ref Component[] references) {
            if (references == null) {
                references = new Component[0];
            }
            for (int i = 0; i < references.Length; i++) {
                var pr = references[i];
                if (pr != null) {
                    if (!(pr is IComponentExtension)) {
                        var ce = pr.GetComponent<IComponentExtension>();
                        if (ce != null) {
                            pr = (Component)ce;
                            references[i] = pr;
                            GUI.changed = true;
                        }
                    }
                }
            }
        }
    }
}
