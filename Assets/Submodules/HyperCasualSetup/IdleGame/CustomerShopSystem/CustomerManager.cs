using System.Collections;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.IdleGame.AI;
using UnityEngine;

namespace Mobge.IdleGame.CustomerShopSystem
{
    public class CustomerManager
    {
        public const string c_cacheKey = "aiChrCache";
        private PrefabCache<BaseAI> _prefabCache;

        public static CustomerManager GetCache(LevelPlayer player)
        {
            if (!player.TryGetExtra(c_cacheKey, out CustomerManager c))
            {
                c = new CustomerManager();
                c.InitCache();
                player.SetExtra(c_cacheKey, c);
            }

            return c;
        }

        private void InitCache()
        {
            _prefabCache = new PrefabCache<BaseAI>(true, true);
        }

        public BaseAI InstantiateAi(BaseAI aiPrefab, LevelPlayer player)
        {
            return _prefabCache.Pop(aiPrefab, player.transform);
        }

        public void DestroyAi(BaseAI ai)
        {
            _prefabCache.Push(ai);
        }
    }
}