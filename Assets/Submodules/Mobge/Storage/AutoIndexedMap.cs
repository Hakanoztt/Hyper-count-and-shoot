using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    /// <summary>
    /// A map that gives index to elements automatically.
    /// Can be serialized by unity if extended with a constant type as T. </summary>
    [Serializable]
    public class AutoIndexedMap<T> {
        [SerializeField]
        //[HideInInspector]
        protected List<T> _elements = new List<T>();
        [SerializeField]
        // [HideInInspector]
        protected List<bool> _existences = new List<bool>();

        [SerializeField]
        protected int _nextIndex = 0;
        [SerializeField]
        protected int _count = 0;
        public int AddElement(T element) {
            int id = _nextIndex;
            if(_nextIndex == _elements.Count) {
                _elements.Add(element);
                _existences.Add(true);
                _nextIndex++;
            }
            else{
                _elements[_nextIndex] = element;
                _existences[_nextIndex] = true;
                do{
                    _nextIndex++;
                }
                while(_nextIndex<_elements.Count && _existences[_nextIndex]);
            }
            _count++;
            return id;
        }
        public void Trim() {
            int i = _existences.Count - 1;
            for (; i>=0; i--) {
                if (_existences[i]) {
                    break;
                }
            }
            int index= i+1;
            int countToRemove = _existences.Count - index;

            _existences.RemoveRange(index, countToRemove);
            _elements.RemoveRange(index, countToRemove);

        }
        public T this[int key] {
            get {
                if (!_existences[key]) {
                    throw new InvalidOperationException("There is no such key in map.");
                }
                return _elements[key];
            }
            set {
                var p = _existences[key];
                if (p) {
                    _elements[key] = value;
                }
                else {
                    throw new InvalidOperationException("Set only works if there is already an element at given index");
                }
            }
        }
        private bool InRange(int id){
            return id >= 0 && id < _elements.Count;
        }
        public bool ContainsValue(T element){
            return KeyOf(element) >= 0;
        }
        public IEnumerator GetEnumerator() {
            return GenericEnumerator();
        }
        public ValueEnumerator GenericEnumerator() {
            return new ValueEnumerator(this);
        }
        public Enumerator GetKeyEnumerator() {
            return new Enumerator(this);
        }
        public PairEnumerator GetPairEnumerator() {
            return new PairEnumerator(this);
        }

        public int IndexToKey(int index){
            for(int i = 0; i< _existences.Count; i++){
                if(_existences[i]){
                    if(index == 0){
                        return i;
                    }
                    index--;
                }
            }
            return -1;
        }
        public int KeyOf(T element){
            for(int i = 0; i < _existences.Count; i++) {
                var p = _existences[i];
                if(p && _elements[i].Equals(element)) {
                    return i;
                }
            }
            return -1;
        }
        public void Clear(){
            _elements.Clear();
            _existences.Clear();
            _count = 0;
            _nextIndex = 0;
        }
        public bool RemoveByValue(T value) {
            var key = KeyOf(value);
            if(key < 0) return false;
            RemoveUnsafe(key);
            return true;
        }
        public bool RemoveElement(int key) {
            if(!InRange(key)) return false;
            var p = _existences[key];
            if(!p) return false;
            RemoveUnsafe(key);
            return true;
        }
        private void RemoveUnsafe(int key) {
            _elements[key] = default(T);
            _existences[key] = false;
            _count--;
            if(key < _nextIndex){
                _nextIndex = key;
            }
        }
        public int Capacity {
            get {
                return _elements.Count;
            }
        }
        public int Count{get{return _count;} }
        public bool ContainsKey(int key) {
            if(!InRange(key)) return false;
            return _existences[key];
        }
        public bool TryGetElement(int key, out T element) {

            if(!InRange(key)){
                element = default(T);
                return false;
            }
            var p = _existences[key];
            if(!p) {
                element = default(T);
                return false;
            }
            element = _elements[key];
            return true;
        }
        private bool MoveEnumerator(ref int key) {
            int c = this._elements.Count;
            do {
                key++;
                if(key >= c) {
                    return false;
                }
            } while(!this._existences[key]);
            return true;
        }
        public struct PairEnumerator : IEnumerator<KeyValuePair<int, T>>
        {
            private AutoIndexedMap<T> _map;
            private int _index;
            internal PairEnumerator(AutoIndexedMap<T> map) {
                _map = map;
                _index = -1;
            }
            public KeyValuePair<int, T> Current => new KeyValuePair<int, T>(_index, _map._elements[_index]);

            object IEnumerator.Current => new KeyValuePair<int, T>(_index, _map._elements[_index]);

            public void Dispose()
            {
                
            }

            public bool MoveNext()
            {
                return _map.MoveEnumerator(ref _index);
            }

            public void Reset()
            {
                _index = -1;
            }
        }
        public struct Enumerator : IEnumerator<int>
        {
            
            internal Enumerator(AutoIndexedMap<T> map) {
                _map = map;
                _index = -1;
            }
            private AutoIndexedMap<T> _map;
            private int _index;
            public int Current => _index;

            object IEnumerator.Current => _index;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                return _map.MoveEnumerator(ref _index);
            }

            public void Reset()
            {
                _index = -1;
            }
        }


        public struct ValueEnumerator : IEnumerator<T>
        {
            private AutoIndexedMap<T> _map;
            private int _index;
            internal ValueEnumerator(AutoIndexedMap<T> map) {
                _map = map;
                _index = -1;
            }
            public T Current => _map[_index];

            object IEnumerator.Current => _map[_index];

            public void Dispose()
            {
                
            }

            public bool MoveNext()
            {
                return _map.MoveEnumerator(ref _index);
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }
}