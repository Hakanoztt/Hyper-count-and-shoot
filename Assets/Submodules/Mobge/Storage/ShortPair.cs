using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public class ShortPair : MonoBehaviour {
        public short value1;
        public short value2;
        public int SaveValue {
            get {
                return (int)((uint)(ushort)value1 + ((uint)(ushort)value2 << 16));
            }
        }
        public ShortPair(int saveValue) {
            value1 = (short)saveValue;
            value2 = (short)((saveValue) >> 16);

        }
        public ShortPair(short value1, short value2) {
            this.value1 = value1;
            this.value2 = value2;
        }
    }
}