using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.MergeSoldiers {
    public class ObjectStack : MonoBehaviour {

        private static List<object> s_intObjectCache = new List<object>();
        public static object ToObjectNonAlloc(int id) {
            while (s_intObjectCache.Count <= id) {
                s_intObjectCache.Add(s_intObjectCache.Count);
            }
            return s_intObjectCache[id];
        }

        public float itemSpace = 0.7f;
        public int rowCount = 10;

        public float collectAnimTime;
        public Vector3 objectRotation;

        private ExposedList<Item> _items;
        private ActionManager _actionManager;

        public int ItemCount => _items.Count;
        public int CollectedCount {
            get {
                for (int i = _items.Count; i > 0;) {
                    i--;
                    if (_items.array[i].action.IsFinished()) {
                        return i;
                    }
                }
                return 0;
            }
        }
        public ExposedList<Item> Items => _items;
        protected void Awake() {
            _items = new ExposedList<Item>();
            _actionManager = new ActionManager();
        }

        public Vector3 GetPosition(int index) {
            int column = index / rowCount;
            int row = index - rowCount * column;
            float yOffset = (column % 2 == 1) ? itemSpace * 0.5f : 0;
            float y = yOffset + row * itemSpace;
            float width = itemSpace * Mathf.Cos(30 * Mathf.Deg2Rad);
            float x = width * column;
            return new Vector3(0, y, x);
        }

        public void AddObject(Rigidbody obj) {
            int index = _items.Count;
            Item item;
            item.body = obj;
            item.startPos = obj.worldCenterOfMass;
            item.startVelocity = obj.velocity;
            item.startRotation = obj.rotation;

            item.action = _actionManager.DoTimedAction(collectAnimTime, UpdateProgress, EndAnim, ToObjectNonAlloc(index));
            _items.Add(item);


            obj.isKinematic = true;

            enabled = true;
        }

        public void Clear() {
            _items.Clear();
            _actionManager.StopAllActions();
        }

        private void EndAnim(object data, bool completed) {
            int index = (int)data;
            var dt = _items.array[index];
            dt.body.transform.SetParent(this.transform, true);
        }

        private void UpdateProgress(in ActionManager.UpdateParams @params) {
            int index = (int)@params.data;
            var data = _items.array[index];
            float prog = @params.progress;
            var virtualStart = data.startPos + data.startVelocity * prog * this.collectAnimTime;
            var pos = Vector3.LerpUnclamped(virtualStart, transform.TransformPoint(GetPosition(index)), prog);
            var rot = Quaternion.LerpUnclamped(data.startRotation,transform.rotation * Quaternion.Euler(this.objectRotation), prog);
            data.body.MoveByCenterOfMass(pos);
            data.body.MoveRotation(rot);
        }

        private void FixedUpdate() {
            _actionManager.Update(Time.fixedDeltaTime);
            if(_actionManager.TotalCount == 0) {
                enabled = false;
            }
        }

        private void OnDrawGizmosSelected() {
            //float height = itemSpace * (rowCount - 1);
            //float width = itemSpace * Mathf.Cos(30 * Mathf.Deg2Rad);
            
            Gizmos.matrix = transform.localToWorldMatrix;

            for(int i = 0; i < rowCount * 3.5f; i++) {
                var pos = GetPosition(i);
                var p1 = pos;
                var p2 = pos;
                p1.x = -itemSpace * 1.5f;
                p2.x = itemSpace * 1.5f;
                Gizmos.DrawLine(p1, p2);
            }

            //Gizmos.DrawLine(new Vector3(0, 0, 0), new Vector3(0, height, 0));
            //Gizmos.DrawLine(new Vector3(0, 0, 0), new Vector3(0, 0, width));
            //Gizmos.DrawLine(new Vector3(0, 0, width), new Vector3(0, height, width));
            //Gizmos.DrawLine(new Vector3(0, height, 0), new Vector3(0, height, width));

            Gizmos.matrix = Matrix4x4.identity;
        }
        public struct Item {
            public Rigidbody body;
            public Vector3 startPos;
            public Vector3 startVelocity;
            public Quaternion startRotation;
            public ActionManager.Action action;
        }
    }
}