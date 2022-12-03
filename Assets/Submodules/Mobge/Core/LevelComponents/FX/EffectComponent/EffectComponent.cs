using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components {
    public class EffectComponent : ComponentDefinition<EffectComponent.Data> {
        [Serializable]
        public class Data : BaseComponent , IChild, IRotationOwner {
            public ReusableReference effect;
            public bool playOnAwake;
            [SerializeField]
            [HideInInspector]
            private ElementReference _parent = -1;
            ElementReference IChild.Parent { get => _parent; set => _parent = value; }
            Quaternion IRotationOwner.Rotation { get => rotation; set => rotation = value; }
            public Quaternion rotation = Quaternion.identity;
            public Vector3 scale = Vector3.one;

            private Transform _tr;
            public override void Start(in InitArgs initData) {
                _tr = initData.parentTr;
                if (playOnAwake) {
                    Play(position, _tr);
                }
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch(index) {
                    case 0:
                        return Play(position, _tr);
                    case 1:
                        effect.Stop();
                        break;
                    case 2:
                        effect.StopImmediately();
                        break;
                    case 3:
                        var v = (Vector3) input;
                        return Play(v, _tr);
                }
                return null;
            }

            private Transform Play(Vector3 pos, Transform parent) {
                var reusable = effect.SpawnItem(pos, parent);
                if (reusable == null) return null;
                var tr = reusable.transform;
                tr.localScale = scale;
                tr.localRotation = rotation;
                return tr;
            }
            public override Transform PrefabReference {
                get {
                    var rf = effect.ReferenceItem;
                    return rf != null ? rf.transform : null;
                }
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("play", 0, null, typeof(Transform)));
                slots.Add(new LogicSlot("play at position", 3, typeof(Vector3), typeof(Transform)));
                slots.Add(new LogicSlot("stop", 1, null, typeof(Transform)));
                slots.Add(new LogicSlot("stop immediately", 2, null, typeof(Transform)));
            }
#endif
        }
    }

}