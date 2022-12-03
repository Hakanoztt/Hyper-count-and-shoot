using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Mobge.Core.Components
{
    public class PlatformOutlineComponent : ComponentDefinition<PlatformOutlineComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent, IResourceOwner
        {
            public Color color = Color.white;
            private ExposedList<float> s_backupScales = new ExposedList<float>();

            [SerializeField, HideInInspector] private LogicConnections _connections;

            [SerializeField, HideInInspector] public AssetReferencePolygonVisualizer polygonVisualizer;
            public float zOffset = -0.02f;
            public float skinScale = 1;
            private List<IPolygonRenderer> _renderers;
            private LevelPlayer _player;
            private Dictionary<int, BaseComponent> _components;
            private Color _currentColor;
            private IPlatformComponentData _platform;
            public override void Start(in InitArgs initData) {
                _renderers = new List<IPolygonRenderer>();
                _currentColor = color;
                _player = initData.player;
                _components = initData.components;
                _platform = _connections.InvokeSimple(this, 0, null, _components) as IPlatformComponentData;
                _player.FixedRoutineManager.DoAction(DelayedInit, 0);
            }

            private void DelayedInit(bool completed, object data) {


                var c = _platform;
                var pols = c.GetPolygons();
                var pv = polygonVisualizer.Asset as IPolygonVisualizer;
                s_backupScales.SetCountFast(pols.Length);
                for(int i = 0; i < pols.Length; i++) {
                    s_backupScales.array[i] = pols[i].skinScale;
                    pols[i].skinScale = skinScale;
                }
                var rend = pv.Visualize(pols, new Vector3(0, 0, zOffset), Quaternion.identity, false, c.Transform, _currentColor);

                for (int i = 0; i < pols.Length; i++) {
                    pols[i].skinScale = s_backupScales.array[i];
                }
                _renderers.Add(rend);
            }


            public override LogicConnections Connections {
                get => _connections;
                set => _connections = value;
            }

            int IResourceOwner.ResourceCount => 1;
            AssetReference IResourceOwner.GetResource(int index) {
                return polygonVisualizer;
            }
            private void SetColor(Color color) {
                if (color != _currentColor) {
                    _currentColor = color;
                    for (int i = 0; i < _renderers.Count; i++) {
                        _renderers[i].Color = color;
                    }
                }
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    default:
                    case 0:
                        SetColor((Color)input);
                        break;
                    case 1:
                        return _platform.Rigidbody;
                }
                return null;
            }

#if UNITY_EDITOR
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("platforms", 0, null, typeof(IPlatformComponentData)));
            }
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("set color", 0, typeof(Color), null));
                slots.Add(new LogicSlot("get platform body", 1, null, typeof(Rigidbody2D)));
            }
#endif
        }
    }
}