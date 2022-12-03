using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public interface ILogicComponent
    {
        object HandleInput(ILogicComponent sender, int index, object input);
        LogicConnections Connections { get; set; }
        #if UNITY_EDITOR
        void EditorInputs(List<LogicSlot> slots);
        void EditorOutputs(List<LogicSlot> slots);
        #endif
    }
}