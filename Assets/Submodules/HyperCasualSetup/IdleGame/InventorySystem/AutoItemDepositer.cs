using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mobge.IdleGame {
    public class AutoItemDepositer : MonoBehaviour {


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
                int itemCount = targetStack.CalculateAcceptableItemCount(stack);
                if(itemCount == 0) {
                    id = default;
                    return false;
                }
                ItemDepositer t;
                t.target = targetStack;
                t.startTime = Time.fixedTime;
                int leftSpace = targetStack.maxCount - targetStack.ItemCount;
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
                bool added = false;
                while(requiredCount > dep.sentCount) {
                    var it = FindItemToSend(dep.target);
                    if (it == null) {
                        break;
                    }
                    if (stack.RemoveItem(it)) {
                        dep.target.AddItem(it);
                        added = true;
                    }
                    dep.sentCount++;
                }
                if (added) {
                    _depositers[r.value] = dep;
                }
            }
        }

        private Item FindItemToSend(BaseItemStack target) {
            var en = stack.GetEnumerator();
            while (en.MoveNext()) {
                var c = en.Current;
                if (target.CanAdd(c.Value)) {
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