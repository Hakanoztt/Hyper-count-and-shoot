using System.Collections.Generic;
using Mobge.IdleGame.AI;
using UnityEngine;

namespace Mobge.IdleGame.CustomerShopSystem
{
    [CreateAssetMenu(fileName = "CustomerList", menuName = "Hyper Casual/Idle Game/Customer List")]
    public class CustomerList : ScriptableObject
    {
        public List<CustomerType> customers;
    }

    [System.Serializable]
    public struct CustomerType
    {
        public string customerName;
        public float customerRate;
        public BaseAI[] customerPrefabs;
    }
}