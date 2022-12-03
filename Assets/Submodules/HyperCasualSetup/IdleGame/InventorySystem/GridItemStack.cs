using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame {
    public class GridItemStack : BaseItemStack {

        public Vector2Int bottomItemCount;
        public Vector3 gridDimension;
        [OwnComponent] public Transform itemParent;
        [OwnComponent] public ItemStackAnimator animator;


        public override bool AddItem(Item item, out int id) {
            if (base.AddItem(item, out id)) {

                var ittr = item.transform;

                if (itemParent == null) {
                    throw new System.Exception(this + ": item parent must be set.");
                }
                // Debug.Log(id);
                var pos = GetPosition(id);
                var euler = GetEuler(id);
                ittr.SetParent(itemParent, true);
                if (animator == null) {
                    ittr.localPosition = pos;
                }
                else {
                    ItemStackAnimator.ItemBlock block;
                    block.item = item;
                    block.stack = this;
                    block.targetLocalPosition = pos;
                    block.targetLocalEuler = euler;
                    animator.Animate(block);
                }


                return true;
            }
            return false;
        }
        private Vector3Int GetCoordinate(int index) {
            int rowCount = bottomItemCount.x * bottomItemCount.y;
            int rowIndex = index / rowCount;
            int rowOffset = index - rowIndex * rowCount;
            int depthIndex = rowOffset / bottomItemCount.x;
            int offset = rowOffset - bottomItemCount.x * depthIndex;
            return new Vector3Int(offset, rowIndex, depthIndex);
        }
        private Vector3 GetPosition(int index) {
            Vector3Int coor = GetCoordinate(index);
            Vector3 off = Vector3.Scale(coor, gridDimension);
            Vector3Int size = new Vector3Int(bottomItemCount.x-1, 0, bottomItemCount.y-1);
            size = Vector3Int.Max(size, Vector3Int.zero);
            Vector3 startCorner = Vector3.Scale(size, gridDimension) * -0.5f;
            return startCorner + off;
        }

        private Vector3 GetEuler(int index)
        {
            return Vector3.zero;
        }
    }
}
