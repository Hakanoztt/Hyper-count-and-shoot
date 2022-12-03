using System;
using System.Collections.Generic;
using Mobge.Serialization;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Mobge.Core.Components {
    public interface IPlatformComponentData {
        Polygon[] GetPolygons();
        Transform Transform { get; }
        Rigidbody2D Rigidbody { get; }
    }
    public abstract class BasePlatformComponent<T> : ComponentDefinition<T> where T : BasePlatformComponent<T>.Data {
        [Serializable]
        public abstract class Data : BaseComponent, IParent, ISaveable, IChild, IRotationOwner, IResourceOwner, IPlatformComponentData {
            public PlatformWorkMode Mode { get => (PlatformWorkMode)_mode; set => _mode = (int)value; }
            [SerializeField] private int _mode;
            public bool disableOnStart = false;
            public int visualsIndex;
            [SerializeField] public AssetReferencePolygonVisualizer polygonVisualizer; //todo: refactor to overridePolygonVisualizer
            public PhysicsMaterial2D physicsMaterial;
            public CollisionDetectionMode2D collisionDetectionMode;
            public float mass;
            public float drag;
            public float gravityScale = 1;
            public RigidbodyConstraints2D bodyConstrainst;
            public Color color = Color.white;
            public int subdivisionCount = 12;


            private Rigidbody2D _rb;
            public Rigidbody2D Rigidbody => _rb;

            [Layer] public int layer;
            int IResourceOwner.ResourceCount => (polygonVisualizer != null && polygonVisualizer.RuntimeKeyIsValid()) ? 1 : 0;
            AssetReference IResourceOwner.GetResource(int index) => polygonVisualizer;
            public Transform Transform => _renderer?.Transform;
            protected IPolygonRenderer _renderer;
            public IPolygonRenderer Renderer => _renderer;
            ElementReference IChild.Parent { get => parent.id; set => parent = value; }
            [SerializeField] [HideInInspector] private ElementReference parent = -1;
            public Quaternion Rotation { get => _rotation; set => _rotation = value; }
            [SerializeField] [HideInInspector] private Quaternion _rotation = Quaternion.identity;


            public abstract Polygon[] GetPolygons();

            public override void Start(in InitArgs initData) {
                SetupRenderer(initData);
                SetupPhysics(initData);
            }
            private void SetupRenderer(in InitArgs initData) {
                _renderer = CreateRenderer(
                    initData.player.level, 
                    initData.parentTr,
                    position, 
                    _rotation, 
                    Mode == PlatformWorkMode.Fixed);
                
                if (_renderer == null) {
                    var tr = new GameObject("Dummy Platform Component").transform;
                    tr.SetParent(initData.parentTr, false);
                    tr.localPosition = position;
                    tr.localRotation = _rotation;
                    var dr = new DummyRenderer {tr = tr};
                    _renderer = dr;
                }
                
                if (disableOnStart && Mode != PlatformWorkMode.Fixed) {
                    _renderer.Transform.gameObject.SetActive(false);
                }
            }
            private void SetupPhysics(in InitArgs initData) {
                GameObject physics;
                Vector3 origin;
                bool hasParent = initData.parentTr != initData.player.transform;
                var visual = Mode == PlatformWorkMode.Visual;
                bool hasColliderRotation;
                if ((Mode == PlatformWorkMode.Fixed) || visual) {
                    physics = initData.player.PhysicsRoot.gameObject;
                    origin = this.position;
                    hasColliderRotation = true;
                } else {
                    physics = _renderer.Transform.gameObject;
                    origin = Vector3.zero;
                    hasColliderRotation = false;
                    if (!hasParent) {
                        _rb = physics.AddComponent<Rigidbody2D>();
                        _rb.mass = mass;
                        _rb.gravityScale = gravityScale;
                        _rb.bodyType = Mode == PlatformWorkMode.Dynamic ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
                        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                        _rb.constraints = bodyConstrainst;
                        _rb.collisionDetectionMode = collisionDetectionMode;
                        _rb.drag = drag;
                    }
                }
                if (Mode != PlatformWorkMode.Fixed) {
                    var tr = _renderer.Transform;
                    tr.gameObject.layer = layer;
                    for (int i = 0; i < tr.childCount; i++) {
                        var go = tr.GetChild(i).gameObject;
                        go.layer = layer;
                    }
                }
                if (visual) return;
                var polygons = GetPolygons();
                for (int i = 0; i < polygons.Length; i++) {
                    var p = polygons[i];
                    if (p.noCollider) continue;
                    PolygonCollider2D col;
                    if (hasColliderRotation) {
                        col = PolygonUtilities.AddCollider(physics, p, _rotation);
                    }
                    else {
                        col = PolygonUtilities.AddCollider(physics, p);
                    }
                    col.sharedMaterial = physicsMaterial;
                    col.offset = origin;
                }
            }
            public IPolygonRenderer CreateRenderer(Level level, Transform parentTransform, Vector3 offset, Quaternion rotation, bool final) {
                var visualizer = GetVisualizer(level);
                if (visualizer == null) return null;
                return visualizer.Visualize(GetPolygons(), offset, rotation, final, parentTransform, color);
            }
            private IPolygonVisualizer GetVisualizer(Level level) {
                if (polygonVisualizer != null && polygonVisualizer.RuntimeKeyIsValid()) return polygonVisualizer.LoadedAsset;
                var decorationSet = level.decorationSet.LoadedAsset;
                if (decorationSet == null) return null;
                var visualizer = decorationSet.GetPolygonVisualizer(visualsIndex);
                return visualizer;
            }
            public void UpdateRenderer(Level level, IPolygonRenderer polygonRenderer) {
                var visualizer = GetVisualizer(level);
                if (visualizer == null) return;
                visualizer.UpdateVisuals(polygonRenderer, GetPolygons());
                polygonRenderer.Color = color;
            }
            
            object ISaveable.State {
                get {
                    if (_rb == null) {
                        return null;
                    }
                    return new BasicPhysical2DState(_rb);
                }
                set {
                    if (_rb != null && value is BasicPhysical2DState s) {
                        s.ApplyState(_rb);
                    }
                }
            }


            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case 0:     return _rb;                                      
                    case 1:     _rb.bodyType = RigidbodyType2D.Dynamic;          break;
                    case 2:     _rb.bodyType = RigidbodyType2D.Kinematic;        break;
                    case 3:     _renderer.Transform.gameObject.SetActive(true);  break;
                    case 4:     _renderer.Transform.gameObject.SetActive(false); break;
                    case 5:     _rb.position = (Vector3)input;                   break;
                    case 6:     _renderer.Color = (Color)input;                  break;
                    case 7:     return _renderer.Transform;
                    case -1024: return this;
                }
                return null;
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                if (Mode == PlatformWorkMode.Fixed) return;
                slots.Add(new LogicSlot("get rigidbody", 0, null, typeof(Rigidbody2D)));
                slots.Add(new LogicSlot("go dynamic", 1));
                slots.Add(new LogicSlot("go kinematic", 2));
                slots.Add(new LogicSlot("set enabled", 3));
                slots.Add(new LogicSlot("set disabled", 4));
                slots.Add(new LogicSlot("set position", 5, typeof(Vector3)));
                slots.Add(new LogicSlot("set color", 6, typeof(Color)));
                slots.Add(new LogicSlot("get transform", 7, null, typeof(Transform), true));
                slots.Add(new LogicSlot("this", -1024, null, GetType(), true));
            }

#endif
        }

        private class DummyRenderer : IPolygonRenderer {
            public Transform tr;
            public Color Color { get => Color.white; set { } }
            Transform IPolygonRenderer.Transform => tr;
        }
    }

    [Flags]
    public enum PlatformWorkMode {
        Fixed = 0,
        Dynamic = 1,
        Kinematic = 2,
        Visual = 3,
    }
}

