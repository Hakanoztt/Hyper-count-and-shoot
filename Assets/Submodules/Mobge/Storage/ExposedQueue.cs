using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public class ExposedQueue<T> {
        public T[] array;
        private int _head;
        private int _tail;
        private int _size;

        public int Head => _head;
        public int TailIndex {
            get {
                var t = _tail - 1;
                if(t < 0) {
                    return t + array.Length;
                }
                return t;

            }
        }
        public ExposedQueue() : this(4) {

        }
        public ExposedQueue(int capacity) {
            this.array = new T[capacity];
            this._head = 0;
            this._tail = 0;
            this._size = 0;
        }
        
        public int Count {
            get {
                return _size;
            }
        }
        public void Clear() {
            if (this._head < this._tail) {
                Array.Clear(this.array, this._head, this._size);
            }
            else {
                Array.Clear(this.array, this._head, this.array.Length - this._head);
                Array.Clear(this.array, 0, this._tail);
            }
            this._head = 0;
            this._tail = 0;
            this._size = 0;
        }
        public T this[int index] {
            get {
                return array[ToArrayIndex(index)];
            }
            set {
                array[ToArrayIndex(index)] = value;
            }
        }
        public void Enqueue(in T item) {
            if (this._size == this.array.Length) {
                int num = (int)((long)this.array.Length * 200L / 100L);
                if (num < this.array.Length + 4) {
                    num = this.array.Length + 4;
                }
                this.SetCapacity(num);
            }
            this.array[this._tail] = item;
            this._tail = (this._tail + 1) % this.array.Length;
            this._size++;
        }
        public T Dequeue() {
            if (this._size == 0) {
                throw new Exception("The queue is empty.");
            }
            T result = this.array[this._head];
            this.array[this._head] = default(T);
            this._head = (this._head + 1) % this.array.Length;
            this._size--;
            return result;
        }
        public ref T Peek() {
            if (this._size == 0) {
                throw new Exception("The queue is empty.");
            }
            return ref this.array[this._head];
        }
        private void SetCapacity(int capacity) {
            T[] array = new T[capacity];
            if (this._size > 0) {
                if (this._head < this._tail) {
                    Array.Copy(this.array, this._head, array, 0, this._size);
                }
                else {
                    Array.Copy(this.array, this._head, array, 0, this.array.Length - this._head);
                    Array.Copy(this.array, 0, array, this.array.Length - this._head, this._tail);
                }
            }
            this.array = array;
            this._head = 0;
            this._tail = ((this._size == capacity) ? 0 : this._size);
        }
        public void TrimExcess() {
            int num = (int)((double)this.array.Length * 0.9);
            if (this._size < num) {
                this.SetCapacity(this._size);
            }
        }
        public IndexEnumerator GetIndexEnumerator() {
            return new IndexEnumerator(this);
        }
        public ReversedIndexEnumerator GetReversedIndexEnumerator() {
            return new ReversedIndexEnumerator(this);
        }
        public int ToArrayIndex(int index) {

            var i = _head + index;
            var l = array.Length;
            if (i >= l) {
                i -= l;
            }
            return i;
        }
        public struct ReversedIndexEnumerator {
            private ExposedQueue<T> _q;
            private int _index;
            internal ReversedIndexEnumerator(ExposedQueue<T> q) {
                this._q = q;
                this._index = q._size;
            }
            public int Current {
                get {
                    return _q.ToArrayIndex(_index);
                }
            }
            public void Reset() {
                this._index = _q._size;
            }
            public bool MoveNext() {
                if (this._index == -2) {
                    return false;
                }
                this._index--;
                if (this._index == -1) {
                    this._index = -2;
                    return false;
                }
                return true;
            }
            public void Dispose() {
                this._index = -2;
                this._q = null;
            }
        }
        public struct IndexEnumerator {
            private ExposedQueue<T> _q;
            private int _index;
            internal IndexEnumerator(ExposedQueue<T> q) {
                this._q = q;
                this._index = -1;
            }
            public int Current {
                get {
                    var i = _q._head + _index;
                    var l = _q.array.Length;
                    if (i >= l) {
                        i -= l;
                    }
                    return i;
                }
            }
            public void Reset() {
                this._index = -1;
            }
            public bool MoveNext() {
                if (this._index == -2) {
                    return false;
                }
                this._index++;
                if (this._index == this._q._size) {
                    this._index = -2;
                    return false;
                }
                return true;
            }
            public void Dispose() {
                this._index = -2;
                this._q = null;
            }
        }
    }
}