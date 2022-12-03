using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame {
    public class ItemStackAnimator : MonoBehaviour {

        private ExposedList<AnimParameters> _anims;

        public float animationTime = 0.3f;

        public bool canRotate = false;


        protected void Awake() {
            _anims = new ExposedList<AnimParameters>();
        }


        protected void Update() {
            float time = Time.time;
            int i = 0;
            while (i < _anims.Count) {
                ref var a = ref _anims.array[i];

                bool finish;
                if (a.block.item.Container != (object)a.block.stack) {
                    finish = true;
                }
                else {
                    float passed = time - a.time;
                    Vector3 targetPos;
                    Vector3 targetEuler = Vector3.zero;
                    if (passed >= animationTime) {
                        targetPos = a.block.targetLocalPosition;
                        targetEuler = a.block.targetLocalEuler;
                        finish = true;
                    }
                    else {
                        targetPos = Vector3.LerpUnclamped(a.startPos, a.block.targetLocalPosition, passed / animationTime);
                        if(canRotate)
                        {
                            targetEuler = Vector3.LerpUnclamped(a.startEuler, a.block.targetLocalPosition, passed / animationTime);
                        }
                        finish = false;
                    }

                    a.block.item.transform.localPosition = targetPos;
                    a.block.item.transform.localEulerAngles = targetEuler;
                }

                if (finish) {
                    _anims.RemoveFast(i);
                }
                else {
                    i++;
                }
            }
        }


        public void Animate(in ItemBlock block) {
            AnimParameters prm;
            prm.time = Time.time;
            prm.block = block;
            prm.startPos = block.item.transform.localPosition;
            prm.startEuler = block.item.transform.localEulerAngles;
            _anims.Add(prm);
        }

        public struct ItemBlock {
            public Item item;
            public BaseItemStack stack;
            public Vector3 targetLocalPosition;
            public Vector3 targetLocalEuler;
        }

        private struct AnimParameters {
            public float time;
            public Vector3 startPos;
            public Vector3 startEuler;
            public ItemBlock block;
        }

    }
}