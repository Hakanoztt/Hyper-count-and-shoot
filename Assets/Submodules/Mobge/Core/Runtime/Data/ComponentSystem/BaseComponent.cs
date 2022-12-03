using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core {
    public abstract class BaseComponent : ILogicComponent
    {
        public Vector3 position;

        public virtual Transform PrefabReference { get => null; }
        public virtual LogicConnections Connections { get => null; set { } }
        public virtual void Load(in LevelPlayer.LoadArgs loadArgs) {
            
        }
        public abstract void Start(in InitArgs initData);
        public virtual void End() {

        }
        public virtual object HandleInput(ILogicComponent sender, int index, object input)
        {
            return this;
        }
        public struct InitArgs {
            public Dictionary<int, BaseComponent> components;
            public Transform parentTr;
            public LevelPlayer player;
            /// <summary>
            /// Unique id among all direct and nested components inside level.
            /// </summary>
            public int id;
            /// <summary>
            /// Visible id from level editor. It is Unique only for containing piece.
            /// </summary>
            public int componentId;
            public T GetComponent<T>(int id) where T : BaseComponent {
                return components[id] as T;
            }
            public T GetOwnComponent<T>() where T : BaseComponent {
                return GetComponent<T>(id);
            }
        }
#if UNITY_EDITOR

        public virtual void EditorInputs(List<LogicSlot> slots) {
        }
        public virtual void EditorOutputs(List<LogicSlot> slots) {
        }
#endif
    }
}