using Mobge.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Mobge.HyperCasualSetup
{
    public abstract class ALevelSet : ScriptableObject
    {
        public AssetReference[] extraAddressables;
        public abstract int WorldCount { get; }
        public abstract int GetLevelCount(int world);
        public abstract bool TryIncreaseLevel(ref ID id);
        public abstract bool TryDecreaseLevel(ref ID id);
        public abstract IEnumerator<ID> GetDependencies(ID target);
        public abstract ID ToNearestLevelId(ID id);
        public abstract bool TryGetLinearIndex(ID id, out int index);
        public abstract AddressableLevel this[ID id] { get; }


        [Serializable]
        public class AddressableLevel : AssetReferenceT<Level>
        {
#if UNITY_EDITOR
            public int editorTag;
#endif
            public int LinearId { get; internal set; }
            public AddressableLevel(string guid) : base(guid) {
            }
            public new bool RuntimeKeyIsValid() => base.RuntimeKeyIsValid();
            public AddressableLevel() : base(null) { }
#if UNITY_EDITOR
            public override string ToString() {
                return editorAsset ? editorAsset.name : base.ToString();
            }
#endif
        }
        public static ID IdFromValue(int value) {
            return new ID(value);
        }
        /// <summary>
        /// Id to determine 
        /// </summary>
        [Serializable]
        public struct ID
        {
            static readonly int[] offsets = new int[] { 24, 10, 4, 0 };
            static readonly int[] lengths = new int[] { 8, 14, 6, 4 };
            [SerializeField] private int _id;
            public const string c_valueFieldName = nameof(_id);
            public int this[int index] {
                get {
                    return Get(offsets[index], lengths[index]);
                }
                set {
                    Set(offsets[index], lengths[index], value);
                }
            }
            public ID(int value) {
                _id = value;
            }
            public int World => this[0];
            public int Level => this[1];
            public int SubLevel => this[2];

            public int Value {
                get => _id;
                set => _id = value;
            }

            public static bool operator ==(ID id1, ID id2) {
                return id1._id == id2._id;
            }
            public static bool operator !=(ID id1, ID id2) {
                return id1._id != id2._id;
            }

            private int Get(int offset, int length) {
                int i = _id;
                i = i >> offset;
                int mask = (1 << length) - 1;
                return (i & mask) - 1;
            }
            private void Set(int offset, int length, int value) {
                value = (value + 1) << offset;
                int mask = ((1 << length) - 1) << offset;
                _id = (value & mask) | (~mask & _id);
            }
            public int Depth {
                get {
                    int d = 0;
                    while (this[d] >= 0) {
                        d++;
                        if (d == offsets.Length) {
                            return d;
                        }
                    }
                    return d;
                }
            }
            public override int GetHashCode() {
                return _id;
            }
            public override string ToString() {
                return _id.ToString();
            }
            public static ID New(int world) {
                ID i = new ID();
                i[0] = world;
                return i;
            }
            public static ID New(int world, int level) {
                ID i = new ID();
                i[0] = world;
                i[1] = level;
                return i;
            }
            public static ID New(int world, int level, int subLevel) {
                ID i = new ID();
                i[0] = world;
                i[1] = level;
                i[2] = subLevel;
                return i;
            }
            public static ID New(int world, int level, int subLevel, int subSubLevel) {
                ID i = new ID();
                i[0] = world;
                i[1] = level;
                i[2] = subLevel;
                i[3] = subSubLevel;
                return i;
            }
            public static ID FromWorldLevel(int world, int level) {
                ID i = new ID();
                i[0] = world;
                i[1] = level;
                return i;
            }

            public static ID New() {
                ID i;
                i._id = -1;
                return i;
            }
            public static implicit operator int(ID id) {
                return id.Value;
            }

            public override bool Equals(object obj) {
                return obj is ID iD &&
                       _id == iD._id;
            }
        }
    }
}