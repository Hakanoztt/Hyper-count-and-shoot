using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public class SomeScript : MonoBehaviour {

        public DataRef someData;

        void Start() {
            Debug.Log(someData.GetValue<int>());
        }
    }
}
