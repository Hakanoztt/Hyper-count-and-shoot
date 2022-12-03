using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge
{
    public class ContextManager<T> 
    {
        private Dictionary<object, T> _cd;
        //private Stack<T> _cs;
        public T Current { get; private set; }
        //public ContextManager(bool IsSimple)
        public ContextManager()
        {
            //if (IsSimple) 
            //    { _cs = new Stack<T>(); }
            //else 
                { _cd = new Dictionary<object, T>(); }
        }
        /// <summary>
        /// Sets current context to given context. 
        /// Old current context is saved for later if available. 
        /// </summary>
        /// <param name="context">Context.</param>
        //public void PushContext(T context)
        //{
        //    if (Current != null)
        //        _cs.Push(Current);
        //    Current = context;
        //}
        /// <summary>
        /// Sets current context to given context. 
        /// Old current context is saved for later if available. 
        /// </summary>
        /// <param name="context">Context.</param>
        public void PushContext(object owner, T context)
        {
            if (_cd.ContainsKey(owner))
                _cd.Remove(owner);
            if (Current != null)
                _cd.Add(owner, Current);
            Current = context;
        }
        /// <summary>
        /// (LIFO) Sets old context to current context if available.
        /// </summary>
        //public void PopContext()
        //{
        //    if (_cs.Count > 0)  
        //        Current = _cs.Pop();
        //    else
        //    {
        //        Current = default;
        //        throw new System.Exception("context manager is empty.");
        //    }
        //}
        /// <summary>
        /// (LIFO) Sets old context to current context if available.
        /// </summary>
        public void GetContext(object owner)
        {
            if (_cd.Count > 0)
            {
                if (_cd.TryGetValue(owner, out T value))
                {
                    Current = value;
                }
                else
                {
                    Current = default(T);
                    //throw new System.Exception("context of " + nameof(owner) + " does not exist.");
                }
            }
            else
            {
                Current = default(T);
                //throw new System.Exception("context of " + nameof(owner) + " is empty.");
            }
        }
    }
}