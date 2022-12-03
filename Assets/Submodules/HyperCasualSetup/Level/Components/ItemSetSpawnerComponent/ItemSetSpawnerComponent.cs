using System;
using System.Collections.Generic;
using Mobge;
using Mobge.Core;
using UnityEngine;

namespace Mobge.HyperCasualSetup
{
    public class ItemSetSpawnerComponent : ComponentDefinition<ItemSetSpawnerComponent.Data>
    {
        [Serializable]
        public class Data : Mobge.Core.Components.PrefabSpawnerComponent.BaseData
        {
            public ItemSet set;


            //[SerializeField] [HideInInspector] private LogicConnections _connections;
            public override void Start(in InitArgs initData) {
                var player = (BaseLevelPlayer)initData.player;
                var pf = GetPrefab(player.Context);
                _instance = Instantiate(pf);
                _instance.SetParent(initData.parentTr);
                _instance.localPosition = position;
                _instance.localRotation = _rotation;
                _instance.localScale = Vector3.Scale(pf.localScale, _scale);

            }
            private Transform GetPrefab (AGameContext context){
                var item = set.GetEquippedItem(context, out _);
                return GetPrefab(item);
            }
            private Transform GetPrefab(ItemSet.Item item)
            {
                var c = item.contents[0];
                Transform t = null;
                if (c is GameObject go)
                {
                    t = go.transform;
                }
                else if (c is Component comp)
                {
                    t = comp.transform;
                }
                return t;
            }
#if UNITY_EDITOR
            public override Transform PrefabReference {
                get {
                    if(set == null)
                    {
                        return null;
                    }
                    AGameContext context = FindObjectOfType<AGameContext>();
                    if (context == null)
                    {
                        return null;
                    }
                    int index = set.editorEquipped;
                    if (index < 0 || index >= set.items.Count)
                    {
                        return null;
                    }
                    var item = set.items[index];
                    
                    if(item.contents ==null || item.contents.Length == 0)
                    {
                        return null;
                    }
                    return GetPrefab(item);
                }
            }
#endif
            // public override LogicConnections Connections {
            //     get => _connections;
            //     set => _connections = value;
            // }
        }
    }
}