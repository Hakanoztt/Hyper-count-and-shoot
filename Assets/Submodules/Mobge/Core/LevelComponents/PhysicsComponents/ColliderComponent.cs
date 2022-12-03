using System;
using System.Collections.Generic;
using UnityEngine;


namespace Mobge.Core.Components {
    public class ColliderComponent : ComponentDefinition<ColliderComponent.Data>
    {
        public static Collider2DShape DefaultShape {
            get {
#if UNITY_EDITOR
                Collider2DShape shape = new Collider2DShape();
                shape.shape = Collider2DShape.Shape.Rectangle;
                shape.EnsureData();
                shape.Size = new Vector2(2, 2);
                return shape;
#else
                return default(Collider2DShape);
#endif
            }
        }
        [Serializable]
        public class Data : BaseComponent, IChild, Trigger2DListener, IRotationOwner, Collision2DListerner
        {
            [HideInInspector] public Collider2DShape shape = DefaultShape;
            [HideInInspector, SerializeField] private Quaternion rotation = Quaternion.identity;
            [HideInInspector, SerializeField] private ElementReference _parent = -1;
            public bool _disabled;
            public bool isTrigger = true;
            [SerializeField] private Vector2[] points;
            [Layer] public int layer = -1;
            [LayerMask] public int layerMask = -1;
            public Vector2[] Points => points;
            public PhysicsMaterial2D material;
            
            public FilterMode mode;
            public Activation activation;
            [SerializeField, HideInInspector] public LogicConnections _connections;

            private InitArgs _args;
            private Collider2D _collider;
            private Collision2DCallbacks _callbacks;
            private int _itemCount;
            private bool _enterEnabled; //runtime
            private bool _exitEnabled; //runtime
            
            Quaternion IRotationOwner.Rotation { get => rotation; set => rotation = value; }
            public ElementReference Parent { get => _parent; set => _parent = value; }
            public override LogicConnections Connections {
                get => _connections;
                set => _connections = value;
            }
            public Collider2D Collider => _collider;
            public void EnsureLayer(Level level) {
                if(layer < 0) {
                    layer = level.GameSetup.defaultTriggerType;
                }
            }
            public override void Start(in InitArgs initData)
            {
                _args = initData;
                EnsureLayer(initData.player.level);
                Transform owner;
                _enterEnabled = !_disabled;
                _exitEnabled = !_disabled;

                if(_connections.GetConnectionCount(2) > 0) {
                    initData.player.FixedActionManager.DoTimedAction(0, null, InitFromOtherObject, null);
                }

                if (shape.shape != Collider2DShape.Shape.None) {
                    owner = new GameObject("trigger").transform;
                    owner.SetParent(initData.parentTr, false);
                    owner.localPosition = position;
                    owner.localRotation = rotation;
                    owner.gameObject.layer = layer;

                    _collider = shape.AddCollider(owner.gameObject);
                    _collider.isTrigger = isTrigger;
                    _collider.enabled = !_disabled;
                    _collider.sharedMaterial = material;

                    AddCallbacks(owner.gameObject);
                }
            }

            private void InitFromOtherObject(object data, bool completed) {
                var e = _connections.Invoke(this, 2, null, _args.components);
                while (e.MoveNext()) {
                    var obj = (Rigidbody2D)e.Current;
                    _callbacks = obj.gameObject.AddComponent<Collision2DCallbacks>();
                    _callbacks.listener = this;
                }
            }

            void AddCallbacks(GameObject owner) {
                if (isTrigger) {
                    var cbs = owner.AddComponent<Trigger2DCallbacks>();
                    cbs.listener = this;
                }
                else {
                    var cbs = owner.AddComponent<Collision2DCallbacks>();
                    cbs.listener = this;
                }
            }
            public bool IsEmpty() {
                if (shape.points == null) {
                    return true;
                }
                for(int i = 0; i < shape.points.Length; i++) {
                    if(shape.points[i]!=Vector2.zero) {
                        return false;
                    } 
                }
                return true;
            }
            bool Accepts(bool trigger) {
                if(trigger) {
                    if (mode == FilterMode.AcceptCollider) return false;
                }
                else {
                    if (mode == FilterMode.AcceptTrigger) return false;
                }
                return true;
            }
            private bool Masks(int objectLayer) {
                bool p = layerMask != (layerMask | (1 << objectLayer));
                return p;
            }
            bool CanCollider(Collider2D col) {
                if (!Accepts(col.isTrigger)) {
                    return false;
                }
                var go = col.gameObject;
                var l = go.layer;
                if (Masks(l)) {
                    return false;
                }
                return true;
            }
            private void HandleEnter(Collider2D col) {
                if (!_enterEnabled) {
                    return;
                }
                if (!CanCollider(col)) {
                    return;
                }
                switch (activation) {
                    default:
                    case Activation.None:
                        _connections.InvokeSimple(this, 0, col.attachedRigidbody, _args.components);
                        break;
                    case Activation.OneTime:
                        _enterEnabled = false;
                        if (shape.shape != Collider2DShape.Shape.None) {
                            _collider.enabled = false;
                        }
                        // if(_connections.GetConnectionCount(2) > 0 && _callbacks != null) {
                            // _callbacks.listener = null;
                            //Destroy(_callbacks); //Do not create garbage
                        // }
                        _connections.InvokeSimple(this, 0, col.attachedRigidbody, _args.components);
                        break;
                    case Activation.CumulativeEnterExit:
                        if (_itemCount == 0) {
                            _connections.InvokeSimple(this, 0, col.attachedRigidbody, _args.components);
                        }
                        _itemCount++;
                        break;
                }
            }
            
            private void HandleExit(Collider2D col) {
                if (!_exitEnabled) {
                    return;
                }
                if (!CanCollider(col)) {
                    return;
                }
                switch (activation) {
                    default:
                    case Activation.None:
                        _connections.InvokeSimple(this, 1, col.attachedRigidbody, _args.components);
                        break;
                    case Activation.OneTime:
                        _exitEnabled = false;
                        _connections.InvokeSimple(this, 1, col.attachedRigidbody, _args.components);
                        break;
                    case Activation.CumulativeEnterExit:
                        _itemCount--;
                        if (_itemCount == 0) {
                            _connections.InvokeSimple(this, 1, col.attachedRigidbody, _args.components);
                        }
                        break;
                }
            }
            void Trigger2DListener.OnTriggerEnter2D(Trigger2DCallbacks sender, Collider2D collider)
            {
                HandleEnter(collider);
            }

            void Trigger2DListener.OnTriggerExit2D(Trigger2DCallbacks sender, Collider2D collider) {
                HandleExit(collider);
            }
            void Collision2DListerner.OnCollisionEnter2D(Collision2DCallbacks collider2DCallbacks, Collision2D collision) {
                HandleEnter(collision.collider);
            }

            void Collision2DListerner.OnCollisionExit2D(Collision2DCallbacks collider2DCallbacks, Collision2D collision) {
                HandleExit(collision.collider);
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case 0:
                        _collider.enabled = true;
                        _enterEnabled = true;
                        _exitEnabled = true;
                        break;
                    case 1:
                        _collider.enabled = false;
                        _enterEnabled = false;
                        _exitEnabled = false;
                        break;
                    case 2:
                        layer = (int) (float) input;
                        _collider.gameObject.layer = layer;
                        break;
                    case 3:
                        layerMask = (int) (float) input;
                        break;
                }
                return null;
            }
#if UNITY_EDITOR
            public override void EditorOutputs(List<LogicSlot> slots) {

                slots.Add(new LogicSlot("object entered", 0, typeof(Rigidbody2D)));
                slots.Add(new LogicSlot("object exited", 1, typeof(Rigidbody2D)));
                slots.Add(new LogicSlot("listen rigidbody for collider events", 2,null, typeof(Rigidbody2D)));
            }
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("enable", 0));
                slots.Add(new LogicSlot("disable", 1));
                slots.Add(new LogicSlot("set layer", 2, typeof(float), null));
                slots.Add(new LogicSlot("set layermask", 3, typeof(int), null));
            }
#endif
        }
        public enum FilterMode {
            AcceptCollider = 0,
            AcceptTrigger = 1,
            AcceptAny = 2,
        }
        public enum Activation {
            None = 0,
            OneTime = 1,
            CumulativeEnterExit = 2,
        }
    }
}