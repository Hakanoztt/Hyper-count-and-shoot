using System;
using System.Collections;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.Core.Components;
using Mobge.IdleGame.AI;
using UnityEngine;

namespace Mobge.IdleGame.CustomerShopSystem
{
    public class CustomerAction : MonoBehaviour, IComponentExtension
    {
        public const string c_key = "Customer";

        public CustomerTarget target;

        public Action<BaseAI> customerTargetAciton;
        private LevelPlayer _player;

        void IComponentExtension.Start(in BaseComponent.InitArgs initData)
        {
            _player = initData.player;
            _player.FixedRoutineManager.DoAction(InitSpawner);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(c_key) && other.TryGetComponent(out BaseAI ai))
            {
                if (customerTargetAciton == null)
                {
                    if (TryGetComponent(out AnimationBasedUnit unit))
                    {
                        _player = unit.GetLevelPlayer();
                        InitSpawner(true, null);
                    }
                }

                if (ai.currentTargetQueue[ai.currentTargetIndex] == this.target)
                {
                    customerTargetAciton(ai);
                }
            }
        }

        private void InitSpawner(bool complete, object data)
        {
            if (complete)
            {
                _player.TryGetExtra<CustomerSpawnControllerComponent.Data>(CustomerSpawnControllerComponent.Data.cs_key, out var customerSpawnData);

                customerSpawnData.Register(this);
            }
        }
    }
}