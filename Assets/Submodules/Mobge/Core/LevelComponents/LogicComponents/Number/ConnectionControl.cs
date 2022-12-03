using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components
{
    public class ConnectionControl : ComponentDefinition<ConnectionControl.Data>
    {
        [Serializable]
        public class Data : BaseComponent
        {
            public const int c_idInput = 100;
            public const int c_idEnableAll = 101;
            public const int c_idDisableAll = 102;
            public const int c_idSelectIndex = 103;
            [SerializeField, HideInInspector]
            private LogicConnections _connections;
            public override LogicConnections Connections { get => _connections; set => _connections = value; }
            public int connectionCount;
            public int defaultIndex = 0;
            private int _selectedIndex;
            private Dictionary<int, BaseComponent> _components;

            public override void Start(in InitArgs initData) {
                _selectedIndex = defaultIndex;
                _components = initData.components;
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case c_idInput:
                        return HandleInput(input);
                    case c_idEnableAll:
                        _selectedIndex = -1;
                        break;
                    case c_idDisableAll:
                        _selectedIndex = -2;
                        break;
                    case c_idSelectIndex:
                        _selectedIndex = (int)(float)input;
                        break;
                    default:
                        _selectedIndex = index;
                        break;
                }
                return null;
            }
            public void Trigger(object @param) {
                HandleInput(@param);
            }

            private object HandleInput(object input) {
                if(_connections == null) {
                    return null;
                }
                switch (_selectedIndex) {
                    case -1:
                        var allConnections = _connections.List;
                        object returnVal = null;
                        for(int i = 0; i < allConnections.Count; i++) {
                            var c = allConnections[i];
                            returnVal = _components[c.target].HandleInput(this, c.input, input);
                        }
                        return returnVal;
                    case -2:
                        return null;
                    default:
                        return _connections.InvokeSimple(this, _selectedIndex, input, _components);
                }
            }
#if UNITY_EDITOR
            [NonSerialized] public Type e_type;
            [NonSerialized] public Type r_type;
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("input", c_idInput));
                slots.Add(new LogicSlot("enable all", c_idEnableAll));
                slots.Add(new LogicSlot("disable all", c_idDisableAll));
                slots.Add(new LogicSlot("select", c_idSelectIndex));
                for(int i = 0; i < connectionCount; i++) {
                    slots.Add(new LogicSlot("select: " + i, i));
                }
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                for (int i = 0; i < connectionCount; i++) {
                    slots.Add(new LogicSlot("output: " + i, i, e_type));
                }
            }
#endif
        }

    }
}