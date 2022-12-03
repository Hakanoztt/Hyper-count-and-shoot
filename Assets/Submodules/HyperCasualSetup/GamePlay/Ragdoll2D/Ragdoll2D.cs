using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.HyperCasualSetup.GamePlay {
    public class Ragdoll2D : MonoBehaviour {
        public Bone[] bones;
        public Bone MainBone => bones[0];
        public Vector2 Velocity {
            get {
                Vector2 speed = Vector2.zero;
                for(int i = 0; i < bones.Length; i++) {
                    speed += bones[i].body.velocity;
                }
                return speed * (1f/ (float)bones.Length);
            }
            set {
                var vel = Velocity;
                var dif = value - vel;
                for(int i = 0; i <bones.Length; i++) {
                    bones[i].body.velocity += dif;
                }
            }
        }
        private static readonly Dictionary<Rigidbody2D, Ragdoll2D> RigidbodyToRagdollCache = new Dictionary<Rigidbody2D, Ragdoll2D>();
        private ExposedList<IRagdollModule> _modules = new ExposedList<IRagdollModule>();

        private void OnEnable() {
            RigidbodyToRagdollCache.Clear();
            _modules.Clear();
        }
        public static bool TryGetRagdoll(Rigidbody2D rb, out Ragdoll2D ragdoll) {
            if (RigidbodyToRagdollCache.ContainsKey(rb)) {
                ragdoll = RigidbodyToRagdollCache[rb];
                return ragdoll != null;
            }
            ragdoll = null;
            var tr = rb.transform;
            while (tr != null) {
                ragdoll = tr.GetComponent<Ragdoll2D>();
                if (ragdoll != null) {
                    break;
                }
                tr = tr.parent;
            }
            RigidbodyToRagdollCache.Add(rb, ragdoll);
            return ragdoll != null;
        }
        public T AddModule<T>(T module) where T : MonoBehaviour, IRagdollModule {
            var mInstance = Instantiate(module);
            var instance = (IRagdollModule)mInstance;
            var itr = mInstance.transform;
            itr.SetParent(transform, false);
            itr.localPosition = Vector3.zero;
            instance.AddedToRagdoll(this);
            _modules.Add(instance);
            return (T)instance;
        }
        public void RemoveModule(IRagdollModule module) {
            var i = _modules.IndexOf(module);
            if (i >= 0) {
                _modules.RemoveFast(i);
                module.RemovedFromRagdoll(this);
            }
        }
        public T GetModule<T>() where T : class, IRagdollModule {
            var arr = _modules.array;
            var count = _modules.Count;
            for (int i = 0; i < count; i++) {
                var el = arr[i];
                if (el is T t) {
                    return t;
                }
            }
            return null;
        }
        public ExposedList<IRagdollModule>.Enumerator GetAllModules() {
            return _modules.GetEnumerator();
        }
        public void RemoveAllModules() {
            var marr = _modules.array;
            for(int i = 0; i < _modules.Count; i++) {
                var m = marr[i];
                m.RemovedFromRagdoll(this);
            }
            _modules.Clear();
        }
        [Serializable]
        public struct Bone {
            public Transform bone;
            public int parent;
            public Collider2D collider;
            public Rigidbody2D body;
            public HingeJoint2D joint;
        }
    }

    public interface IRagdollModule {
        void AddedToRagdoll(Ragdoll2D ragdoll);
        void RemovedFromRagdoll(Ragdoll2D ragdoll);
    }
}