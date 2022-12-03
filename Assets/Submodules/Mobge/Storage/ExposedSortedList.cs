using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge
{
    public class ExposedSortedList<T>
    {
        public Pair[] array;
        private int _count;
        public ExposedSortedList() {
            array = new Pair[4];
        }
        /// <summary>
        /// Initializes the list with sorted data. If the data is not sorted it DOES NOT throw any exception but the created object works in an undefined way.
        /// </summary>
        /// <param name="sortedArray"></param>
        public ExposedSortedList(Pair[] sortedArray) {
            array = sortedArray;
            _count = sortedArray.Length;
        }
        public void Trim() {
            Array.Resize(ref array, _count);
        }
        public void Clear() {
            Array.Clear(array, 0, _count);
            _count = 0;
        }
        public void ClearFast() {
            _count = 0;
        }
        public int GetIndex(int key) {
            return BinarySearch(0, _count, key);
        }
        public T this[int key] {
            get {
                return array[BinarySearch(0, _count, key)].value;
            }
            set {
                array[BinarySearch(0, _count, key)].value = value;
            }
        }
        public bool TryGetValue(int key, out T t) {
            var index = BinarySearch(0, _count, key);
            if (index >= 0) {
                t = array[index].value;
                return true;
            }
            else {
                t = default(T);
            }
            return false;
        }
        public void SetCountFast(int count) {
            if (count >= array.Length) {
                IncreaseCount(count);
            }
            _count = count;
        }
        private int BinarySearch(int index, int length, int key) {
            int start = index;
            int end = index + length - 1;
            while(start <= end) {
                int mid = start + ((end - start) >> 1);
                if((uint)array[mid].key == (uint)key) {
                    return mid;
                }
                if((uint)array[mid].key < (uint)key) {
                    start = mid + 1;
                }
                else {
                    end = mid - 1;
                }
            }
            return ~start;
        }
        public int Count => _count;
        public void Add(int key, T value) {
            int num = BinarySearch(0, _count, key);
            if (num >= 0) {
                throw new ArgumentException("The key is already present in the list.");
            }
            this.Insert(~num, key, value);
        }

        private void Insert(int index, int key, T value) {
            if(_count == array.Length) {
                IncreaseCount(_count + 1);
            }
            Array.Copy(array, index, array, index + 1, _count - index);
            array[index] = new Pair {
                key = key,
                value = value
            };
            _count++;
        }

        private int Capacity {
            get => array.Length;
            set {
                var newArray = new Pair[value];
                Array.Copy(this.array, newArray, _count);
                array = newArray;
            }
        }
        private void IncreaseCount(int count) {
            int newSize = array.Length == 0 ? 4 : array.Length * 2;
            if (newSize < count) {
                newSize = count;
            }
            Capacity = newSize;
        }
        [Serializable]
        public struct Pair
        {
            public int key;
            public T value;
        }
    }
}