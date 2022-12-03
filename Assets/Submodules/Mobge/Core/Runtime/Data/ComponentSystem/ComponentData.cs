using Mobge.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.Core {
    [Serializable]
    public class LevelComponentData {
        [SerializeField]
        private BinaryObjectData data;
        [SerializeField]
        private ComponentDefinition definition;
        public ComponentDefinition Definition => definition;

        public int Size => data.data.Length;
        public int ReferenceCount => data.targets.Length;
        public LevelComponentData() {

        }
        public LevelComponentData(ComponentDefinition definition, object obj) {
            this.data = new BinaryObjectData(obj);
            this.definition = definition;
        }
        public T GetObject<T>() {
            return (T)GetObject();
        }
        public void SetObject(object obj) {
            data = new BinaryObjectData(obj);
        }
        public object GetObject() {
            return data.GetObject(definition.DataType);
        }
    }
}