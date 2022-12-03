using System;
using System.Collections;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.Core.Components;
using UnityEngine;

namespace Mobge.IdleGame.CustomerShopSystem
{
    public class CustomerSpawner : MonoBehaviour, IComponentExtension
    {
        private LevelPlayer _player;

        void IComponentExtension.Start(in BaseComponent.InitArgs initData)
        {
            _player = initData.player;
            _player.FixedRoutineManager.DoAction(InitSpawner);
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