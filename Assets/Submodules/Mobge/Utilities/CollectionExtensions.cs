using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mobge
{
    public static class CollectionExtensions
    {
        public static void ReverseDirection<T>(this T[] arr) {
            if (arr == null || arr.Length <= 0) return;
            for (int i = 0; i < arr.Length / 2; i++) {
                var tmp = arr[i];
                arr[i] = arr[arr.Length - 1 - i];
                arr[arr.Length - 1 - i] = tmp;
            }
        }
        public static void Reverse<T>(this T[] arr, int index, int length) {
            int operationCount = length / 2;
            int topLimit = index + length - 1;
            for (int i = 0; i < operationCount; i++) {
                var tmp = arr[i + index];
                arr[i + index] = arr[topLimit - i];
                arr[topLimit - i] = tmp;
            }
        }
        public static void Shift<T>(this T[] arr, int amount) {
            if (amount == 0) {
                return;
            }
            var len = arr.Length;
            amount = amount % len;
            int ind = 0;
            int starti = 0;
            while (ind < len) {
                int ls = (starti + len - amount) % len;
                do {
                    var next = (ls + amount) % len;
                    Swap(ref arr[next], ref arr[ls]);
                    ind++;

                    ls = (ls + len - amount) % len;
                }
                while (ls != starti);
                ind++;
                starti++;
            }
        }
        public static void Swap<T>(ref T t1, ref T t2) {
            var temp = t1;
            t1 = t2;
            t2 = temp;
        }
        
        
    }
}