using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core.Components
{
    public class TextMeshComponent : ComponentDefinition<TextMeshComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent, IRotationOwner, IVisualSpawner
        {
            public string text;
            public TextMeshVisual textMeshRes;
            [SerializeField] private Quaternion _rotation;
            public Vector3 scale = Vector3.one;
            public Color color = new Color(1, 1, 1, 1);
            private TextMeshVisual _instance;


            public override void Start(in InitArgs initData) {
                _instance = Instantiate(textMeshRes);
                var ttr= _instance.transform;
                ttr.SetParent(initData.parentTr, false);
                ttr.localPosition = position;
                ttr.localRotation = _rotation;
                UpdateVisuals(_instance);
            }



            //public override Transform PrefabReference {
            //    get {
            //        if (!textMeshRes) {
            //            return null;
            //        }
            //        return textMeshRes.transform;
            //    }
            //}

            Quaternion IRotationOwner.Rotation { get => _rotation; set => _rotation = value; }

            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    default:
                    case 0:
                        _instance.Text = (string)input;
                        break;
                }
                return null;
            }
            private void UpdateVisuals(TextMeshVisual v) {
                v.Text = text;
                v.transform.localScale = scale;
                v.Color = color;
            }

#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("set text", 0, typeof(string)));
            }

            Transform IVisualSpawner.CreateVisuals() {
                if (textMeshRes) {
                    return Instantiate(textMeshRes).transform;
                }
                return null;
            }

            void IVisualSpawner.UpdateVisuals(Transform instance) {
                if (instance) {
                    instance.localScale = scale;
                    var v = instance.GetComponent<TextMeshVisual>();
                    if (v) {
                        UpdateVisuals(v);
                    }
                }
            }
#endif
        }
    }
}