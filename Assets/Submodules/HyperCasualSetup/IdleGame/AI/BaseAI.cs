using System;
using System.Collections;
using System.Collections.Generic;
using Mobge.IdleGame.CustomerShopSystem;
using UnityEngine;

namespace Mobge.IdleGame.AI
{
    public class BaseAI : MonoBehaviour
    {
        public Transform Target => _target;
        private Transform _target;

        public ShopSpawnerComponent.Data TargetShop => _targetShop;
        private ShopSpawnerComponent.Data _targetShop;

        public UnityEngine.AI.NavMeshPath Path => _agent.path;
        [SerializeField] private UnityEngine.AI.NavMeshPath _path;

        [SerializeField] private UnityEngine.AI.NavMeshAgent _agent;

        public List<CustomerTarget> currentTargetQueue => _targetQueue;

        [SerializeField] private List<CustomerTarget> _targetQueue;

        public int currentTargetIndex => _currentTargetIndex;
        private int _currentTargetIndex = 0;

        public void SetNextTarget(Transform target)
        {
            SetTarget(target);
            _currentTargetIndex++;
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            RecalculatePath();
        }

        public void SetShop(ShopSpawnerComponent.Data target)
        {
            _targetShop = target;
        }

        public void ResetCustomer()
        {
            _currentTargetIndex = 0;
        }

        public void RecalculatePath()
        {
            if (_target)
            {
                _agent.SetDestination(_target.position);
                _agent.isStopped = true;
            }
        }
    }
}