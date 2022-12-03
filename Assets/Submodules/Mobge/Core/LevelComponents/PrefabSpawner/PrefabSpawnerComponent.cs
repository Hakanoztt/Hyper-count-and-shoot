using Mobge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Mobge.Core.Components
{
    public class PrefabSpawnerComponent : ComponentDefinition<PrefabSpawnerComponent.Data>
    {
        [Serializable]
        public class Data : BaseData, IParent
        {
            [LabelPicker, HideInInspector] public Component res;

            public override void Start(in InitArgs initData)
            {
                var ins = Instantiate(res, initData.parentTr, false);
                _instance = ins.transform;
                _instance.gameObject.SetActive(false);
                // _instance.SetParent(initData.parentTr, false);
                _instance.localPosition = Divide(position, initData.parentTr.localScale);
                _instance.localRotation = _rotation;
                _instance.localScale = Divide(_scale, initData.parentTr.localScale);
                _instance.gameObject.SetActive(true);

                if (ins.TryGetComponent<IComponentExtension>(out var extension)) {
                    extension.Start(initData);
                }
            }
            public Vector3 Divide(Vector3 v1, Vector3 divider) {
                return new Vector3(v1.x / divider.x, v1.y / divider.y, v1.z / divider.z);
            }
            public override Transform PrefabReference {
                get {
                    if (res != null) {
                        if(res.GetType().GetCustomAttribute<HiddenOnEditorAttribute>() == null) {
                            return res.transform;
                        }
                    }
                    return null;
                }
            }
            public Transform Instance => _instance;
            Transform IParent.Transform => _instance;
        }
        public abstract class BaseData : BaseComponent, IRotationOwner, IChild
        {
            public Quaternion Rotation { get => _rotation; set => _rotation = value; }
            [SerializeField, HideInInspector] protected Quaternion _rotation = Quaternion.identity;
            public Vector3 Scale { get => _scale; set => _scale = value; }
            [SerializeField, HideInInspector] protected Vector3 _scale = new Vector3(1, 1, 1);
            ElementReference IChild.Parent { get => _parent.id; set => _parent = value; }
            [SerializeField] [HideInInspector] private ElementReference _parent = -1;
            // public Transform Transform => _instance;
            protected Transform _instance;

            public override object HandleInput(ILogicComponent sender, int index, object input)
            {
                switch (index)
                {
                    default:
                    case 0:
                        return _instance;
                    case 1:
                        return _instance.GetComponent<Rigidbody>();
                    case 4:
                        return _instance.GetComponent<Rigidbody2D>();
                    case 2:
                        _instance.gameObject.SetActive(true);
                        break;
                    case 3:
                        _instance.gameObject.SetActive(false);
                        break;
                }
                return null;
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots)
            {
                slots.Add(new LogicSlot("get transform", 0, null, typeof(Transform)));
                var pref = PrefabReference;
                if (pref != null) {
                    var rb3 = pref.GetComponent<Rigidbody>();
                    if (rb3) {
                        slots.Add(new LogicSlot("get rigidbody", 1, null, typeof(Rigidbody)));
                    }
                    var rb2 = pref.GetComponent<Rigidbody2D>();
                    if (rb2) {
                        slots.Add(new LogicSlot("get rigidbody", 4, null, typeof(Rigidbody2D)));
                    }
                }
                slots.Add(new LogicSlot("activate", 2));
                slots.Add(new LogicSlot("deactivate", 3));
            }
#endif
        }
    }
}