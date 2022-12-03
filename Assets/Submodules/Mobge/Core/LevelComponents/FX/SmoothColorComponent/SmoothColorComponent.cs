using System;
using System.Collections.Generic;
using Mobge;
using Mobge.Core;
using UnityEngine;

namespace Mobge.Core.Components
{
    public class SmoothColorComponent : ComponentDefinition<SmoothColorComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent
        {
            public float smoothTime = 0.25f;
            public Color startColor = new Color(1, 1, 1, 1);
            public bool fireOnAwake;
            [SerializeField] [HideInInspector] private LogicConnections _connections;

            private Color _currentColor;
            private Color _startColor;
            private Color _targetColor;
            private Dictionary<int, BaseComponent> _components;
            private LevelPlayer _player;
            private RoutineManager.Routine _swapAction;

            public override void Start(in InitArgs initData) {
                _currentColor = startColor;
                _components = initData.components;
                _player = initData.player;
                if (fireOnAwake) {
                    _player.FixedRoutineManager.DoAction(DelayedInit, 0);
                }
            }

            private void DelayedInit(bool completed, object data) {
                _connections.InvokeSimple(this, 0, _currentColor, _components);
            }

            public override LogicConnections Connections {
                get => _connections;
                set => _connections = value;
            }

            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    default:
                    case 0:
                        _swapAction.Stop();
                        _targetColor = (Color)input;
                        _startColor = _currentColor;
                        _swapAction = _player.FixedRoutineManager.DoAction(null, smoothTime, UpdateColor);
                        break;
                    case 1:
                        _swapAction.Stop();
                        _currentColor = (Color)input;
                        _connections.InvokeSimple(this, 0, _currentColor, _components);
                        break;
                }
                return null;
            }

            private void UpdateColor(float progress, object data) {
                _currentColor = Color.LerpUnclamped(_startColor, _targetColor, progress);
                _connections.InvokeSimple(this, 0, _currentColor, _components);
            }


#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("lerp to color", 0, typeof(Color)));
                slots.Add(new LogicSlot("lerp to color immediately", 1, typeof(Color)));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("on color change", 0, typeof(Color)));
            }
#endif
        }
    }
}