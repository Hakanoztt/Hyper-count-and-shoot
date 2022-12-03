using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace Mobge {
    public abstract class AEditableElement {
        private static List<LogicSlot> s_slots = new List<LogicSlot>();
        private object _buttonDescriptor;
        internal SelectionArea selectionArea;
        protected internal SlotList inputSlots;
        protected internal SlotList outputSlots;
        internal ILogicComponent LogicComponent => DataObject as ILogicComponent;
        public bool RequestsExclusiveEdit {
            get; private set;
        }
        public int Id { get; set; }

        public abstract object DataObject { get; set; }
        public virtual bool DrawOtherObjectsGizmos => false;
        protected internal ElementEditor ElementEditor {
            get; set;
        }
        public object ButtonDescriptor => _buttonDescriptor;
        internal void UpdateSlots() {
            if(LogicComponent != null) {
                s_slots.Clear();
                LogicComponent.EditorInputs(s_slots);
                inputSlots.UpdateSlots(s_slots);
                s_slots.Clear();
                LogicComponent.EditorOutputs(s_slots);
                outputSlots.UpdateSlots(s_slots);
            }
        }
        public virtual bool HandlesEnabled {
            get => true;
        }
        internal void ClearSlots() {
            inputSlots.UpdateSlots(null);
            outputSlots.UpdateSlots(null);
        }
        protected bool ExclusiveEditField(string label = "edit") {
            RequestsExclusiveEdit = true;
            return ElementEditor.ExclusiveEditField(label, this);
        }
        protected void ReleaseExclusiveEdit() {
            ElementEditor.ReleaseExclusiveEdit(this);
        }
        public abstract Vector3 Position{ get; set; }
        public AEditableElement(object buttonDescriptor) {
            this._buttonDescriptor = buttonDescriptor;
            this.Id = -1;
            inputSlots.Initialize();
            outputSlots.Initialize();
        }
        public abstract void InspectorGUILayout();
        public virtual bool SceneGUI(in SceneParams @params) {
            //var t = Event.current.type;
            //return t == EventType.MouseUp || t == EventType.Used;
            return false;
        }

        public virtual Transform CreateVisuals() {
            var sp = DataObject as IVisualSpawner;
            if (sp != null) {
                return sp.CreateVisuals();
            }
            var pr = PrefabReference;
            if (pr == null) {
                return null;
            }
            var ins = UnityEngine.Object.Instantiate(pr);
            UpdateVisuals(ins);
            return ins;
        }
        public virtual void UpdateVisuals(Transform instance) {
            var sp = DataObject as IVisualSpawner;
            if (sp != null) {
                sp.UpdateVisuals(instance);
            }
        }
		public virtual Texture2D IconTexture => null;
        public IEnumerator<InConnection> GetConnections(int index) {
            var lc = LogicComponent;
            if(lc != null) {
                var cons = lc.Connections;
                if(cons != null){
                    var e = lc.Connections.GetConnections(index);
                    while(e.MoveNext()) {
                        var c = e.Current;
                        if(ElementEditor.TryGetElement(c.target, out AEditableElement element)) {
                            InConnection ic;
                            ic.elemenet = element;
                            ic.inputId = c.input;
                            yield return ic;
                        }
                    }
                }
            }
        }
        public bool TryGetConnectedInputSlot(int inputId, out AEditableElement sender, out Slot slot) {
            if (DataObject is ILogicComponent li) {

                var allElements = ElementEditor.AllElements;
                while (allElements.MoveNext()) {
                    var e = allElements.Current;
                    if(e.DataObject is ILogicComponent senderLi) {
                        var cons = senderLi.Connections;
                        if (cons != null) {
                            for(int i = 0;  i < cons.List.Count; i++) {
                                var con = cons.List[i];
                                if (con.target == Id && con.input == inputId) {
                                    if (e.outputSlots.TryFindSlot(con.output, out slot)) {
                                        sender = e;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            slot = default;
            sender = null;
            return false;
        }
        public bool TryGetConnectedSlot(int outputId, out AEditableElement target, out Slot slot) {
            if (DataObject is ILogicComponent li) {
                var cons = li.Connections;
                if (cons != null) {
                    var e = cons.GetConnections(outputId);
                    LogicConnection con = default;
                    bool found = false;
                    while (e.MoveNext()) {
                        con = e.Current;
                        found = true;
                    }
                    if (found) {
                        if (ElementEditor.TryGetElement(con.target, out target)) {
                            if (target.inputSlots.TryFindSlot(con.input, out slot)) {
                                return true;
                            }
                        }
                    }

                }
            }
            slot = default;
            target = null;
            return false;
        }
        public void RecordObject(UnityEngine.Object obj, string name) {
            ElementEditor.RecordObject(obj, name);
        }
        public abstract string Name{ get; }
        public abstract bool Delete();
        public virtual Transform PrefabReference {
            get {
                return null;
            }
        }
        public Matrix4x4 GetMatrix() {
            return ElementEditor.GetMatrix(this);
        }
        public void SetPose(Pose pose) {
            ElementEditor.SetPose(this, pose);
            ElementEditor.UpdateVisualInstance(this);
        }
        public void SetPosition(Vector3 pos) {
            ElementEditor.SetPosition(this, pos);
            ElementEditor.UpdateVisualInstance(this);
        }
        public Vector3 MoveCenterByLocal(Vector3 localOffset) {
            var m = GetMatrix();
            var realPos = m.MultiplyPoint3x4(localOffset);
            SetPosition(realPos);
            var im = m.inverse;
            return im.MultiplyPoint3x4(realPos);
        }
        public Vector3 GetPosition() {
            return ElementEditor.GetPose(this).position;
        }
        public virtual void UpdateData() {
            ElementEditor.RecordMainObject("edit");
        }
        public bool HasRotation {
            get => DataObject is IRotationOwner;
        }
        public virtual Quaternion Rotation {
            get {
                var o = DataObject as IRotationOwner;
                if (o == null) return Quaternion.identity;
                var r = o.Rotation;
                if (r.x == 0 && r.y == 0 && r.z == 0 & r.w == 0) {
                    o.Rotation = Quaternion.identity;
                }
                return o.Rotation;
            }
            set {
                ((IRotationOwner)DataObject).Rotation = value;
                UpdateData();
            }
        }
        public bool IsParent {
            get => DataObject is IParent;
        }
        public bool IsChild {
            get => DataObject is IChild;
        }
        public ElementReference Parent {
            get => ((IChild)DataObject).Parent;
            set => ((IChild)DataObject).Parent = value;
        }
        public struct SceneParams {
            public Vector3 position;
            public Quaternion rotation;
            public Matrix4x4 matrix;
            public bool selected;
            public bool solelySelected;
        }
        public struct SelectionArea {
            private Vector2 _size;
            private Vector3 _center;
            private Vector3[] _corners;
            public void Update(Vector3 center, Vector2 size) {
                _center = center;
                _size = size;
            }
            public Vector3[] GetCorners(Axis axis) {
                if (_corners == null) {
                    _corners = new Vector3[4];
                }
                var hs = _size * 0.5f;
                _corners[0] = _center + hs.x * axis.right + hs.y * axis.up;
                _corners[1] = _center + hs.x * axis.right + hs.y * -axis.up;
                _corners[2] = _center + hs.x * -axis.right + hs.y * -axis.up;
                _corners[3] = _center + hs.x * -axis.right + hs.y * axis.up;
                return _corners;

            }
            public bool Collides(Ray ray) {
                var l = Vector3.Dot(ray.direction, _center - ray.origin);
                var p = ray.origin +  l * ray.direction;
                var rayToCenterDistanceSqr = (_center - p).sqrMagnitude;
                return _size.sqrMagnitude * (0.5f*0.5f) >= rayToCenterDistanceSqr;

            }
        }
        public struct SlotList {
            private ExposedList<Slot> _slots;
            public void UpdateSlots(List<LogicSlot> slots) {
                int count = slots == null ? 0 : slots.Count;
                if(_slots == null) {
                    _slots = new ExposedList<Slot>();
                }
                _slots.SetCountFast(count);
                
                var arr = _slots.array;
                for(int i = 0; i < count; i++) {
                    arr[i]._slot = slots[i];
                }
            }
            public int Count => _slots.Count;
            public void SetPosition(int index, Vector3 position, float radius, in Axis axis) {
                ElementEditor.Sphere vs;
                vs.position = position;
                vs.radius = radius;
                _slots.array[index].visual = vs;
                _slots.array[index].axis = axis;
            }
            public Slot this[int index]
            {
                get => _slots.array[index];
            }
            internal void Initialize()
            {
                _slots = new ExposedList<Slot>();
            }
            public bool TryFindSlot(int id, out Slot slot) {
                var arr = _slots.array;
                for(int i = 0; i < _slots.Count; i++) {
                    var s = arr[i];
                    if(s._slot.id == id) {
                        slot = s;
                        return true;
                    }
                }
                slot = default(Slot);
                return false;
            }
            public Slot FindSlot(Ray ray) {
                var arr = _slots.array;
                for(int i = 0; i < _slots.Count; i++) {
                    var s = arr[i];
                    if(s.visual.Contains(ray)) {
                        return s;
                    }
                }
                return new Slot();
            }
        }
        public struct Slot {
            public Mobge.LogicSlot _slot;
            public ElementEditor.Sphere visual;
            public Axis axis;
        }
        public struct InConnection {
            public AEditableElement elemenet;
            public int inputId;
        }
    }
    public abstract class AEditableElement<T> : AEditableElement where T : class{
        private T _dataObject;
        public AEditableElement(object buttonDescriptor) : base(buttonDescriptor) {
        }
        public override object DataObject {
            get => _dataObject;
            set => _dataObject = value as T;
        }
        public T DataObjectT {
            get => _dataObject;
            set => _dataObject = value;
        }
    }
}