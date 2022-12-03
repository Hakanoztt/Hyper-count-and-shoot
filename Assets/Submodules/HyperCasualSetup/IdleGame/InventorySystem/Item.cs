using Mobge.Core;
using Mobge.Core.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mobge.IdleGame {
    public class Item : MonoBehaviour, IComponentExtension {
        public object Container { get; private set; }
        public int IdInContainer { get; private set; }

        public Item PrefabReference { get; set; }

        [SerializeField] private ItemClass _class;

        public ItemClass Class => _class;

        internal static void SetOwner(Item item, object container, int id) {
            if (item.Container != null) {
                throw new System.Exception("Cannot set owner of an " + typeof(Item) + " which already has an owner");
            }
            item.Container = container;
            item.IdInContainer = id;
        }
        internal static void ReleaseOwner(Item item, object container) {
            if(container != item.Container) {
                throw new System.Exception("Cannot release an " + typeof(Item) + " without specifiying its container.");
            }

            item.Container = null;
            item.IdInContainer = -1;
        }


        void IComponentExtension.Start(in BaseComponent.InitArgs initData) {
            var spawner = initData.GetOwnComponent<BaseComponent>();
            PrefabReference = spawner.PrefabReference.GetComponent<Item>();
        }
    }
}