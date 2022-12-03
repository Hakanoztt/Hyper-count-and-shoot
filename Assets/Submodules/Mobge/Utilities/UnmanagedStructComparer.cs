using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge
{
    public unsafe sealed class UnmanagedStructComparer<T> : IEqualityComparer<T> where T : unmanaged
    {
		
		static int size = sizeof(T);
		
        public bool Equals(T x, T y) {
            var p1 = (byte*)&x;
            var p2 = (byte*)&y;
            for (int i = 0; i < size; i++) {
                if (p1[i] != p2[i]) {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(T obj) {
            byte* ptr = (byte*)&obj;
            int rv = 0;
            for(int i = 0; i < size; i++) {
                rv = rv * 807403 + ptr[i]; // 807403 is a random prime number
            }
            return rv;
        }
    }
}