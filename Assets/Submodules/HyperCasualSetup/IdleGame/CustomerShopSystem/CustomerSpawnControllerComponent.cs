using System;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.IdleGame.AI;
using UnityEngine;

namespace Mobge.IdleGame.CustomerShopSystem
{
    public class CustomerSpawnControllerComponent : ComponentDefinition<CustomerSpawnControllerComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent
        {
            public const string cs_key = "cs_key";
            //public override LogicConnections Connections { get => connections; set => connections = value; }
            //[SerializeField] [HideInInspector] private LogicConnections connections;
            //private Dictionary<int, BaseComponent> _components;

            // private LevelPlayer _player;

            [Space(10)]
            [Header("Customer Spawn Controller Values")]
            [Range(0f, 1f)]
            public float spawnRate;

            public float spawnCooltime;

            [Space(5)]
            private List<ShopSpawnerComponent.Data> _shops;

            private List<CustomerSpawner> _spawnZones;
            private Dictionary<CustomerTarget, List<Transform>> _triggers;

            private LevelPlayer _player;

            private int _maxCustomer = 0;
            private float _allChance = 0;

            private int _currentCustomers = 0;

            [SerializeField] private CustomerList _customerList;

            public override void Start(in InitArgs initData)
            {
                _player = initData.player;
                _player.SetExtra(cs_key, this);

                _triggers = new Dictionary<CustomerTarget, List<Transform>>();
                _spawnZones = new List<CustomerSpawner>();
                _shops = new List<ShopSpawnerComponent.Data>();

                _player.RoutineManager.DoAction(InitSpawners, 0.1f);
            }

            private void InitSpawners(bool complete, object data)
            {
                if (!complete)
                {
                    return;
                }

                _player.FixedRoutineManager.DoAction(SpawnCustomerLoop, spawnCooltime);
            }

            public void Register(CustomerSpawner cSpawner)
            {
                _spawnZones.Add(cSpawner);
            }

            public void Register(CustomerAction cAction)
            {
                cAction.customerTargetAciton = SetCustomerNextTarget;
                List<Transform> actionList;

                if (_triggers.ContainsKey(cAction.target))
                {
                    actionList = _triggers[cAction.target];
                    actionList.Add(cAction.transform);
                }
                else
                {
                    actionList = new List<Transform>();
                    actionList.Add(cAction.transform);

                    _triggers.Add(cAction.target, actionList);
                }
            }

            public void Register(ShopSpawnerComponent.Data shopData)
            {
                _shops.Add(shopData);
            }

            private void UpdateCustomerLimit()
            {
                _maxCustomer = 0;

                for (int i = 0; i < _shops.Count; i++)
                {
                    if (_shops[i].Instance.gameObject.activeInHierarchy)
                    {
                        _maxCustomer += _shops[i].currentCustomerLimit;
                    }
                }
            }

            private void UpdateShopChance()
            {
                _allChance = 0;

                for (int i = 0; i < _shops.Count; i++)
                {
                    if (_shops[i].Instance.gameObject.activeInHierarchy && _shops[i].isAvaliableForCustomer)
                    {
                        _allChance += _shops[i].currentCustomerRate;
                    }
                }
            }

            private void SpawnCustomerLoop(bool complete, object data)
            {
                if (_shops.Count == 0)
                {
                    _player.FixedRoutineManager.DoAction(SpawnCustomerLoop, spawnCooltime);
                    return;
                }

                UpdateCustomerLimit();

                if (_currentCustomers < _maxCustomer)
                {
                    if (UnityEngine.Random.Range(0, 1f) < spawnRate && CanSpawn())
                    {
                        UpdateShopChance();

                        BaseAI currentAi = CustomerManager.GetCache(_player).InstantiateAi(GetRandomCustomer(), _player);

                        currentAi.transform.position = GetRandomStartPosition().position;
                        currentAi.SetShop(GetRandomShop());
                        currentAi.SetTarget(currentAi.TargetShop.GetRandomStack());
                        _currentCustomers++;
                    }
                }

                _player.FixedRoutineManager.DoAction(SpawnCustomerLoop, spawnCooltime);
            }


            #region Get Functions

            public BaseAI GetRandomCustomer()
            {
                BaseAI ai = null;

                float allCustomerChance = 0;

                for (int i = 0; i < _customerList.customers.Count; i++)
                {
                    allCustomerChance += _shops[i].currentCustomerRate;
                }

                float customerChance = UnityEngine.Random.Range(0f, allCustomerChance);
                float currenctChance = 0f;

                for (int i = 0; i < _customerList.customers.Count; i++)
                {
                    currenctChance += _customerList.customers[i].customerRate;

                    if (customerChance <= currenctChance)
                    {
                        ai = _customerList.customers[i].customerPrefabs[UnityEngine.Random.Range(0, _customerList.customers[i].customerPrefabs.Length)];
                        break;
                    }

                }

                return ai;
            }

            public List<ShopSpawnerComponent.Data> GetAvaliableShops()
            {
                List<ShopSpawnerComponent.Data> avaliableShops = new List<ShopSpawnerComponent.Data>();

                for (int i = 0; i < _shops.Count; i++)
                {
                    ShopSpawnerComponent.Data avaliableShop = _shops[i];

                    if (avaliableShop.isAvaliableForCustomer && avaliableShop.IsVisible)
                    {
                        avaliableShops.Add(avaliableShop);
                    }
                }

                return avaliableShops;
            }

            private ShopSpawnerComponent.Data GetRandomShop()
            {
                List<ShopSpawnerComponent.Data> avaliableShops = GetAvaliableShops();

                ShopSpawnerComponent.Data shop = null;

                float shopChance = UnityEngine.Random.Range(0f, _allChance);
                float currenctChance = 0f;

                for (int i = 0; i < avaliableShops.Count; i++)
                {
                    currenctChance += avaliableShops[i].currentCustomerRate;

                    if (shopChance <= currenctChance)
                    {
                        shop = avaliableShops[i];
                        break;
                    }

                }

                if (shop != null)
                {
                    shop.currentCustomers++;

                    return shop;
                }

                return null;

            }

            private Transform GetRandomStartPosition()
            {
                return _spawnZones[UnityEngine.Random.Range(0, _spawnZones.Count)].transform;
            }

            #endregion

            private bool CanSpawn()
            {
                if (_spawnZones.Count == 0)
                {
                    return false;
                }

                if (_triggers.Count == 0)
                {
                    return false;
                }

                return true;
            }

            private void SetCustomerNextTarget(BaseAI customer)
            {
                CustomerTarget target = null;

                int nextTarget = customer.currentTargetIndex;
                nextTarget++;

                if (customer.currentTargetQueue.Count > nextTarget)
                {
                    target = customer.currentTargetQueue[nextTarget];
                }

                //Exit Condition
                if (!target)
                {
                    CustomerExit(customer);
                    return;
                }

                customer.SetNextTarget(_triggers[target][UnityEngine.Random.Range(0, _triggers[target].Count)].transform);
            }

            private void CustomerExit(BaseAI customer)
            {
                customer.TargetShop.currentCustomers--;
                _currentCustomers--;

                CustomerManager.GetCache(_player).DestroyAi(customer);
            }


            public override object HandleInput(ILogicComponent sender, int index, object input)
            {
                switch (index)
                {
                    case 0:
                        return this;
                    default:
                        return this;
                }
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots)
            {
                slots.Add(new LogicSlot("this", 0, null, typeof(CustomerSpawnControllerComponent.Data)));
            }
            //public override void EditorOutputs(List<LogicSlot> slots) {
            //    slots.Add(new LogicSlot("example output", 0));
            //}
#endif
        }
    }
}
