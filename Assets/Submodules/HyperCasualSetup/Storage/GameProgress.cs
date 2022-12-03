using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

namespace Mobge.HyperCasualSetup {

    [Serializable]
    public class GameProgress<T> : AGameProgress where T : LevelResult {

        [SerializeField] private Dictionary<ALevelSet.ID, T> _levels = new Dictionary<ALevelSet.ID, T>();
        public struct Level {
            public T result;
            public ALevelSet.ID id;
        }

        public T this[ALevelSet.ID id] {
            get {
                _levels.TryGetValue(id, out T value);
                return value;
            }
            set {
                _levels[id] = value;
            }
        }

        public override bool TryGetLevelResult(ALevelSet.ID id, out LevelResult result) {
            var b =_levels.TryGetValue(id, out T r);
            result = r;
            return b;
        }

        public override void SetLevelResult(AGameContext ctx, ALevelSet.ID id, LevelResult result) {
            _levels[id] = (T)result;
            ChangeTotalScore(ctx, result.score, "LevelEnd");
        }

        public override void ResetAllData() {
            base.ResetAllData();
            _levels.Clear();
        }
    }

    public abstract class AGameProgress {

        [SerializeField] private Dictionary<string, ItemSetData> _itemSets;
        [SerializeField] private Dictionary<string, QuantityItemSetData> _quantityItemSets;
        [SerializeField] private Dictionary<string, float> _generalUseValues;
        [SerializeField] private Dictionary<string, double> _generalUse64Bits;
        [SerializeField] private ALevelSet.ID _nextLevelToPlay;
        [SerializeField] private float _totalScore;
        [SerializeField] private Dictionary<string, byte[]> _generalUseBytes;

        public float TotalScore {
            get => _totalScore;
            private set => _totalScore = value;
        }

        public ALevelSet.ID NextLevelToPlay {
            get => _nextLevelToPlay;
            set => _nextLevelToPlay = value;
        }

        public abstract bool TryGetLevelResult(ALevelSet.ID id, out LevelResult result);
        public abstract void SetLevelResult(AGameContext ctx, ALevelSet.ID id, LevelResult result);

        public virtual void ChangeTotalScore(AGameContext ctx, float amount, string source = null) {
            TotalScore += amount;
            FireMoneyEvent(ctx, amount, source);
        }

        public void FireMoneyEvent(AGameContext ctx, float amount, string source) {
            if (!string.IsNullOrWhiteSpace(source) && ctx.MenuManager.TryGetLastOpenedLinearIndex(out int index)) {
                if (amount != 0) {
#if UNITY_EDITOR
                    Debug.Log("transaction event: " + source + ": " + amount);
#endif
                    ElephantSDK.Elephant.Transaction("money", index + 1, (long)amount, (long)TotalScore, source);
                }
            }
        }

        public bool IsUnlocked(ALevelSet data, ALevelSet.ID id, AGameContext gc) {
            if (gc.bypassLevelUnlocking) return true;
            var e = data.GetDependencies(id);
            while (e.MoveNext()) {
                if (!TryGetLevelResult(e.Current, out LevelResult r) || !r.completed) {
                    return false;
                }
            }
            return true;
        }

        private ItemSetData GetItemSet(string identifier, int defaultItem = -1) {
            if (_itemSets == null) {
                _itemSets = new Dictionary<string, ItemSetData>();
            }
            if (!_itemSets.TryGetValue(identifier, out ItemSetData data)) {
                data = new ItemSetData(defaultItem);
                _itemSets.Add(identifier, data);
            }
            return data;
        }

        public ItemSetData GetItemSet(ItemSet set) {
            if (set.stackable) {
                throw new Exception($"Item set {set} is stackable. Use {nameof(GetQuantityItemSet)} instead or change {nameof(set.stackable)} field to false.");
            }
            var its = GetItemSet(set.name, set.defaultItem);
            var at = set.items.GetPairEnumerator();
            while (at.MoveNext()) {
                var p = at.Current;
                if (p.Value.defaultLevel > 0) {
                    its.AddItem(p.Key);
                }
            }
            return its;
        }

        public QuantityItemSetData GetQuantityItemSet(ItemSet set) {
            if (!set.stackable) {
                throw new Exception($"Item set {set} is NOT stackable. Use {nameof(GetItemSet)} instead or change {nameof(set.stackable)} field to true.");
            }
            var identifier = set.name;
            var data = GetCustomItemsetData(identifier, out bool freshNew);
            if (freshNew) {
                var at = set.items.GetPairEnumerator();
                while (at.MoveNext()) {
                    var p = at.Current;
                    if (p.Value.defaultLevel > 0) {
                        data.AddItem(p.Key, p.Value.defaultLevel);
                    }
                }
            }
            return data;
        }
        public QuantityItemSetData GetCustomQuantityData(string key) {

            return GetCustomItemsetData(key, out _);
        }
        private QuantityItemSetData GetCustomItemsetData(string identifier, out bool freshNew) {
            if (_quantityItemSets == null) {
                _quantityItemSets = new Dictionary<string, QuantityItemSetData>();
            }
            freshNew = !_quantityItemSets.TryGetValue(identifier, out QuantityItemSetData data);
            if (freshNew) {
                data = new QuantityItemSetData();
                _quantityItemSets.Add(identifier, data);
            }
            return data;
        }
        public float GetFloat(string key, float defaultValue) {
            if (_generalUseValues != null && _generalUseValues.TryGetValue(key, out float value)) {
                return value;
            }
            return defaultValue;
        }

        public void SetFloat(string key, float value) {
            if (_generalUseValues == null) {
                _generalUseValues = new Dictionary<string, float>();
            }
            _generalUseValues[key] = value;
        }

        public double GetDouble(string key, double defaultValue) {
            if(_generalUse64Bits != null && _generalUse64Bits.TryGetValue(key, out double value)) {
                return value;
            }
            return defaultValue;
        }

        public void SetDouble(string key, double value) {
            if(_generalUse64Bits == null) {
                _generalUse64Bits = new Dictionary<string, double>();
            }
            _generalUse64Bits[key] = value;
        }

        public bool RemoveValue(string key) {
            if (_generalUseValues == null) {
                return false;
            }
            return _generalUseValues.Remove(key);
        }

        public bool Remove64Value(string key) {
            if (_generalUse64Bits == null) {
                return false;
            }
            return _generalUse64Bits.Remove(key);
        }

        public virtual void ResetAllData() {
            _itemSets?.Clear();
            _quantityItemSets?.Clear();
            _generalUseValues?.Clear();
            _generalUse64Bits?.Clear();
            _generalUseBytes?.Clear();
            _nextLevelToPlay = new ALevelSet.ID();
            _totalScore = 0f;
        }

        public bool Has32BitValue(string key) {
            if (_generalUseValues == null) {
                return false;
            }
            return _generalUseValues.ContainsKey(key);
        }

        public bool Has64BitValue(string key) {
            if (_generalUse64Bits == null) {
                return false;
            }
            return _generalUse64Bits.ContainsKey(key);
        }

        public unsafe int GetInt(string key, int defaultValue) {
            if (_generalUseValues != null && _generalUseValues.TryGetValue(key, out float value)) {
                return *(int*)(&value);
            }
            return defaultValue;
        }

        public unsafe void SetInt(string key, int value) {
            if (_generalUseValues == null) {
                _generalUseValues = new Dictionary<string, float>();
            }
            _generalUseValues[key] = *(float*)(&value);
        }

        public unsafe long GetLong(string key, long defaultValue) {
            if (_generalUse64Bits != null && _generalUse64Bits.TryGetValue(key, out double value)) {
                return *(long*)(&value);
            }
            return defaultValue;
        }

        public unsafe void SetLong(string key, long value) {
            if (_generalUse64Bits == null) {
                _generalUse64Bits = new Dictionary<string, double>();
            }
            _generalUse64Bits[key] = *(double*)(&value);
        }

        public bool GetBool(string key, bool defaultValue) {
            return GetFloat(key, defaultValue ? 1f : 0f) != 0;
        }

        public void SetBool(string key, bool value) {
            SetFloat(key, value ? 1f : 0f);
        }

        public string GetString(string key, string defaultValue) {
            var bytes = GetBytes(key);
            return bytes == null ? defaultValue : Encoding.Unicode.GetString(bytes);
        }

        public void SetString(string key, string value) {
            SetBytes(key, Encoding.Unicode.GetBytes(value));
        }

        public byte[] GetBytes(string key) {
            if (_generalUseBytes != null && _generalUseBytes.TryGetValue(key, out byte[] value)) {
                return value;
            }
            return null;
        }

        public void SetBytes(string key, byte[] bytes) {
            if (_generalUseBytes == null) {
                _generalUseBytes = new Dictionary<string, byte[]>();
            }
            _generalUseBytes[key] = bytes;
        }

        public bool HasBytes(string key) {
            return _generalUseBytes != null && _generalUseBytes.ContainsKey(key);
        }

        public bool RemoveBytes(string key) {
            return _generalUseBytes != null && _generalUseBytes.Remove(key);
        }

        [Serializable]
        public struct Pair {
            public int key, quantity;

            public Pair(int key, int quantity = 1) {
                this.key = key;
                this.quantity = quantity;
            }
        }

        [Serializable]
        public class QuantityItemSetData {

            [SerializeField] private List<Pair> _itemQuantities = new List<Pair>();

            public QuantityItemSetData() {}

            public int AddItem(int itemId, int quantity = 1) {
                Assert.IsTrue(quantity > 0, "Quantity must be bigger than 0.");
                int index = IndexOf(itemId, out int current);
                if (index < 0) {
                    _itemQuantities.Add(new Pair(itemId, quantity));
                    return quantity;
                } else {
                    int total = current + quantity;
                    _itemQuantities[index] = new Pair(itemId, total);
                    return total;
                }
            }
            public void Clear() {
                _itemQuantities.Clear();
            }

            public bool HasOrHad(int itemId) {
                int index = IndexOf(itemId, out _);
                return index >= 0;
            }

            public bool TryConsume(int itemId, int quantity) {
                return TryConsume(itemId, quantity, out _);
            }

            public bool TryConsume(int itemId, int quantity, out int left) {
                Assert.IsTrue(quantity > 0, "Quantity must be bigger than 0.");
                int index = IndexOf(itemId, out left);
                if (left < quantity) {
                    return false;
                } else {
                    left -= quantity;
                    _itemQuantities[index] = new Pair(itemId, left);
                    return true;
                }
            }

            public int this[int itemId] {
                get {
                    IndexOf(itemId, out int quantity);
                    return quantity;
                }
                set {
                    int index = IndexOf(itemId, out _);
                    if (index < 0) {
                        _itemQuantities.Add(new Pair(itemId, value));
                    } else {
                        _itemQuantities[index] = new Pair(itemId, value);
                    }
                }
            }

            private int IndexOf(int itemId, out int quantity) {
                for (int i = 0; i < _itemQuantities.Count; i++) {
                    var p = _itemQuantities[i];
                    if (p.key == itemId) {
                        quantity = p.quantity;
                        return i;
                    }
                }
                quantity = 0;
                return -1;
            }

            public QuantityEnumerator GetQuantityEnumerator() {
                return new QuantityEnumerator(this);
            }


            public struct QuantityEnumerator: IDisposable {

                private QuantityItemSetData _data;
                private int _index;

                internal QuantityEnumerator(QuantityItemSetData data) {
                    _data = data;
                    _index = -1;
                }

                public bool MoveNext() {
                    for (;;) { // inf loop
                        _index++;
                        if (_index >= _data._itemQuantities.Count) {
                            return false;
                        }
                        int q = _data._itemQuantities[_index].quantity;
                        if (q > 0) {
                            return true;
                        }
                    }
                }

                public Pair Current => _data._itemQuantities[_index];

                public void Reset() {
                    _index = -1;
                }

                public void Dispose() {}
            }
        }

        [Serializable]
        public class ItemSetData {

            [SerializeField] private List<int> _ownedItems = new List<int>();
            [SerializeField] private int _equippedItem;

            public ItemSetData() {
                _equippedItem = -1;
            }

            internal ItemSetData(int defaultItem) {
                if (defaultItem >= 0) {
                    _ownedItems.Add(defaultItem);
                }
                _equippedItem = defaultItem;
            }

            public bool AddItem(int itemId) {
                int index = _ownedItems.IndexOf(itemId);
                if (index >= 0) {
                    return false;
                }
                _ownedItems.Add(itemId);
                return true;
            }

            public bool RemoveItem(int item) {
                return _ownedItems.Remove(item);
            }

            public bool HasItem(int item) {
                return _ownedItems.Contains(item);
            }

            public int EquippedItem {
                get {
                    return _equippedItem;
                }
                set {
                    _equippedItem = value;
                }
            }
        }
    }
}