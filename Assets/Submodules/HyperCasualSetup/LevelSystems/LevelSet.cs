using System;
using System.Collections;
using System.Collections.Generic;
using Mobge.Core;
using Mobge.Serialization;
using UnityEngine;

namespace Mobge.HyperCasualSetup
{
    [CreateAssetMenu(menuName = "Hyper Casual/LevelSet")]
    public class LevelSet : ALevelSet, ISerializationCallbackReceiver {
        public RandomConfiguration randomConfiguration = new RandomConfiguration
        {
            seed = 912639833
        };
        [SerializeField, HideInInspector] private BinaryObjectData _data;
        private Map _levels;

        public Map Levels {
            get {
                if (_levels == null) {
                    _levels = new Map();
                }
                return _levels;
            }
        }
        protected void OnEnable() {
            randomConfiguration.Initialize();
        }
        public override bool TryGetLinearIndex(ID id, out int index) {
            if (_levels.TryGetValue(id.Value, out AddressableLevel aLevel) && aLevel.LinearId >= 0) {
                if (randomConfiguration.enabled && id.World > randomConfiguration.startWorld) {
                    index = -1;
                    return false;
                }
                index = aLevel.LinearId;
                return true;
            }
            else {
                if (randomConfiguration.enabled) { // checked
                    if (IsRandomID(id, out var llibr)) {
                        int subLevel = id[2];
                        var lastLevelBeforeRandom = _levels.array[llibr];
                        int level = id[1] - new ID(lastLevelBeforeRandom.key).Level-1;
                        int count = lastLevelBeforeRandom.value.LinearId + 1;
                        index = count + level * randomConfiguration.subLevelPerLevel + subLevel;
                        return true;
                    }
                }
                index = -1;
                return false;
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {

            if (_data.data != null && _data.data.Length > 0) {
                var ps = Serialization.BinaryDeserializer.Instance.Deserialize<Map.Pair[]>(_data);
                _levels = new Map(ps);
            }
            else {
                _levels = new Map();
            }
            int nextIndex = 0;
            for (int i = 0; i < _levels.Count; i++) {
                var val = _levels.array[i].value;
                if (val.RuntimeKeyIsValid()) {
                    val.LinearId = nextIndex;
                    nextIndex++;
                }
            }
        }
        public ID ConvertToBaseId(ID id) {
            if (IsRandomID(id)) {
                return randomConfiguration.GetOriginalID(this, id);
            }
            return id;
        }
        public override AddressableLevel this[ID id] {
            get {
                if (randomConfiguration.enabled) { // checked
                    if (IsRandomID(id)) {
                        return randomConfiguration.GetLevel(this, id);
                    }
                }

                return _levels[id.Value];
            }
        }
        /// <summary>
        /// Returns nearest playable level with id equals or larger than the specified id. Throws Index out of bound exception if there is no such level.
        /// </summary>
        /// <param name="id">Specified id</param>
        /// <returns></returns>
        public override ID ToNearestLevelId(ID id) {
            int index = _levels.GetIndex(id.Value);
            if (index < 0) {
                index = ~index;
            }
            if (randomConfiguration.enabled) { // checked
                if (IsRandomID(id)) {
                    return id;
                }
            }
            Map.Pair p;
            while (!(p = _levels.array[index]).value.RuntimeKeyIsValid()) {
                index++;
            }
            return new ID(p.key);
        }
        public override bool TryIncreaseLevel(ref ID id) {
            if (randomConfiguration.enabled) { // checked
                if (IsRandomID(id)) {
                    id[2]++;
                    if (id[2] == randomConfiguration.subLevelPerLevel) {
                        id[1]++;
                        id[2] = 0;
                    }
                    return true;
                }
                if(id.World > randomConfiguration.startWorld) {
                    return false;
                }
            }
            int index = _levels.GetIndex(id.Value);
            if (index < 0) {
                return false;
            }
            Map.Pair p;
            var firstRandomId = GetFirstRandomId(out int lastLevelBeforeRandom);
            do {
                index++;
                if (randomConfiguration.enabled) { // checked
                    if (lastLevelBeforeRandom + 1 == index) {
                        id = firstRandomId;
                        return true;
                    }
                }
                if (index >= _levels.Count) {
                    return false;
                }
            }
            while (!(p = _levels.array[index]).value.RuntimeKeyIsValid());
            id.Value = p.key;
            return true;
        }

        public override bool TryDecreaseLevel(ref ID id) {
            int index = _levels.GetIndex(id.Value);
            if (index == 0) return false;
            if (index < 0) {
                if (randomConfiguration.enabled) { // checked
                    
                    if (IsRandomID(id)) 
                    {
                        if (id.Value == GetFirstRandomId(out int ll).Value) {
                            index = ll;
                            id = new ID(_levels.array[index].key);
                            
                        }
                        else {
                            id[2]--;
                            if (id[2] < 0) {
                                id[1]--;
                                id[2] = randomConfiguration.subLevelPerLevel - 1;
                            }
                        }
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            Map.Pair p;
            do {
                if (index == 0) return false;
                index--;
            }
            while (!(p = _levels.array[index]).value.RuntimeKeyIsValid());
            id.Value = p.key;
            return true;
        }

        public override IEnumerator<ID> GetDependencies(ID target) {
            int index = _levels.GetIndex(target.Value);
            if (index < 0) {
                if (randomConfiguration.enabled) { // checked
                    if (IsRandomID(target)) {
                        var firstRandomID = FirstRandomID;
                        if (target.Value == firstRandomID.Value) {
                            yield return new ID(_levels.array[_levels.Count - 1].key);
                        }
                        else {
                            var level = target[1];
                            var subLevel = target[2];
                            if (subLevel == 0) {
                                target[1] = level - 1;
                                target[2] = randomConfiguration.subLevelPerLevel - 1;
                            }
                            else {
                                target[2] = subLevel - 1;
                            }
                            yield return target;
                        }
                    }
                }
            }
            else {
                while (index > 0) {
                    index--;
                    var l = _levels.array[index];
                    if (l.value.RuntimeKeyIsValid()) {
                        yield return new ID(l.key);
                        break;
                    }
                }
            }
        }

        public class Map : ExposedSortedList<AddressableLevel>
        {
            public Map(Pair[] sortedArray) : base(sortedArray) {
            }
            public Map() {

            }
        }
        public LevelEnumerator GetAllLevels() {
            return new LevelEnumerator(_levels.array, 0, _levels.Count);
        }
        public int LevelCount {
            get {
                for(int i = _levels.Count-1; i >=0; i--) {
                    var a = _levels.array[i].value;
                    if (a.RuntimeKeyIsValid()) {
                        return a.LinearId + 1;
                    }
                }
                return -1;
            }
        }
        public override int GetLevelCount(int world) {
            ID id = ID.New(world + 1, -1);
            int index = _levels.GetIndex(id.Value);
            if (index < 0) {
                index = ~index;
            }
            if (index == 0) {
                return 0;
            }
            return new ID(_levels.array[index - 1].key)[1] + 1;

        }
        private bool IsRandomID(ID id) {
            return IsRandomID(id, out _);
        }
        private bool IsRandomID(ID id, out int lastLevelIdBeforeRandom) {
            lastLevelIdBeforeRandom = LastLevelIndexBeforeRandom;
            var key = new ID(_levels.array[lastLevelIdBeforeRandom].key);
            return id.World == key.World && id.Value > key.Value;
        }
        public ID FirstRandomID {
            get {
                return GetFirstRandomId(out _);
            }
        }
        public ID GetFirstRandomId(out int lastLevelIdBeforeRandom) {

            lastLevelIdBeforeRandom = LastLevelIndexBeforeRandom;
            ID lastId = new ID(_levels.array[lastLevelIdBeforeRandom].key);
            lastId[1]++;
            lastId[2] = 0;
            return lastId;
        }
        private int LastLevelIndexBeforeRandom {
            get {
                var randomStartWorld = ID.FromWorldLevel(randomConfiguration.startWorld + 1, -1);
                int index = _levels.GetIndex(randomStartWorld.Value);
                if (index < 0) {
                    index = ~index;
                }
                index -= 1;
                if (index < 0) {
                    throw new Exception("There is no suitable level as the last level before random levels.");
                }
                return index;
            }
        }
        public int LevelCountBeforeRandom {
            get {
                int index = LastLevelIndexBeforeRandom;
                return _levels.array[index].value.LinearId + 1;
            }
        }
        public override int WorldCount {
            get {
                return new ID(_levels.array[_levels.Count - 1].key)[0] + 1;
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            if (_levels != null) {
                _levels.Trim();
                var ps = _levels.array;
                _data = Serialization.BinarySerializer.Instance.Serialize(typeof(ExposedSortedList<AddressableLevel>.Pair[]), ps);
            }
        }
        public struct LevelEnumerator : IEnumerator<Map.Pair>
        {
            private Map.Pair[] _array;
            private int _start;
            private int _current;
            private int _endValue;
            internal LevelEnumerator(Map.Pair[] array, int start, int end) {
                _array = array;
                _start = start - 1;
                _current = _start;
                _endValue = end;
            }
            public Map.Pair Current => _array[_current];

            object IEnumerator.Current => _array[_current];

            public void Dispose() {

            }

            public bool MoveNext() {
                do {
                    _current++;
                    if (_current >= _endValue) {
                        return false;
                    }
                }
                while (!_array[_current].value.RuntimeKeyIsValid());
                return true;
            }

            public void Reset() {
                _current = _start;
            }
        }
        [Serializable]
        public struct RandomConfiguration
        {
            private const long m = 1 << 31;
            private const long a = 1103515245;
            private const long c = 12345;
            public bool enabled;
            public bool seedBasedShufflerEnabled;
            public bool groupBasedRandomnessEnabled;
            public int subLevelPerLevel;
            public int levelGroupCount;
            public int seed;
            public uint[] seeds;
            public int ignoreUntilWorld;
            public int ignoreUntilLevel;
            public int ignoreUntilSubLevel;
            public int startWorld;
            [NonSerialized] private List<LevelReference> _levels;
            //private RandomModule __randomModule;
            public void Initialize(bool deep = false)
            {
                if (deep) {
                    if (_levels != null) {
                        _levels.Clear();
                    }
                }
                //_randomModule = new RandomModule();
                if (subLevelPerLevel < 1) subLevelPerLevel = 1;
                if (levelGroupCount < 1) levelGroupCount = 1;

                seeds = new uint[10000];
                System.Random rand = new System.Random(seed);

                for(int i = 0; i < seeds.Length; i++) {
                    seeds[i] = (uint)rand.Next();
                }
            }
            uint Hash(int input) {
                uint state = (uint)input + (seedBasedShufflerEnabled ? (uint)seeds[input % seeds.Length] : 0);
                state ^= 2747636419;
                state ^= 2554435769;
                state ^= state >> 16;
                state ^= 2554435769;
                state ^= state >> 16;
                state ^= 2554435769;
                return state;
            }
            public AddressableLevel GetLevel(LevelSet set, ID id) {
                EnsureLevels(set);
                int index = GetIndex(set, id);
                return _levels[index].level;
            }
            public ID GetOriginalID(LevelSet set, ID id) {
                EnsureLevels(set);
                int index = GetIndex(set, id);
                return _levels[index].id;
            }
            private int GetIndex(LevelSet set, ID id) {
                if (groupBasedRandomnessEnabled)
                    return GetIndexGroupMode(set, id);

                var levelCount = _levels.Count;
                var groupNumber = Mathf.CeilToInt((float)levelCount / levelGroupCount);
                var lastGroupCount = levelCount - (groupNumber - 1) * levelGroupCount;
                var levelId = (id[1] * subLevelPerLevel + id[2]);

                //long seed = levelId * 23 + this.seed;
                //_randomModule.RandomInit((uint) seed);
                uint randomNumber = Hash(levelId) % (uint)(levelCount);// _randomModule.IRandom(0, levelCount - 1);
                int index = Mathf.FloorToInt((float)randomNumber / levelGroupCount);

                if (index == groupNumber - 1 && lastGroupCount > 0) {
                    levelId = levelId % lastGroupCount;
                    index = levelGroupCount * index + levelId;
                }
                else {
                    levelId = levelId % levelGroupCount;
                    index = levelGroupCount * index + levelId;
                }
                return index;
            }


            private int GetIndexGroupMode(LevelSet set, LevelSet.ID id)
            {
                set.TryGetLinearIndex(id, out int linearIndex);
                // todo group based randomdan dolayi degistilirdi. Incelenmesi lazim
                var levelId = linearIndex - set.GetLevelCount(0);
                var levelCount = _levels.Count;
                int randomIndex = (int)Hash(levelId / levelGroupCount);
                randomIndex = randomIndex % ((levelCount + levelGroupCount - 1) / levelGroupCount);
                randomIndex *= levelGroupCount;
                randomIndex += (levelId % levelGroupCount);
                randomIndex = Mathf.Min(randomIndex, levelCount - 1);

                return randomIndex;


                /*var levelCount = _levels.Count;

                int randomIndex = (int)(Hash(id.Level) % (uint)((levelCount + subLevelPerLevel - 1) / subLevelPerLevel));
                randomIndex *= subLevelPerLevel;
                randomIndex += id.SubLevel;
                randomIndex = Mathf.Min(randomIndex, levelCount - 1);

                return randomIndex;*/
            }

            private void EnsureLevels(LevelSet set) {
                if (_levels == null || _levels.Count == 0) {
                    _levels = new List<LevelReference>();
                    ID ignoreUntil = ID.New(ignoreUntilWorld, ignoreUntilLevel, ignoreUntilSubLevel);
                    int start = set._levels.GetIndex(ignoreUntil.Value);
                    if (start < 0) {
                        start = ~start;
                    }
                    var e = new LevelEnumerator(set._levels.array, start, set._levels.Count);
                    while (e.MoveNext()) {
                        var ec = e.Current;
                        if (new ID( ec.key).World > startWorld) {
                            break;
                        }
                        LevelReference @ref;
                        @ref.level = ec.value;
                        @ref.id = new ID(ec.key);
                        _levels.Add( @ref);
                    }
                }
            }
        }
        struct LevelReference {
            public ID id;
            public AddressableLevel level;
        }
        #if UNITY_EDITOR
        public struct LevelDataEnumerable : IEnumerable<Level> {
            private readonly LevelEnumerator _levelEnumerator;
            public LevelDataEnumerable(LevelEnumerator levelEnumerator) {
                _levelEnumerator = levelEnumerator;
            }
            public IEnumerator<Level> GetEnumerator() {
                return new LevelDataEnumerator(_levelEnumerator);
            }
            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
        }
        public struct LevelDataEnumerator : IEnumerator<Level> {
            private LevelEnumerator _levelEnumerator;
            public LevelDataEnumerator(LevelEnumerator levelEnumerator) {
                _levelEnumerator = levelEnumerator;
            }
            public bool MoveNext() {
                return _levelEnumerator.MoveNext();
            }
            public void Reset() {
                _levelEnumerator.Reset();
            }
            public Level Current => (Level) _levelEnumerator.Current.value.editorAsset;
            object IEnumerator.Current => Current;
            public void Dispose() {
                _levelEnumerator.Dispose();
            }
        }
        #endif
    }
}