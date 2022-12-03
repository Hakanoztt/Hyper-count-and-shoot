using System.Collections;
using System.Collections.Generic;
using Mobge;
using UnityEngine;

namespace Mobge
{
    public class BasicPool<T> where T : new()
    {

        public Stack<T> stack = new Stack<T>();

        public T Get() {
            if (stack.Count > 0) {
                return stack.Pop();
            }
            return new T();
        }

        public void Release(T t) {
            stack.Push(t);
        }
    }
}