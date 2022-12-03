using System;
using UnityEngine;

namespace Mobge {
    public class ExposedList<T> {
        public T[] array;
        [SerializeField]
        private int _count;
        // todo: remove this elegantly
        public float lastSweepTime = 0;
        public ExposedList(int capacity) {
            array = new T[capacity];
            _count = 0;
        }
        public ExposedList() : this(4) {

        }
        public void SetArray(T[] array, int count) {
            this.array = array;
            _count = count;
        }
        public int Count => _count;
        public T RemoveFast(int index){
            var t = array[index];
            _count--;
            array[index] = array[_count];
            array[_count] = default(T);
            return t;
        }
        //public bool RemoveFast(T element) {
        //    var i = IndexOf(element);
        //    if (i >= 0) {
        //        RemoveFast(i);
        //        return true;
        //    }
        //    return false;
        //}
        public void RemoveRangeFast(int index, int count)
        {
            int removeEnd = index + count;
            int lastStart = _count - count;
            int moveStart;
            if(removeEnd < lastStart) {
                moveStart = lastStart;
            }
            else{
                moveStart = removeEnd;
            }
            for(int i = moveStart; i < _count; i++, index++) {
                array[index] = array[i];
            }
            var d = default(T);
            for(int i = lastStart; i < _count; i++) {
                array[i] = d;
            }
            _count = lastStart;
        }
        public void Insert(int index, T value) {
            int i = _count;
            AddFast();
            while(i > index) {
                array[i] = array[--i];
            }
            array[index] = value;
        }

        public bool Contains(T value) {
            if (value == null) {
                for (int i = 0; i < _count; i++) {
                    if (array[i] == null) {
                        return true;
                    }
                }
            }
            else {
                for (int i = 0; i < _count; i++) {
                    if (value.Equals(array[i])) { 
                        return true;
                    }
                }
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            _count--;
            for(int i = index; i < _count; i++) {
                array[i] = array[i + 1];
            }
            array[_count] = default(T);
        }
        public T RemoveLast(){
            _count--;
            var a = array[_count];
            array[_count] = default(T);
            return a;
        }
        public T RemoveLastFast(){
            _count--;
            return array[_count];
        }
        public T First => array[0];
        public T Last => array[_count - 1];
        public int Capacity{
            get => array.Length;
            private set{
                var newArray = new T[value];
                Array.Copy(this.array, newArray, _count);
                array = newArray;
            }
        }
        public void SetCountFast(int count) {
            if(count > array.Length) {
                IncreaseCount(count);
            }
            _count = count;
        }
        public void SetCount(int count) {
            if (count >= array.Length) {
                IncreaseCount(count);
            }
            var d = default(T);
            while(count < this._count) {
                this._count--;
                array[this._count] = d;
            }
            _count = count;
        }
        private void IncreaseCount(int count){
            int newSize = array.Length == 0 ? 4 : array.Length * 2;
            if(newSize < count){
                newSize = count;
            }
            Capacity = newSize;
        }
        public T[] ToArray() {
            T[] ts = new T[_count];
            Array.Copy(array, ts, _count);
            return ts;
        }
        public void Add(T t){
            if(_count == array.Length) {
                IncreaseCount(_count + 1);
            }
            array[_count] = t;
            _count++;
        }
        public int AddFast() {

            if (_count == array.Length) {
                IncreaseCount(_count + 1);
            }
            return _count++;
        }

        public void Clear()
        {
            var d = default(T);
            for(int i = 0; i < _count; i++) {
                array[i] = d;
            }
            _count = 0;
        }

        public void Reverse(){
            array.Reverse(0, _count);
        }

        public void ClearFast() {
            _count = 0;
        }
        public bool Trim() {
            if(array.Length > _count) {
                Array.Resize(ref array, _count);
                return true;
            }
            return false;
        }
        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }
        public void Swap(int i, int j) {
            var temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
        public struct Enumerator
        {
            private ExposedList<T> _list;
            private int _index;
            internal Enumerator(ExposedList<T> list) {
                _list = list;
                _index = -1;
            }
            public bool MoveNext() {
                _index++;
                return _index < _list._count;
            }
            public T Current {
                get {
                    return _list.array[_index];
                }
            }
            public int CurrentKey => _index;
        }

        public int IndexOf(T obj) {
            return System.Array.IndexOf(array, obj, 0, _count);
        }
    }
}