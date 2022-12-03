using System;
using System.Collections;
using UnityEngine;

namespace Mobge {

    [Serializable]
    public abstract class _Data : ScriptableObject {
#if UNITY_EDITOR
        public abstract string GetNameOf(int index);
#endif
        public abstract bool HasIndex(int index);
        public abstract object GetValueOf(int index);
        public abstract IEnumerator GetEnumerator();
    }

    [Serializable]
    public class Data<T> : _Data {
        public AutoIndexedMap<Pair<T>> map;

#if UNITY_EDITOR
        public override string GetNameOf(int index) => map[index].label;
#endif
        public override bool HasIndex(int index) {
            var enumerator = map.GetKeyEnumerator();
            while (enumerator.MoveNext()) {
                int idx = enumerator.Current;
                if (idx == index) {
                    return true;
                }
            }
            return false;
        }
        public override object GetValueOf(int index) => map[index].value;
        public override IEnumerator GetEnumerator() => map.GetKeyEnumerator();
    }
}