using System.Collections;
using System.Collections.Generic;
using Mobge.IdleGame;
using UnityEngine;


namespace Mobge.IdleGame.CustomerShopSystem
{
    public class ShopStackDepositer : MonoBehaviour
    {
        private List<BaseItemStack> _stacks;

        private void Start()
        {
            _stacks = new List<BaseItemStack>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(BaseItemStack.c_tag) && other.TryGetComponent(out BaseItemStack itemStack))
            {
                _stacks.Add(itemStack);
            }
        }

        public Transform GetRandomStack()
        {
            Transform stackTransform = null;

            List<BaseItemStack> avaliableStacks = new List<BaseItemStack>();

            for (int i = 0; i < _stacks.Count; i++)
            {
                if (_stacks[i].isActiveAndEnabled)
                {
                    avaliableStacks.Add(_stacks[i]);
                }
            }

            if (avaliableStacks.Count > 0)
            {
                stackTransform = avaliableStacks[Random.Range(0, avaliableStacks.Count)].transform;
            }

            return stackTransform;
        }
    }
}