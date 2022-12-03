using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mobge.IdleGame {
    public class AutoItemCollector : MonoBehaviour {


        public BaseItemStack stack;
        public float maxDepositTime = 0.5f;
        public float depositFrequency = 6f;
        [OwnComponent] public Collider trigger;

        private TriggerTracker<int> _tracker;
        private AutoIndexedMap<ItemDepositer> _depositers;
        

        protected void Awake() {
            _depositers = new AutoIndexedMap<ItemDepositer>();
            _tracker = Tracker.Add<Tracker>(trigger.gameObject, BaseItemStack.c_tag, HandleTriggerEnter);
            _tracker.onTriggerExit = HandleTriggerExit;
        }


        private bool HandleTriggerEnter(Collider c, out int id) {
            if (c.TryGetComponent<BaseItemStack>(out var targetStack)) {
                int itemCount = stack.CalculateAcceptableItemCount(targetStack);
                if(itemCount == 0) {
                    id = default;
                    return false;
                }
                ItemDepositer t;
                t.target = targetStack;
                t.startTime = Time.fixedTime;
                int leftSpace = stack.maxCount - stack.ItemCount;
                itemCount = Mathf.Min(itemCount, leftSpace);
                t.rate = itemCount / maxDepositTime;
                t.rate = Mathf.Max(t.rate, this.depositFrequency);
                t.sentCount = 0;
                id = _depositers.AddElement(t);
                return true;
            }
            id = default;
            return false;
        }

        private void HandleTriggerExit(int id) {
            _depositers.RemoveElement(id);
        }

        private void FixedUpdate() {
            var tagets = _tracker.GetEnumerator();
            float time = Time.fixedTime;
            while (tagets.MoveNext()) {
                var r = tagets.Current;
                var dep = _depositers[r.value];
                float passedTime = time - dep.startTime;
                int requiredCount = Mathf.FloorToInt(passedTime * dep.rate) + 1;
                bool removed = false;
                while(requiredCount > dep.sentCount) {
                    var it = FindItemToGet(dep.target);
                    if (it == null) {
                        break;
                    }
                    if (dep.target.RemoveItem(it)) {
                        stack.AddItem(it);
                        removed = true;
                    }
                    dep.sentCount++;
                }
                if (removed) {
                    _depositers.RemoveByValue(dep);
                }
            }
        }

        private Item FindItemToGet(BaseItemStack target) {
            var en = target.GetEnumerator();
            while (en.MoveNext()) {
                var c = en.Current;
                if (stack.CanAdd(c.Value)) {
                    en.Dispose();
                    return c.Value;
                }
            }
            en.Dispose();
            return null;
        }

        private struct ItemDepositer {
            public BaseItemStack target;
            public float startTime;
            public float rate;
            public int sentCount;
        }

        class Tracker : TriggerTracker<int> { }
    }
}