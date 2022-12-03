using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    [DisallowMultipleComponent]
    public class ReusableGroup : AReusableItem {
        public override bool IsActive {
            get {
                for(int i = 0; i < items.Length; i++) {
                    if (items[i].IsActive) return true;
                }
                return false;
            }
        }
        public AReusableItem[] items;

        public override void Stop() {
            for(int i = 0;  i < items.Length; i++) {
                items[i].Stop();
            }
        }

        public override void StopImmediately() {
            for (int i = 0; i < items.Length; i++) {
                items[i].StopImmediately();
            }
        }

        protected override void OnPlay() {
            for (int i = 0; i < items.Length; i++) {
                items[i].Play();
            }
        }
    }
}