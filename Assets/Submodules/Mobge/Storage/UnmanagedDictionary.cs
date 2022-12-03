using System;
using System.Collections;
using System.Collections.Generic;

namespace Mobge {

    /// <summary>
    /// C# port of std::unordered_map from C++ standard library,
    /// insertion O(1) amortized,
    /// deletion O(1) amortized,
    /// lookup O(1) amortized.
    ///
    /// EDIT:
    /// Originally, this data structure used System.Collections.LinkedList, but because of Mobge's policy
    /// to have no dynamic allocation when possible, the internals have changed from the aforementioned LinkedList
    /// into a namely index array(buckets) and the elements themselves. The index array is an array of indices
    /// to the first elements of chains of hash sharing elements. I know that sentence was a bit hard to understand.
    ///
    /// An element is a simple POD with data NEXT-KEY-VALUE. KEY and VALUE are self explanatory. NEXT is an index to
    /// elements array. Contents of buckets are also indices to elements array.
    ///
    /// Contents of buckets can be demonstrated like so:
    /// buckets ->  [0][3][5]
    /// elements -> [buckets[0]]-[?]-[?]-[buckets[1]]-[?]-[buckets[2]]-[?]-[?]-[?]
    ///
    /// Remember, each element has a NEXT value. This NEXT value is an index to elements array. Like so:
    ///
    /// elements -> [elem 0 NEXT=-1]-[elem 1 NEXT = 3]-[?]-[elem 3 (next of elem 1)]-[?]-[?]-[?]-[?]-[?]
    ///
    /// Just like that, these NEXT values form the basis of "separate chaining".
    ///
    /// All in all, this strategy ensures minimal allocations, and since every node is not new'd like in the LinkedList case,
    /// locality is respected. This approach also retains all of the characteristic big-O of run of the mill hash tables.
    /// </summary>
    /// <typeparam name="K">Key type</typeparam>
    /// <typeparam name="V">Value type</typeparam>
    public class UnmanagedDictionary<K, V> : IEnumerable where K : unmanaged {

        public int Count => elements.Length - freeCount;

        private struct Element {
            /* notice, next is the first element because of struct alignment. */
            public int next; // -1 if last of its chain
            public K key;
            public V value;

            public void Reset(in K key, in V value){
                this.key = key;
                this.value = value;
                next = -1;
            }
        }

        private int[] buckets;
        private Element[] elements;

        private bool[] freeList;
        private int freeCount;

        private float maxLoadFactor;

        private Comparer comparer;
        private Hasher hasher;

        /*
        * Setting default bucket count as 1 may seem weird and inefficient and I agree, but let's pretend the C++ committee knows what they were doing.
        * https://www.google.com/search?q=c%2B%2B+unorderd_map+default+bucket_size&rlz=1C1OKWM_trTR1003TR1003&oq=c%2B%2B+unorderd_map+default+bucket_size&aqs=chrome..69i57.11811j0j4&sourceid=chrome&ie=UTF-8
        */
        public UnmanagedDictionary() : this(1) {}
        public UnmanagedDictionary(int bucketCount) : this(bucketCount, 1) {}
        public UnmanagedDictionary(int bucketCount, float maxLoadFactor) {
            if (bucketCount <= 0) throw new ArgumentOutOfRangeException("Capacity must be positive");

            buckets = new int[bucketCount];
            Array.Clear(buckets, -1, buckets.Length);
            elements = new Element[bucketCount];
            for (int i = 0; i < elements.Length; i++) {
                elements[i] = default(Element);
                elements[i].next = -1;
            }
            freeList = new bool[bucketCount];
            for (int i = 0; i < freeList.Length; i++) {
                freeList[i] = true;
            }
            freeCount = freeList.Length;
            this.maxLoadFactor = maxLoadFactor;

            comparer = new Comparer(this, new UnmanagedStructComparer<K>());
            hasher = new Hasher(this, new UnmanagedStructComparer<K>());
        }

        public void Add(in K key, in V value) {
            int index = FindElementIndex(key);
            /* if element exists */
            if (index != -1) {
                /* update element value*/
                ref Element elem = ref elements[index];
                elem.value = value;
                return;
            }
            /* else, we must insert a new element! */
            int newIndex = AllocFreeIndex();
            ref Element newInsertion = ref elements[newIndex];
            newInsertion.Reset(key, value);

            /* element inserted! now put it in a bucket! */
            int hash = hasher.Hash(key);
            /* if bucket == -1 */
            if (buckets[hash] == -1) {
                /* no chain, just put and continue with your life */
                buckets[hash] = newIndex;
                return;
            }
            /* else, there is a chain. traverse the chain and append to the end! */
            ref Element element = ref elements[buckets[hash]];
            while (element.next != -1) {
                element = ref elements[element.next];
            }
            element.next = newIndex;

            RehashIfNeeded();
        }

        public void Clear() {
            Array.Clear(buckets, -1, buckets.Length);
            for (int i = 0; i < freeList.Length; i++) {
                freeList[i] = true;
            }
            freeCount = freeList.Length;
        }

        public bool Contains(in K key) => FindElementIndex(key) != -1;

        public void Remove(in K key) {
            int index = FindElementIndex(key);
            if (index == -1) { return; }

            ref Element elem = ref elements[index];
            int prevElementIndex = FindPrevElementIndex(elem);
            ref Element prevElement = ref elements[prevElementIndex];
            prevElement.next = elem.next;

            elem = default(Element);
            elem.next = -1;
            freeList[index] = true;
            freeCount++;
        }

        private int FindElementIndex(in K key) {
            int hash = hasher.Hash(key);
            int index = buckets[hash];

            while (index != -1) {
                ref Element elem = ref elements[index];

                bool equal = comparer.Compare(elem.key, key);
                if (equal) { return index; }

                index = elem.next;
            }
            return -1;
        }

        private int FindPrevElementIndex(in Element elem) {
            int hash = hasher.Hash(elem.key);
            int index = buckets[hash];

            ref Element current = ref elements[index];
            while (current.next != -1) {
                current = ref elements[index];
                ref Element next = ref elements[current.next];
                if (comparer.Compare(next.key, elem.key)) {
                    return index;
                }
                index = current.next;
            }
            return -1;
        }

        private int AllocFreeIndex() {
            for (int i = 0; i < freeList.Length; i++) {
                bool candidate = freeList[i];
                if (candidate == false) {
                    freeList[i] = true;
                    freeCount--;
                    return i;
                }
            }
            /* no free index? grow! */
            Element[] new_elements = new Element[elements.Length * 2];
            bool[] new_freeList = new bool[elements.Length * 2];
            for (int i = 0; i < elements.Length; i++) {
                new_elements[i] = elements[i];
                new_freeList[i] = false;
                new_freeList[elements.Length + i] = true;
            }
            elements = new_elements;
            freeList = new_freeList;
            freeCount = elements.Length / 2;
            return AllocFreeIndex();
        }

        private void RehashIfNeeded() {
            if (Count / buckets.Length <= maxLoadFactor) { return; }
            buckets = new int[buckets.Length * 2];
            for (int i = 0; i < elements.Length; i++) {
                ref Element element = ref elements[i];
                int index = hasher.Hash(element.key);
                if (buckets[index] == -1) {
                    buckets[index] = i;
                } else {
                    ref Element chain = ref elements[buckets[index]];
                    while (chain.next != -1) {
                        chain = ref elements[chain.next];
                    }
                    chain.next = i;
                }
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator {

            public KeyValuePair Current { get; private set; }
            object IEnumerator.Current => Current;

            private UnmanagedDictionary<K, V> dict;
            private int index;

            public Enumerator(UnmanagedDictionary<K, V> dict) {
                this.dict = dict;
                index = -1;
                Current = default;
            }

            public bool MoveNext() {
                index++;
                if (index >= dict.elements.Length) { return false; }
                if (!dict.freeList[index]) { MoveNext(); }
                Current = new KeyValuePair(dict, index);
                return true;
            }

            public void Reset() => index = -1;
        }

        public readonly struct KeyValuePair {

            public K Key => dict.elements[index].key;
            public ref V Value => ref dict.elements[index].value;

            private readonly UnmanagedDictionary<K, V> dict;
            private readonly int index;

            public KeyValuePair(UnmanagedDictionary<K, V> dict, int index) {
                this.dict = dict;
                this.index = index;
            }
        }

        private struct Comparer {

            private UnmanagedDictionary<K, V> dd;
            private readonly UnmanagedStructComparer<K> usc;

            public Comparer(UnmanagedDictionary<K, V> dd, UnmanagedStructComparer<K> usc) {
                this.dd = dd;
                this.usc = usc;
            }

            public bool Compare(in K value1, in K value2) {
                return usc.Equals(value1, value2);
            }
        }
        private struct Hasher {

            private readonly UnmanagedDictionary<K, V> dd;
            private readonly UnmanagedStructComparer<K> usc;

            public Hasher(UnmanagedDictionary<K, V> dd, UnmanagedStructComparer<K> usc) {
                this.dd = dd;
                this.usc = usc;
            }

            public int Hash(in K value) {
                return usc.GetHashCode(value) % dd.buckets.Length;
            }
        }
    }
}