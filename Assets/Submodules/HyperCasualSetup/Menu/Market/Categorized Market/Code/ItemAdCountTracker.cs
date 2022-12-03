using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.UI.CategorizedMarket {

    [RequireComponent(typeof(AGameContext))]
    public class ItemAdCountTracker : MonoBehaviour {

        public const string c_key = "ad_count_tracker";

        public static ItemAdCountTracker GetTracker(AGameContext context) {
            if(!context.TryGetExtra<ItemAdCountTracker>(c_key, out var t)){
                t = context.GetComponent<ItemAdCountTracker>();
                if (t == null) {
                    t = context.gameObject.AddComponent<ItemAdCountTracker>();
                }
                t._context = context;
                context.SetExtra(c_key, t);
            }
            return t;
        }

        AGameContext _context;

        private readonly Dictionary<string, ItemSet> adQuantities = new Dictionary<string, ItemSet>();

        private ItemSet Track(ItemSet set) {
            if (set.stackable) {
                throw new Exception("Stackable item sets cannot be tracked by " + GetType() + ".");
            }
            if (!adQuantities.TryGetValue(set.name, out var stackable)) {
                stackable = Instantiate(set);
                stackable.stackable = true;
                stackable.name = set.name;
                adQuantities.Add(set.name, stackable);
            }
            return stackable;
        }

        public int GetAdCount(ItemSet.ItemPath path) {
            ItemSet set = Track(path.set);
            return _context.GameProgressValue.GetQuantityItemSet(set)[path.id];

            //throw new Exception("set not tracked, call Track with set");
        }

        public void SetAdCount(ItemSet.ItemPath path, int amount = 1) {
            ItemSet set = Track(path.set);
            AGameProgress progress = _context.GameProgressValue;
            var data = progress.GetQuantityItemSet(set);
            data[path.id] += amount;
            _context.GameProgress.SaveValue(progress);
        }
    }
}