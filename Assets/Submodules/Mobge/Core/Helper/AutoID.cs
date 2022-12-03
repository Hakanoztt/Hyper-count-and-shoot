using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core
{
    // If there is no use for this class, as is right now (21 Feb '19)
    public class AutoID
    {
        private static uint UsedCounter;
        private static Queue<uint> ReUse = new Queue<uint>();

        private static object Lock = new object();
        public static uint id = 1;

        public AutoID()
        {
            lock (Lock)
            {
                uint nextIndex = GetAvailableIndex();
                if (nextIndex == 0)
                {
                    nextIndex = ++UsedCounter;
                }
                // Auto increment ID
                id = nextIndex;
            }
        }
        public static uint ID()
        {
            return id;
        }

        public static void Dispose()
        {
            lock (Lock)
            {
                ReUse.Enqueue(id);
            }
        }

        // Todo: this method is here for testing reasons; it is not intended to exist past testing phase.
        // If this is still in the code at the production time. REMOVE IT!
        public static void Reset()
        {
            UsedCounter = 0;
        }

        private uint GetAvailableIndex()
        {
            if (ReUse.Count > 0) 
               return ReUse.Dequeue();

            // Nothing available.
            return 0;
        }

        override public string ToString()
        {
            return id.ToString();
        }
    }
}