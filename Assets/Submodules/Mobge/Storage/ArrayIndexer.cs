using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core {
    public struct ArrayIndexer<T> {
        private T[] _array;
        private bool _writable;

        public ArrayIndexer(T[] array, bool writable = true) {
            _array = array;
            _writable = writable;
        }

        public int Count => _array.Length;
        public T this[int index] {
            get => _array[index];
            set {

                if (!_writable) {
                    throw new System.Exception("Write is not enabled for this property.");
                }
                _array[index] = value;
            }
        }
    }
    public struct ListIndexer<T> {
        private List<T> _list;
        private bool _writable;

        public ListIndexer(List<T> list, bool writable = true) {
            _list = list;
            _writable = writable;
        }
        public int Count => _list.Count;
        public T this[int index] {
            get => _list[index];
            set {
                if (!_writable) {
                    throw new System.Exception("Write is not enabled for this property.");
                }
                _list[index] = value;
            }
        }
    }
}