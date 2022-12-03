using System;
using System.Collections.Generic;
using Mobge.Core;
using UnityEngine;

namespace Mobge.IdleGame.CustomerShopSystem
{
    public class ShopSpawnerComponent : ComponentDefinition<ShopSpawnerComponent.Data>
    {
        [Serializable]
        public class Data : UnitSpawnerComponent.Data
        {
            [Range(0, 1f)]
            public float[] customerRate;

            [Range(0, 100)]
            public int[] customerLimit;

            public int currentCustomers;

            public int currentCustomerLimit => customerLimit[Rank];
            public float currentCustomerRate => customerRate[Rank];

            public bool isAvaliableForCustomer
            {
                get
                {
                    return currentCustomers < currentCustomerLimit;
                }
            }

            private ShopStackDepositer _stackDepositer;
            private Dictionary<int, BaseComponent> _components;

            public override void Start(in InitArgs initData)
            {
                base.Start(initData);

                _components = initData.components;

                _stackDepositer = this.Instance.GetComponentInChildren<ShopStackDepositer>();

                _player.FixedRoutineManager.DoAction(InitShop);
            }

            private void InitShop(bool complete, object data)
            {
                if (complete)
                {
                    _player.TryGetExtra<CustomerSpawnControllerComponent.Data>(CustomerSpawnControllerComponent.Data.cs_key, out var customerSpawnData);
                    customerSpawnData.Register(this);
                }
            }

            public Transform GetRandomStack()
            {
                if (!_stackDepositer)
                {
                    return null;
                }

                return _stackDepositer.GetRandomStack();
            }


            //public override object HandleInput(ILogicComponent sender, int index, object input) {
            //    switch (index) {
            //        case 0:
            //            return "example output";
            //    }
            //    return null;
            //}
#if UNITY_EDITOR
            //public override void EditorInputs(List<LogicSlot> slots) {
            //    slots.Add(new LogicSlot("example input", 0));
            //}
            //public override void EditorOutputs(List<LogicSlot> slots) {
            //    slots.Add(new LogicSlot("example output", 0));
            //}
#endif
        }
    }
}
