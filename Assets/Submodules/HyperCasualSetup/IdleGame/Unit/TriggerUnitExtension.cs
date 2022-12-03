using Mobge.IdleGame.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame {
    public abstract class TriggerUnitExtension : MonoBehaviour {

        protected static List<Collider> s_tempRemoveColliders = new List<Collider>();
        
        // if null or empty, catch all the entering triggers

        [InterfaceConstraint(typeof(IUnit))] public Component contextOwner;

        private HashSet<Collider> _colliders;
        protected IUnit _unit;

        public IUnit Unit => _unit;

        public int ColliderCount => _colliders.Count;

        public abstract string TriggerTag { get; }

        protected HashSet<Collider>.Enumerator GetTriggerEnumerator() {
            return _colliders.GetEnumerator();
        }

        protected void Awake() {
            _colliders = new HashSet<Collider>();
        }
        protected void Start() {
            _unit = contextOwner as IUnit;
            Initialize(_unit);
        }

        protected abstract void Initialize(IUnit unit);


        protected abstract void TriggerEntered(Collider trigger, int newCount);

        protected abstract void TriggerExited(Collider trigger, int newCount);
        protected void OnTriggerEnter(Collider other) {
            if (other.isTrigger && other.CompareTag(TriggerTag)) {

                if (_colliders.Add(other)) {
                    TriggerEntered(other, _colliders.Count);
                }
            }
        }
        protected void OnTriggerExit(Collider other) {
            if (_colliders.Remove(other)) {
                TriggerExited(other, _colliders.Count);
            }

            var en = _colliders.GetEnumerator();
            while (en.MoveNext()) {
                var c = en.Current;
                if (c == null || !c.enabled) {
                    s_tempRemoveColliders.Add(c);
                }
            }
            en.Dispose();

            for (int i = 0; i < s_tempRemoveColliders.Count; i++) {
                var c = s_tempRemoveColliders[i];
                _colliders.Remove(c);
                TriggerExited(c, _colliders.Count);
            }
            s_tempRemoveColliders.Clear();

        }

    }
}
