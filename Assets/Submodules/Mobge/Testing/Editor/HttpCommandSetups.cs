using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace Mobge.Test {
    public class HttpCommandSetups : ScriptableObject {
        public string ip;
        public int port;
        public Setup[] setups;
        [Serializable]
        public struct Setup {
            public string name;
            public MethodCall[] methods;
            public override string ToString() {
                return name;
            }
        }
        [Serializable]
        public struct MethodCall{
            public string methodName;
            public KeyValuePair[] parameters;
        }
        [Serializable]
        public struct KeyValuePair {
            public string key, value;
        }

    }
}