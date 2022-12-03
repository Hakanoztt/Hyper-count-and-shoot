using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Mobge {
    public class ResourceFolderManager : ScriptableObject {
        private static ResourceFolderManager shared;
        public List<ObjectProps> objList;
        public string scriptFolder;

        public static ResourceFolderManager Construct() {
            var otc = ScriptableObject.CreateInstance<ResourceFolderManager>();
            otc.objList = new List<ObjectProps>();
            return otc;
        }
        [Serializable]
        public class ObjectProps {
            public UnityEngine.Object obj;
            public bool generateInstantiateFunc;
        }
    }

}