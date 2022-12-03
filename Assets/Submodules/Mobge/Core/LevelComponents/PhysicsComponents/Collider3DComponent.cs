using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class Collider3DComponent : ComponentDefinition<Collider3DComponent.Data>
    {
        public static Collider3DShape DefaultShape {
            get {
#if UNITY_EDITOR
                Collider3DShape shape = new Collider3DShape();
                shape.shape = Collider3DShape.Shape.Box;
                shape.EnsureData();
                shape.Size = Vector3.one;
                return shape;
#else
                return default(Collider3DShape);
#endif
            }
        }
        [Serializable]
        public class Data : BaseComponent, IChild, TriggerListener, IRotationOwner, CollisionListerner {
            
            Quaternion IRotationOwner.Rotation { get => rotation; set => rotation = value; }
            [SerializeField] private Quaternion rotation = Quaternion.identity;

            public bool _disabled;
            public bool isTrigger = true;

            public PhysicMaterial material;
            public FilterMode mode;
            public Activation activation;

            [HideInInspector]
            public Collider3DShape shape = DefaultShape;

            [HideInInspector] [SerializeField] private ElementReference _parent = -1;
            public ElementReference Parent { get => _parent; set => _parent = value; }
            public bool hasRigidbody;
            private InitArgs _args;
            private Collider _collider;
            private CollisionCallbacks _callbacks;
            [Layer]
            public int layer = -1;
            [HideInInspector]
            public int layerMask = -1;

            private int _itemCount;

            [SerializeField] [HideInInspector] private LogicConnections _connections;
            public override LogicConnections Connections {
                get => _connections;
                set => _connections = value;
            }
            public void EnsureLayer(Level level) {
                if (layer < 0) {
                    layer = level.GameSetup.defaultTriggerType;
                }
            }
            public override void Start(in InitArgs initData) {
                _args = initData;
                EnsureLayer(initData.player.level);
                Transform owner;

                if (_connections.GetConnectionCount(2) > 0) {
                    initData.player.FixedActionManager.DoTimedAction(0, null, InitFromOtherObject, null);
                }

                if (shape.shape != Collider3DShape.Shape.None) {
                    owner = new GameObject("trigger3D").transform;
                    owner.SetParent(initData.parentTr, false);
                    owner.localPosition = position;
                    owner.localRotation = rotation;
                    owner.gameObject.layer = layer;

                    _collider = shape.AddCollider(owner.gameObject);
                    _collider.isTrigger = isTrigger;
                    _collider.enabled = !_disabled;
                    _collider.sharedMaterial = material;

                    if (hasRigidbody) {
                        var rb = owner.gameObject.AddComponent<Rigidbody>();
                        rb.isKinematic = true;
                    }

                    AddCallbacks(owner.gameObject);
                }
            }
            private void InitFromOtherObject(object data, bool completed) {
                var e = _connections.Invoke(this, 2, null, _args.components);
                while (e.MoveNext()) {
                    var obj = (Rigidbody)e.Current;
                    _callbacks = obj.gameObject.AddComponent<CollisionCallbacks>();
                    _callbacks.listener = this;
                }
            }
            void AddCallbacks(GameObject owner) {
                if (isTrigger) {
                    var cbs = owner.AddComponent<TriggerCallbacks>();
                    cbs.listener = this;
                } else {
                    var cbs = owner.AddComponent<CollisionCallbacks>();
                    cbs.listener = this;
                }
            }
            public bool IsEmpty() {
                if (shape.points == null) {
                    return true;
                }
                for (int i = 0; i < shape.points.Length; i++) {
                    if (shape.points[i] != Vector3.zero) {
                        return false;
                    }
                }
                return true;
            }
            bool Accepts(bool trigger) {
                if (trigger) {
                    if (mode == FilterMode.AcceptCollider) return false;
                } else {
                    if (mode == FilterMode.AcceptTrigger) return false;
                }
                return true;
            }
            private bool Masks(int objectLayer) {
                bool p = layerMask != (layerMask | (1 << objectLayer));
                return p;
            }
            private void HandleEnter(Collider col) {
                if (!Accepts(col.isTrigger)) {
                    return;
                }
                var go = col.gameObject;
                var l = go.layer;
                if (Masks(l)) {
                    return;
                }
                switch (activation) {
                    default:
                    case Activation.None:
                        _connections.InvokeSimple(this, 0, col.attachedRigidbody, _args.components);
                        break;
                    case Activation.OneTime:
                        if (shape.shape != Collider3DShape.Shape.None) {
                            _collider.enabled = false;
                        }
                        if (_connections.GetConnectionCount(2) > 0 && _callbacks != null) {
                            _callbacks.listener = null;
                            //Destroy(_callbacks); //Dont create garbage
                        }
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
            private void HandleExit(Collider col) {
                if (!Accepts(col.isTrigger)) {
                    return;
                }
                var go = col.gameObject;
                var l = go.layer;
                if (Masks(l)) {
                    return;
                }
                switch (activation) {
                    default:
                    case Activation.None:
                        _connections.InvokeSimple(this, 1, col.attachedRigidbody, _args.components);
                        break;
                    case Activation.OneTime:
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
            void TriggerListener.OnTriggerEnter(TriggerCallbacks sender, Collider collider) {
                HandleEnter(collider);
            }

            void TriggerListener.OnTriggerExit(TriggerCallbacks sender, Collider collider) {
                HandleExit(collider);
            }
            void CollisionListerner.OnCollisionEnter(CollisionCallbacks collider3DCallbacks, Collision collision) {
                HandleEnter(collision.collider);
            }

            void CollisionListerner.OnCollisionExit(CollisionCallbacks collider3DCallbacks, Collision collision) {
                HandleExit(collision.collider);
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case 0:
                        _collider.enabled = true;
                        break;
                    case 1:
                        _collider.enabled = false;
                        break;
                    case 2:
                        layer = (int)(float)input;
                        _collider.gameObject.layer = layer;
                        break;
                    case 3:
                        layerMask = (int)(float)input;
                        break;
                }
                return null;
            }
#if UNITY_EDITOR
            public override void EditorOutputs(List<LogicSlot> slots) {

                slots.Add(new LogicSlot("object entered", 0, typeof(Rigidbody)));
                slots.Add(new LogicSlot("object exited", 1, typeof(Rigidbody)));
                slots.Add(new LogicSlot("listen rigidbody for collider events", 2, null, typeof(Rigidbody)));
            }
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("enable", 0));
                slots.Add(new LogicSlot("disable", 1));
                slots.Add(new LogicSlot("set layer", 2, typeof(float), null));
                slots.Add(new LogicSlot("set layermask", 3, typeof(int), null));
            }

#endif
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
}